"""
Script para ejecutar la aplicación.
"""
import uvicorn

if __name__ == "__main__":
    uvicorn.run(
        "baba_chatbot.api.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True
    )

