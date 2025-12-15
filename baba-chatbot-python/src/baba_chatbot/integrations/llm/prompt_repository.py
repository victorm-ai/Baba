"""
Repositorio de prompts del sistema.
Carga y gestiona los prompts desde archivos markdown.
"""
import os
import logging
from pathlib import Path


logger = logging.getLogger(__name__)


class PromptRepository:
    """
    Repositorio para cargar y gestionar prompts del sistema.
    """

    def __init__(self, prompts_path: str = None):
        """
        Inicializa el repositorio de prompts.
        
        Args:
            prompts_path: Ruta al directorio de prompts
        """
        if prompts_path is None:
            prompts_path = os.path.join(os.getcwd(), "config", "prompts")
        
        self._prompts_path = prompts_path

    async def get_system_prompt(self) -> str:
        """
        Obtiene el prompt del sistema.
        
        Returns:
            Contenido del prompt del sistema
        """
        return await self._load_prompt("system.md")

    async def get_guardrails_prompt(self) -> str:
        """
        Obtiene el prompt de guardrails.
        
        Returns:
            Contenido del prompt de guardrails
        """
        return await self._load_prompt("guardrails.md")

    async def get_value_prop_prompt(self) -> str:
        """
        Obtiene el prompt de propuesta de valor.
        
        Returns:
            Contenido del prompt de propuesta de valor
        """
        return await self._load_prompt("value-prop.md")

    async def get_response_style_prompt(self) -> str:
        """
        Obtiene el prompt de estilo de respuestas.
        
        Returns:
            Contenido del prompt de estilo de respuestas
        """
        return await self._load_prompt("response-style.md")

    async def _load_prompt(self, file_name: str) -> str:
        """
        Carga un archivo de prompt específico.
        
        Args:
            file_name: Nombre del archivo de prompt
            
        Returns:
            Contenido del prompt o cadena vacía si hay error
        """
        try:
            file_path = os.path.join(self._prompts_path, file_name)
            
            if not os.path.exists(file_path):
                logger.warning(f"Prompt file not found: {file_path}")
                return ""

            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                return content
        except Exception as ex:
            logger.error(f"Error loading prompt file {file_name}: {ex}")
            return ""

