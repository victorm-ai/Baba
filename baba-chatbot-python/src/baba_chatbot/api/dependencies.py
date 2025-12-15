"""
Configuración y dependencias de la aplicación.
"""
import os
from functools import lru_cache
from pydantic_settings import BaseSettings
from typing import Optional


class Settings(BaseSettings):
    """
    Configuración de la aplicación.
    """
    # Configuración general
    app_name: str = "Baba Chatbot"
    debug: bool = False
    
    # Twilio
    twilio_account_sid: str = ""
    twilio_auth_token: str = ""
    twilio_phone_number: str = ""
    
    # OpenAI
    openai_api_key: str = ""
    openai_model: str = "gpt-4o-mini"
    openai_temperature: float = 0.7
    
    # Catálogo
    catalog_file_path: str = "./data/catalog/cars_extract.json"
    catalog_csv_file_path: Optional[str] = None
    
    # RAG
    knowledge_base_path: str = "./config/rag/kb_sources"
    prompts_path: str = "./config/prompts"
    
    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


@lru_cache()
def get_settings() -> Settings:
    """
    Obtiene la configuración de la aplicación (singleton).
    """
    return Settings()

