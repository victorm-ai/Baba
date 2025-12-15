"""
Orquestador de conversaciones que gestiona el flujo completo de validaciones,
generación de respuestas y sistema de reincidencia.
"""
import logging
from typing import List, Optional, Dict
from dataclasses import dataclass, field
from ...abstractions.interfaces import ILlmClient, IGuardrailsValidator
from ..guardrails.content_moderation_result import ModerationFlag, ModerationSeverity


logger = logging.getLogger(__name__)


@dataclass
class ConversationResponse:
    """
    Respuesta del orquestador de conversación.
    """
    success: bool
    message: str
    requires_escalation: bool = False
    moderation_flags: List[ModerationFlag] = field(default_factory=list)
    has_pii_violations: bool = False

    def __post_init__(self):
        """Asegura que moderation_flags sea una lista mutable."""
        if not isinstance(self.moderation_flags, list):
            self.moderation_flags = list(self.moderation_flags) if self.moderation_flags else []


class ConversationOrchestrator:
    """
    Orquesta el flujo completo de conversación con validaciones, generación de respuestas
    y gestión de violaciones (sistema de reincidencia de 3 strikes).
    """

    def __init__(
        self,
        llm_client: ILlmClient,
        guardrails_validator: IGuardrailsValidator
    ):
        """
        Inicializa el orquestador de conversaciones.
        
        Args:
            llm_client: Cliente para comunicación con el LLM
            guardrails_validator: Validador de guardrails
        """
        self._llm_client = llm_client
        self._guardrails_validator = guardrails_validator
        self._user_violation_count: Dict[str, int] = {}

    async def process_message(
        self,
        user_id: str,
        user_message: str,
        system_prompt: str,
        conversation_history: Optional[List[str]] = None
    ) -> ConversationResponse:
        """
        Procesa un mensaje del usuario a través del pipeline completo de validación,
        generación de respuesta y verificación de guardrails.
        
        Args:
            user_id: Identificador del usuario
            user_message: Mensaje del usuario
            system_prompt: Prompt del sistema con instrucciones
            conversation_history: Historial de conversación opcional
            
        Returns:
            Respuesta procesada con el resultado de todas las validaciones
        """
        logger.info(f"Processing message for user {user_id}")

        # Validar entrada del usuario
        user_validation = self._guardrails_validator.validate_user_input(user_message)

        if not user_validation.is_appropriate:
            logger.warning(
                f"User message failed validation for user {user_id}. "
                f"Flags: {[f.value for f in user_validation.flags]}"
            )

            # Incrementar contador de violaciones
            violation_count = self._user_violation_count.get(user_id, 0) + 1
            self._user_violation_count[user_id] = violation_count

            # Determinar si se debe terminar la conversación
            if (user_validation.severity == ModerationSeverity.HIGH or
                    violation_count >= 3):
                logger.warning(
                    f"User {user_id} has {violation_count} violations. "
                    "Terminating conversation."
                )

                return ConversationResponse(
                    success=False,
                    message=self._guardrails_validator.get_response_for_violation(
                        user_validation,
                        violation_count
                    ),
                    requires_escalation=True,
                    moderation_flags=user_validation.flags
                )

            return ConversationResponse(
                success=False,
                message=user_validation.suggested_response or (
                    "Lo siento, no puedo ayudarte con eso. "
                    "¿Hay algo relacionado con vehículos en lo que pueda asistirte?"
                ),
                requires_escalation=False,
                moderation_flags=user_validation.flags
            )

        # Generar respuesta del LLM
        try:
            llm_response = await self._llm_client.generate_response(
                system_prompt,
                user_message,
                conversation_history
            )
        except Exception as ex:
            logger.error(f"Error generating LLM response for user {user_id}: {ex}")
            
            return ConversationResponse(
                success=False,
                message="Lo siento, tuve un problema técnico. ¿Podrías intentar de nuevo?",
                requires_escalation=False
            )

        # Validar respuesta del LLM
        response_validation = await self._guardrails_validator.validate_response(llm_response)

        if not response_validation.is_valid:
            logger.error(
                f"LLM response failed guardrails validation for user {user_id}. "
                f"Violations: {response_validation.violations}"
            )

            if response_validation.requires_human_escalation:
                return ConversationResponse(
                    success=False,
                    message=(
                        "Déjame conectarte con un asesor especializado que podrá ayudarte mejor. "
                        "Dame un momento..."
                    ),
                    requires_escalation=True
                )

            return ConversationResponse(
                success=False,
                message=(
                    "Disculpa, déjame reformular eso. "
                    "¿Podrías ser más específico sobre lo que necesitas?"
                ),
                requires_escalation=False
            )

        # Usar contenido limpio (con PII enmascarada)
        final_response = response_validation.cleaned_content or llm_response

        # Validar calidad de la respuesta
        if not self._guardrails_validator.validate_response_quality(final_response):
            logger.warning(f"Response failed quality validation for user {user_id}")
            
            return ConversationResponse(
                success=False,
                message=(
                    "¿Podrías darme más detalles sobre lo que buscas? "
                    "Así puedo ayudarte mejor."
                ),
                requires_escalation=False
            )

        logger.info(f"Successfully processed message for user {user_id}")

        # Limpiar contador de violaciones en caso de éxito
        if user_id in self._user_violation_count:
            del self._user_violation_count[user_id]

        # Verificar si hubo violaciones de PII
        has_pii_violations = any(
            "detected" in v.lower()
            for v in response_validation.violations
        )

        return ConversationResponse(
            success=True,
            message=final_response,
            requires_escalation=False,
            has_pii_violations=has_pii_violations
        )

    def reset_violation_count(self, user_id: str) -> None:
        """
        Reinicia el contador de violaciones para un usuario específico.
        
        Args:
            user_id: Identificador del usuario
        """
        if user_id in self._user_violation_count:
            del self._user_violation_count[user_id]

