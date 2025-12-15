"""
Cliente para integración con Twilio WhatsApp API.
"""
import logging
from typing import Optional
from twilio.rest import Client


logger = logging.getLogger(__name__)


class TwilioClient:
    """
    Cliente para enviar mensajes a través de Twilio WhatsApp API.
    """

    def __init__(
        self,
        account_sid: str,
        auth_token: str,
        phone_number: str
    ):
        """
        Inicializa el cliente de Twilio.
        
        Args:
            account_sid: SID de la cuenta de Twilio
            auth_token: Token de autenticación de Twilio
            phone_number: Número de teléfono de Twilio (formato: whatsapp:+1234567890)
        """
        self._client = Client(account_sid, auth_token)
        self._phone_number = phone_number
        logger.info("TwilioClient initialized")

    def send_message(self, to: str, body: str) -> Optional[str]:
        """
        Envía un mensaje de WhatsApp a través de Twilio.
        
        Args:
            to: Número de destino (formato: whatsapp:+1234567890)
            body: Contenido del mensaje
            
        Returns:
            SID del mensaje enviado o None si hubo error
        """
        try:
            message = self._client.messages.create(
                from_=self._phone_number,
                body=body,
                to=to
            )
            logger.info(f"Message sent to {to}, SID: {message.sid}")
            return message.sid
        except Exception as ex:
            logger.error(f"Error sending message to {to}: {ex}")
            return None

