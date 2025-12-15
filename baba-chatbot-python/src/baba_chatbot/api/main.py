"""
Aplicación principal FastAPI.
Punto de entrada de la API REST para el chatbot Baba.
"""
import os
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .routes import twilio_router
from .dependencies import get_settings


# Configurar logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Gestor del ciclo de vida de la aplicación.
    """
    logger.info("Starting Baba Chatbot API...")
    yield
    logger.info("Shutting down Baba Chatbot API...")


# Crear aplicación FastAPI
app = FastAPI(
    title="Baba Chatbot API",
    description="API para el chatbot de Kavak que ayuda a los clientes a encontrar su vehículo ideal y calcular financiamiento",
    version="1.0.0",
    contact={
        "name": "Equipo Kavak",
        "email": "support@kavak.com"
    },
    lifespan=lifespan
)

# Configurar CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Registrar rutas
app.include_router(twilio_router, prefix="/v1/webhook/twilio", tags=["Twilio Webhook"])


@app.get("/", tags=["Root"])
async def root():
    """
    Endpoint raíz de la API.
    """
    return {
        "message": "Baba Chatbot API",
        "version": "1.0.0",
        "docs": "/docs"
    }


@app.get("/health", tags=["Health"])
async def health_check():
    """
    Endpoint de verificación de salud.
    """
    return {
        "status": "healthy",
        "service": "baba-chatbot-api"
    }


if __name__ == "__main__":
    import uvicorn
    
    settings = get_settings()
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    )

