"""
Interfaces para los servicios de la aplicación.
Define contratos para LLM, Catálogo y Validadores de Guardrails.
"""
from abc import ABC, abstractmethod
from typing import List, Optional
from ...domain.entities import Vehicle
from ...domain.value_objects import VehicleQuery
from ..conversation.guardrails.content_moderation_result import (
    ContentModerationResult,
    GuardrailsValidationResult
)


class ILlmClient(ABC):
    """
    Cliente para comunicación con modelos de lenguaje (LLM).
    Gestiona generación de respuestas y llamadas a funciones.
    """

    @abstractmethod
    async def generate_response(
        self,
        system_prompt: str,
        user_message: str,
        conversation_history: Optional[List[str]] = None
    ) -> str:
        """
        Genera una respuesta de texto usando el LLM con contexto RAG y función calling.
        
        Args:
            system_prompt: Prompt del sistema con instrucciones
            user_message: Mensaje del usuario
            conversation_history: Historial de conversación (opcional)
            
        Returns:
            Respuesta generada por el LLM
        """
        pass

    @abstractmethod
    async def generate_structured_response(self, prompt: str, response_type: type):
        """
        Genera una respuesta estructurada con un esquema específico.
        
        Args:
            prompt: Prompt para el LLM
            response_type: Tipo de respuesta esperada
            
        Returns:
            Respuesta estructurada del tipo especificado
        """
        pass


class ICatalogRepository(ABC):
    """
    Repositorio para búsqueda y consulta del catálogo de vehículos.
    """

    @abstractmethod
    async def search_vehicles(self, query: VehicleQuery) -> List[Vehicle]:
        """
        Busca vehículos en el catálogo según criterios especificados.
        
        Args:
            query: Criterios de búsqueda
            
        Returns:
            Lista de vehículos que coinciden con los criterios
        """
        pass

    @abstractmethod
    async def get_vehicle_by_id(self, vehicle_id: str) -> Optional[Vehicle]:
        """
        Obtiene un vehículo específico por su ID o stock_id.
        
        Args:
            vehicle_id: ID o stock_id del vehículo
            
        Returns:
            Vehículo encontrado o None si no existe
        """
        pass


class IGuardrailsValidator(ABC):
    """
    Validador de guardrails para moderación de contenido y seguridad de respuestas.
    """

    @abstractmethod
    def validate_user_input(self, user_message: str) -> ContentModerationResult:
        """
        Valida y modera el contenido del mensaje de entrada del usuario.
        
        Args:
            user_message: Mensaje del usuario a validar
            
        Returns:
            Resultado de la moderación de contenido
        """
        pass

    @abstractmethod
    async def validate_response(self, response: str) -> GuardrailsValidationResult:
        """
        Valida la respuesta generada por el LLM (PII, promesas no autorizadas, calidad).
        
        Args:
            response: Respuesta del LLM a validar
            
        Returns:
            Resultado de la validación de guardrails
        """
        pass

    @abstractmethod
    def validate_response_quality(self, response: str) -> bool:
        """
        Valida la calidad básica de la respuesta (longitud, emojis excesivos).
        
        Args:
            response: Respuesta a validar
            
        Returns:
            True si la respuesta cumple con los estándares de calidad
        """
        pass

    @abstractmethod
    def get_response_for_violation(
        self,
        moderation_result: ContentModerationResult,
        violation_count: int = 1
    ) -> str:
        """
        Obtiene un mensaje de respuesta apropiado según el tipo de violación detectada.
        
        Args:
            moderation_result: Resultado de la moderación
            violation_count: Número de violaciones acumuladas
            
        Returns:
            Mensaje de respuesta apropiado
        """
        pass

