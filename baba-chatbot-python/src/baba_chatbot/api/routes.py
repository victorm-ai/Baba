"""
Rutas y endpoints de la API.
"""
import logging
from fastapi import APIRouter, Form, HTTPException
from fastapi.responses import Response

from .dependencies import get_settings
from ..application.conversation.orchestrator import ConversationOrchestrator
from ..application.conversation.guardrails import GuardrailsValidator
from ..integrations.llm import LlmClient, KnowledgeRepository, PromptRepository
from ..integrations.catalog import CatalogRepository


logger = logging.getLogger(__name__)

# Router para Twilio
twilio_router = APIRouter()

# Inicializar dependencias (en una aplicación real, usar inyección de dependencias)
settings = get_settings()

# Inicializar servicios
knowledge_repository = KnowledgeRepository(settings.knowledge_base_path)
prompt_repository = PromptRepository(settings.prompts_path)
catalog_repository = CatalogRepository(
    settings.catalog_file_path,
    settings.catalog_csv_file_path
)

llm_client = LlmClient(
    api_key=settings.openai_api_key,
    model=settings.openai_model,
    temperature=settings.openai_temperature,
    knowledge_repository=knowledge_repository,
    catalog_repository=catalog_repository
)

guardrails_validator = GuardrailsValidator()

orchestrator = ConversationOrchestrator(
    llm_client=llm_client,
    guardrails_validator=guardrails_validator
)


@twilio_router.post("/incoming", response_class=Response)
async def incoming_message(
    From: str = Form(...),
    Body: str = Form(...),
    To: str = Form(None),
    MessageSid: str = Form(None)
):
    """
    Procesa mensajes SMS/WhatsApp entrantes desde Twilio.
    Valida el contenido, genera una respuesta mediante IA y aplica guardrails de seguridad.
    
    Args:
        From: Número de teléfono del remitente
        Body: Contenido del mensaje
        To: Número de teléfono del destinatario (opcional)
        MessageSid: ID del mensaje de Twilio (opcional)
        
    Returns:
        Respuesta TwiML para Twilio
    """
    logger.info(f"Received message from {From}: {Body}")

    try:
        # Obtener el prompt del sistema
        system_prompt = await prompt_repository.get_system_prompt()

        if not system_prompt:
            logger.warning("System prompt is empty, using default")
            system_prompt = "Eres Baba, un asistente virtual de Kavak que ayuda a clientes a encontrar vehículos."

        user_id = From or "unknown"
        user_message = Body or ""

        # Procesar mensaje a través del orquestador
        conversation_result = await orchestrator.process_message(
            user_id=user_id,
            user_message=user_message,
            system_prompt=system_prompt,
            conversation_history=None
        )

        # Logging adicional
        if conversation_result.requires_escalation:
            logger.warning(
                f"Conversation requires human escalation for user {user_id}. "
                f"Flags: {[f.value for f in conversation_result.moderation_flags]}"
            )

        if conversation_result.has_pii_violations:
            logger.warning(f"PII detected and masked in response for user {user_id}")

        logger.info(f"Generated AI response for {From}: {conversation_result.success}")

        # Crear respuesta TwiML
        twiml_response = f"""<?xml version="1.0" encoding="UTF-8"?>
<Response>
    <Message>{conversation_result.message}</Message>
</Response>"""

        return Response(
            content=twiml_response,
            media_type="application/xml"
        )

    except Exception as ex:
        logger.error(f"Error processing incoming message: {ex}")

        error_twiml = """<?xml version="1.0" encoding="UTF-8"?>
<Response>
    <Message>Lo siento, hubo un error procesando tu mensaje. Por favor intenta de nuevo.</Message>
</Response>"""

        return Response(
            content=error_twiml,
            media_type="application/xml"
        )

