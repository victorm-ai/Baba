"""
Implementa validación de guardrails para moderación de contenido, detección de PII,
validación de calidad y prevención de promesas no autorizadas.
"""
import re
import logging
from typing import List, Dict, Set
from ...abstractions.interfaces import IGuardrailsValidator
from .content_moderation_result import (
    ContentModerationResult,
    GuardrailsValidationResult,
    ModerationFlag,
    ModerationSeverity
)


logger = logging.getLogger(__name__)


class GuardrailsValidator(IGuardrailsValidator):
    """
    Implementa validación de guardrails para moderación de contenido, detección de PII,
    validación de calidad y prevención de promesas no autorizadas.
    """

    # Patrones regex para detección de PII
    CREDIT_CARD_PATTERN = re.compile(r'\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b')
    PHONE_PATTERN = re.compile(r'\b\d{3}[-.]?\d{3}[-.]?\d{4}\b')
    EMAIL_PATTERN = re.compile(r'\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b')
    INE_PATTERN = re.compile(r'\b[A-Z]{4}\d{6}[HM][A-Z]{5}\d{2}\b')
    CURP_PATTERN = re.compile(r'\b[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d\b')

    # Teléfonos y emails permitidos de Kavak
    ALLOWED_KAVAK_PHONES: Set[str] = {
        "5512345678",
        "8001234567"
    }

    ALLOWED_KAVAK_EMAILS: Set[str] = {
        "contacto@kavak.com",
        "soporte@kavak.com",
        "ventas@kavak.com"
    }

    # Palabras clave para moderación de contenido
    HATE_SPEECH_KEYWORDS: Set[str] = {
        "naz", "supremac", "discrimin", "racista", "xenofob"
    }

    VIOLENCE_KEYWORDS: Set[str] = {
        "matar", "asesinar", "golpear", "tortura", "violencia", "agredir",
        "atacar", "herir", "lastimar", "daño físico", "sangre",
        "arma", "cuchillo", "pistola", "disparar"
    }

    SEXUAL_CONTENT_KEYWORDS: Set[str] = {
        "sexo", "sexual", "erótico", "pornografía", "desnudo", "íntimo",
        "seducir", "excitar", "placer sexual", "orgasmo"
    }

    BUSINESS_KEYWORDS: Set[str] = {
        "auto", "carro", "coche", "vehículo", "camioneta", "suv", "sedán",
        "comprar", "vender", "precio", "financiamiento", "crédito", "pago",
        "kavak", "garantía", "certificación", "prueba de manejo", "entrega",
        "modelo", "marca", "año", "kilómetros", "transmisión", "motor",
        "seguro", "documentos", "factura", "contrato", "cita", "agendar"
    }

    OFF_TOPIC_PATTERNS: Set[str] = {
        "receta", "cocinar", "comida", "restaurante", "película", "serie",
        "música", "cantante", "actor", "deporte", "fútbol", "política",
        "elección", "presidente", "partido político", "religión", "dios",
        "iglesia", "rezar", "tarea", "traducir", "definición de",
        "clima", "temperatura", "tiempo libre", "hobby"
    }

    def __init__(self):
        """Inicializa el validador de guardrails."""
        pass

    def validate_user_input(self, user_message: str) -> ContentModerationResult:
        """
        Valida el mensaje de entrada del usuario aplicando moderación de contenido.
        Detecta discurso de odio, violencia, contenido sexual, acoso y temas fuera de contexto.
        
        Args:
            user_message: Mensaje del usuario a validar
            
        Returns:
            Resultado de la moderación de contenido
        """
        if not user_message or not user_message.strip():
            return ContentModerationResult(
                is_appropriate=True,
                severity=ModerationSeverity.NONE
            )

        logger.debug("Validating user input message")

        moderation_result = self._moderate_content(user_message)

        if not moderation_result.is_appropriate:
            logger.warning(
                f"User message failed content moderation. "
                f"Flags: {[f.value for f in moderation_result.flags]}, "
                f"Severity: {moderation_result.severity.value}"
            )

        return moderation_result

    async def validate_response(self, response: str) -> GuardrailsValidationResult:
        """
        Valida la respuesta del LLM verificando longitud, PII, promesas no autorizadas,
        información inventada y contenido inapropiado.
        Enmascara automáticamente datos sensibles detectados.
        
        Args:
            response: Respuesta del LLM a validar
            
        Returns:
            Resultado de la validación de guardrails
        """
        if not response or not response.strip():
            return GuardrailsValidationResult(
                is_valid=False,
                violations=["Empty response"]
            )

        result = GuardrailsValidationResult(
            is_valid=True,
            cleaned_content=response,
            violations=[]
        )

        logger.debug("Validating LLM response")

        # Validar longitud
        if len(response) > 1000:
            result.violations.append("Response too long (>200 words)")
            logger.warning("Response exceeds maximum length")

        if len(response) < 10:
            result.violations.append("Response too short")
            logger.warning("Response is too short")

        pii_detected = False

        # Detectar y enmascarar tarjetas de crédito
        if self.CREDIT_CARD_PATTERN.search(response):
            result.violations.append("Credit card number detected")
            result.cleaned_content = self.CREDIT_CARD_PATTERN.sub(
                "[TARJETA OCULTA]",
                result.cleaned_content
            )
            pii_detected = True
            logger.warning("Credit card number detected and masked")

        # Detectar y enmascarar INE/CURP
        if self.INE_PATTERN.search(response) or self.CURP_PATTERN.search(response):
            result.violations.append("INE/CURP detected")
            result.cleaned_content = self.INE_PATTERN.sub("[ID OCULTO]", result.cleaned_content)
            result.cleaned_content = self.CURP_PATTERN.sub("[CURP OCULTO]", result.cleaned_content)
            pii_detected = True
            logger.warning("INE/CURP detected and masked")

        # Detectar y enmascarar teléfonos no autorizados
        phone_matches = self.PHONE_PATTERN.finditer(response)
        for match in phone_matches:
            phone = match.group().replace("-", "").replace(".", "")
            if phone not in self.ALLOWED_KAVAK_PHONES:
                result.violations.append("Unauthorized phone number detected")
                result.cleaned_content = result.cleaned_content.replace(
                    match.group(),
                    "[TELÉFONO OCULTO]"
                )
                pii_detected = True
                logger.warning("Unauthorized phone number detected and masked")

        # Detectar y enmascarar emails no autorizados
        email_matches = self.EMAIL_PATTERN.finditer(response)
        for match in email_matches:
            if match.group().lower() not in self.ALLOWED_KAVAK_EMAILS:
                result.violations.append("Unauthorized email detected")
                result.cleaned_content = result.cleaned_content.replace(
                    match.group(),
                    "[EMAIL OCULTO]"
                )
                pii_detected = True
                logger.warning("Unauthorized email detected and masked")

        # Detectar promesas no autorizadas
        unauthorized_promises = self._detect_unauthorized_promises(response)
        if unauthorized_promises:
            result.violations.extend(unauthorized_promises)
            result.requires_human_escalation = True
            logger.warning(f"Unauthorized promises detected: {unauthorized_promises}")

        # Detectar información inventada
        if self._contains_invented_information(response):
            result.violations.append("Potentially invented vehicle information")
            logger.warning("Response may contain invented vehicle information")

        # Moderar contenido de la respuesta
        moderation_result = self._moderate_content(response)
        if not moderation_result.is_appropriate:
            flags_str = ", ".join([f.value for f in moderation_result.flags])
            result.violations.append(f"Inappropriate content in response: {flags_str}")
            result.is_valid = False
            logger.error("LLM generated inappropriate content")

        # Determinar si es válida
        if result.violations:
            if pii_detected:
                result.is_valid = True
            if not pii_detected or len(result.violations) > 3:
                result.is_valid = False

        logger.info(
            f"Response validation completed. "
            f"IsValid: {result.is_valid}, Violations: {len(result.violations)}"
        )

        return result

    def _detect_unauthorized_promises(self, response: str) -> List[str]:
        """
        Detecta promesas o compromisos no autorizados en las respuestas.
        
        Args:
            response: Respuesta a analizar
            
        Returns:
            Lista de violaciones detectadas
        """
        violations = []

        unauthorized_patterns: Dict[str, str] = {
            r"te garantizo (?:que|el|la)": "Unauthorized guarantee",
            r"te prometo": "Unauthorized promise",
            r"descuento (de|del) \d+%": "Unauthorized discount promise",
            r"precio especial solo para ti": "Unauthorized special pricing",
            r"sin necesidad de (evaluación|crédito|enganche)": "Unauthorized financing terms",
            r"garantía extendida gratis": "Unauthorized extended warranty",
            r"puedo modificar el precio": "Unauthorized price modification",
            r"te apruebo el crédito": "Unauthorized credit approval"
        }

        for pattern, violation_msg in unauthorized_patterns.items():
            if re.search(pattern, response, re.IGNORECASE):
                violations.append(violation_msg)

        return violations

    def _contains_invented_information(self, response: str) -> bool:
        """
        Detecta indicadores de información inventada o especificaciones técnicas inventadas.
        
        Args:
            response: Respuesta a analizar
            
        Returns:
            True si contiene información potencialmente inventada
        """
        invention_indicators = [
            r"este (auto|vehículo|coche) tiene exactamente \d+ hp",
            r"viene (con|equipado con) asientos de (piel|cuero) (italiana|premium)",
            r"consumo de exactamente \d+\.\d+ (km/l|l/100km)",
            r"velocidad máxima de \d+ km/h",
            r"aceleración de 0 a 100 en \d+\.\d+ segundos"
        ]

        return any(
            re.search(pattern, response, re.IGNORECASE)
            for pattern in invention_indicators
        )

    def validate_response_quality(self, response: str) -> bool:
        """
        Valida la calidad básica de una respuesta verificando longitud mínima
        y uso excesivo de emojis.
        
        Args:
            response: Respuesta a validar
            
        Returns:
            True si cumple con los estándares de calidad
        """
        if not response or not response.strip():
            return False

        if len(response) < 20:
            return False

        # Contar emojis (rango Unicode básico de emojis)
        emoji_count = sum(1 for c in response if 0x1F600 <= ord(c) <= 0x1F64F)
        if emoji_count > len(response) // 2:
            return False

        return True

    def _moderate_content(self, message: str) -> ContentModerationResult:
        """
        Modera el contenido de un mensaje verificando discurso de odio, violencia,
        contenido sexual, lenguaje ofensivo y temas fuera del contexto del negocio.
        
        Args:
            message: Mensaje a moderar
            
        Returns:
            Resultado de la moderación
        """
        if not message or not message.strip():
            return ContentModerationResult(
                is_appropriate=True,
                severity=ModerationSeverity.NONE
            )

        result = ContentModerationResult(
            is_appropriate=True,
            severity=ModerationSeverity.NONE,
            flags=[]
        )

        message_lower = message.lower()

        # Verificar discurso de odio
        if self._contains_keywords(message_lower, self.HATE_SPEECH_KEYWORDS):
            result.is_appropriate = False
            result.flags.append(ModerationFlag.HATE_SPEECH)
            result.severity = ModerationSeverity.HIGH
            result.suggested_response = (
                "No puedo continuar con este tipo de conversación. "
                "Si necesitas ayuda con la compra de un vehículo, estaré encantado de asistirte."
            )
            logger.warning("Hate speech detected in message")
            return result

        # Verificar contenido violento
        if self._contains_keywords(message_lower, self.VIOLENCE_KEYWORDS):
            result.is_appropriate = False
            result.flags.append(ModerationFlag.VIOLENCE)
            result.severity = ModerationSeverity.HIGH
            result.suggested_response = (
                "Este tipo de contenido no es apropiado para nuestra conversación. "
                "Si necesitas ayuda urgente, por favor contacta a las autoridades correspondientes."
            )
            logger.warning("Violent content detected in message")
            return result

        # Verificar contenido sexual
        if self._contains_keywords(message_lower, self.SEXUAL_CONTENT_KEYWORDS):
            result.is_appropriate = False
            result.flags.append(ModerationFlag.SEXUAL_CONTENT)
            result.severity = ModerationSeverity.HIGH
            result.suggested_response = (
                "Este tipo de conversación no es apropiada. "
                "Estoy aquí para ayudarte con la compra de vehículos. ¿Puedo asistirte con eso?"
            )
            logger.warning("Sexual content detected in message")
            return result

        # Verificar lenguaje ofensivo
        if self._contains_offensive_language(message_lower):
            result.is_appropriate = False
            result.flags.append(ModerationFlag.HARASSMENT)
            result.severity = ModerationSeverity.MEDIUM
            result.suggested_response = (
                "Entiendo que puedes estar frustrado, pero necesito que mantengamos "
                "una conversación respetuosa para poder ayudarte. "
                "¿Cómo puedo asistirte hoy con la compra de un vehículo?"
            )
            logger.warning("Offensive language detected in message")
            return result

        # Verificar temas off-topic
        if self._is_off_topic(message_lower):
            result.is_appropriate = False
            result.flags.append(ModerationFlag.OFF_TOPIC)
            result.severity = ModerationSeverity.LOW
            result.suggested_response = (
                "Aprecio tu interés, pero mi especialidad es ayudarte con la compra de vehículos. "
                "¿Puedo asistirte en encontrar el auto ideal para ti o resolver dudas sobre nuestros servicios?"
            )
            logger.info("Off-topic message detected")
            return result

        logger.debug("Message passed content moderation")
        return result

    def get_response_for_violation(
        self,
        moderation_result: ContentModerationResult,
        violation_count: int = 1
    ) -> str:
        """
        Genera un mensaje de respuesta apropiado basado en el tipo y severidad de la violación
        así como el número de violaciones acumuladas del usuario.
        
        Args:
            moderation_result: Resultado de la moderación
            violation_count: Número de violaciones acumuladas
            
        Returns:
            Mensaje de respuesta apropiado
        """
        if violation_count == 1 and moderation_result.suggested_response:
            return moderation_result.suggested_response

        if violation_count >= 2 and moderation_result.severity.value >= ModerationSeverity.MEDIUM.value:
            return (
                "No puedo continuar esta conversación. "
                "Si necesitas asistencia para comprar un vehículo en el futuro, "
                "estaremos disponibles. Que tengas buen día."
            )

        return moderation_result.suggested_response or (
            "Lo siento, no puedo ayudarte con eso. "
            "¿Hay algo relacionado con vehículos en lo que pueda asistirte?"
        )

    def _contains_keywords(self, message: str, keywords: Set[str]) -> bool:
        """
        Verifica si un mensaje contiene alguna de las palabras clave proporcionadas.
        
        Args:
            message: Mensaje a verificar
            keywords: Conjunto de palabras clave
            
        Returns:
            True si contiene alguna palabra clave
        """
        return any(keyword.lower() in message for keyword in keywords)

    def _contains_offensive_language(self, message: str) -> bool:
        """
        Detecta lenguaje ofensivo o insultos en el mensaje.
        
        Args:
            message: Mensaje a verificar
            
        Returns:
            True si contiene lenguaje ofensivo
        """
        offensive_patterns = [
            r"\bidiot[ao]s?\b",
            r"\best[úu]pid[ao]s?\b",
            r"\bimbécil(es)?\b",
            r"\bpend[e]j[ao]s?\b",
            r"\bmierda\b",
            r"\bcarajo\b",
            r"\bchingar",
            r"\bputas?\b",
        ]

        return any(re.search(pattern, message, re.IGNORECASE) for pattern in offensive_patterns)

    def _is_off_topic(self, message: str) -> bool:
        """
        Determina si el mensaje está fuera del contexto del negocio de venta de vehículos.
        Evalúa temas ajenos, preguntas personales y solicitudes de tareas no relacionadas.
        
        Args:
            message: Mensaje a verificar
            
        Returns:
            True si está fuera del tema
        """
        has_off_topic_keywords = self._contains_keywords(message, self.OFF_TOPIC_PATTERNS)
        has_business_keywords = self._contains_keywords(message, self.BUSINESS_KEYWORDS)

        personal_question_patterns = [
            r"cómo estás",
            r"qué haces",
            r"tienes novi[ao]",
            r"cuántos años tienes",
            r"dónde vives",
            r"te gusta",
            r"qué opinas de",
            r"cuéntame de ti",
            r"háblame de"
        ]

        is_personal_question = any(
            re.search(pattern, message, re.IGNORECASE)
            for pattern in personal_question_patterns
        )

        if has_off_topic_keywords and not has_business_keywords:
            return True

        if is_personal_question and not has_business_keywords:
            return True

        if len(message) < 50:
            task_request_patterns = [
                r"traduc(e|ir|ción)",
                r"resuelve.*problema",
                r"ayúdame con.*tarea",
                r"escribe.*ensayo",
                r"dame.*receta",
                r"dime.*chiste"
            ]

            if any(re.search(pattern, message, re.IGNORECASE) for pattern in task_request_patterns):
                return True

        return False

