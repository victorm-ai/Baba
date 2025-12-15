"""
Repositorio de conocimiento (RAG simple basado en archivos markdown).
Carga y busca información relevante en documentos de la base de conocimiento.
"""
import os
import logging
from typing import Dict, List, Tuple
from pathlib import Path


logger = logging.getLogger(__name__)


class KnowledgeRepository:
    """
    Repositorio de conocimiento que implementa RAG simple basado en archivos markdown.
    """

    def __init__(self, knowledge_base_path: str = None):
        """
        Inicializa el repositorio de conocimiento.
        
        Args:
            knowledge_base_path: Ruta al directorio con documentos markdown
        """
        if knowledge_base_path is None:
            knowledge_base_path = os.path.join(
                os.getcwd(), "..", "..", "config", "rag", "kb_sources"
            )
        
        self._knowledge_base_path = knowledge_base_path
        self._documents: Dict[str, str] = {}
        self._load_documents()

    def _load_documents(self) -> None:
        """
        Carga todos los documentos markdown del directorio de conocimiento.
        Omite archivos README.
        """
        try:
            logger.info(f"Loading knowledge base from: {self._knowledge_base_path}")

            if not os.path.exists(self._knowledge_base_path):
                logger.warning(f"Knowledge base directory not found: {self._knowledge_base_path}")
                return

            # Buscar todos los archivos markdown
            for root, _, files in os.walk(self._knowledge_base_path):
                for file in files:
                    if file.endswith('.md'):
                        file_name = Path(file).name
                        
                        # Omitir archivos README
                        if file_name.upper().startswith('README'):
                            continue

                        file_path = os.path.join(root, file)
                        try:
                            with open(file_path, 'r', encoding='utf-8') as f:
                                content = f.read()
                                self._documents[file_name] = content
                                logger.info(
                                    f"Loaded document: {file_name} ({len(content)} characters)"
                                )
                        except Exception as ex:
                            logger.error(f"Error reading file {file_path}: {ex}")

            logger.info(f"Knowledge base loaded: {len(self._documents)} documents")
        except Exception as ex:
            logger.error(f"Error loading knowledge base: {ex}")

    def search_relevant_context(self, query: str, max_chunks: int = 3) -> str:
        """
        Busca información relevante en la base de conocimiento usando búsqueda por palabras clave.
        Divide documentos en secciones y puntúa según relevancia.
        
        Args:
            query: Consulta del usuario
            max_chunks: Número máximo de secciones a retornar
            
        Returns:
            Contexto relevante concatenado
        """
        if not self._documents:
            logger.warning("No documents loaded in knowledge base")
            return ""

        try:
            # Extraer palabras clave de la consulta
            query_words = set(
                word.lower()
                for word in query.replace(',', ' ').replace('.', ' ')
                .replace('?', ' ').replace('!', ' ').split()
                if len(word) > 3
            )

            if not query_words:
                return ""

            # Buscar secciones relevantes
            relevant_sections: List[Tuple[str, str, int]] = []

            for doc_name, content in self._documents.items():
                sections = self._split_into_sections(content)
                
                for section in sections:
                    section_lower = section.lower()
                    score = sum(1 for word in query_words if word in section_lower)
                    
                    if score > 0:
                        relevant_sections.append((doc_name, section, score))

            # Ordenar por relevancia y tomar las mejores
            top_sections = sorted(
                relevant_sections,
                key=lambda x: x[2],
                reverse=True
            )[:max_chunks]

            if not top_sections:
                logger.debug(f"No relevant context found for query: {query}")
                return ""

            # Concatenar secciones
            context = "\n\n---\n\n".join(section for _, section, _ in top_sections)
            logger.info(f"Found {len(top_sections)} relevant sections for query")
            
            return context
        except Exception as ex:
            logger.error(f"Error searching knowledge base: {ex}")
            return ""

    def _split_into_sections(self, content: str) -> List[str]:
        """
        Divide un documento en secciones basándose en headers markdown.
        
        Args:
            content: Contenido del documento
            
        Returns:
            Lista de secciones
        """
        sections = []
        lines = content.split('\n')
        current_section = []

        for line in lines:
            # Detectar inicio de nueva sección
            if line.startswith('## ') or line.startswith('### '):
                if current_section:
                    section_text = '\n'.join(current_section).strip()
                    if len(section_text) > 50:
                        sections.append(section_text)
                    current_section = []

            current_section.append(line)

        # Agregar última sección
        if current_section:
            section_text = '\n'.join(current_section).strip()
            if len(section_text) > 50:
                sections.append(section_text)

        return sections

    def reload(self) -> None:
        """Recarga los documentos de la base de conocimiento."""
        self._documents.clear()
        self._load_documents()

    def get_stats(self) -> Tuple[int, int]:
        """
        Obtiene estadísticas de la base de conocimiento.
        
        Returns:
            Tupla con (número de documentos, total de caracteres)
        """
        total_chars = sum(len(doc) for doc in self._documents.values())
        return len(self._documents), total_chars

