# Baba Chatbot - Python

Chatbot inteligente para asistencia de ventas de vehículos mediante WhatsApp, con sistema avanzado de guardrails y moderación de contenido.

## 📊 Estado del Proyecto

**Fecha de migración:** 14 de diciembre de 2025  
**Estado:** ✅ Migrado a Python desde C#/.NET  
**Versión:** 1.0.0-python

### ✨ Características Implementadas

- ✅ **API REST** con FastAPI
- ✅ **Integración con Twilio** para WhatsApp
- ✅ **Cliente LLM** (compatible con OpenAI)
- ✅ **Catálogo de vehículos** con búsqueda
- ✅ **Sistema RAG** (Retrieval-Augmented Generation)
- ✅ **Guardrails y moderación de contenido** completo
- ✅ **Sistema de orquestación** de conversaciones
- ✅ **Validación de PII** (Información Personal Identificable)
- ✅ **Sistema de reincidencia** (3 strikes)
- ✅ **Documentación OpenAPI** (Swagger)

## 🏗️ Arquitectura

Este proyecto sigue los principios de **Clean Architecture** y **DDD** (Domain-Driven Design).

### Estructura de Proyectos

```
src/baba_chatbot/
├── api/                           # 🌐 API REST, controladores
│   ├── main.py                    #    - Aplicación FastAPI
│   ├── routes.py                  #    - Rutas y endpoints
│   └── dependencies.py            #    - Configuración y dependencias
│
├── application/                   # 🧠 Casos de uso y lógica de negocio
│   ├── conversation/              
│   │   ├── guardrails/            #    - Moderación y validación
│   │   └── orchestrator/          #    - Orquestación de conversaciones
│   └── abstractions/              #    - Interfaces
│
├── domain/                        # 💎 Entidades y value objects
│   ├── entities/                  #    - Vehicle
│   └── value_objects/             #    - VehicleQuery
│
└── integrations/                  # 🔌 Integraciones externas
    ├── catalog/                   #    - Repositorio de vehículos
    ├── llm/                       #    - Cliente LLM y RAG
    └── twilio/                    #    - Cliente Twilio
```

### 🛡️ Sistema de Guardrails

El chatbot incluye un sistema completo de moderación y validación:

| Componente | Funcionalidad |
|------------|---------------|
| **GuardrailsValidator** | Valida mensajes, detecta PII, promesas no autorizadas e información inventada |
| **ContentModeration** | Detecta discurso de odio, violencia, contenido sexual, lenguaje ofensivo y temas off-topic |
| **ConversationOrchestrator** | Orquesta el flujo completo con sistema de reincidencia (3 strikes) |

## 🚀 Inicio Rápido

### Requisitos

- **Python 3.11+**
- **pip** o **poetry** para gestión de dependencias
- **OpenAI API Key** (para LLM)
- **Twilio Account** (para integración WhatsApp)

### Instalación

```bash
# Clonar o navegar al proyecto
cd baba-chatbot-python

# Crear entorno virtual
python -m venv venv

# Activar entorno virtual
# Windows:
venv\Scripts\activate
# Linux/Mac:
source venv/bin/activate

# Instalar dependencias
pip install -r requirements.txt

# Configurar variables de entorno
cp .env.example .env
# Editar .env con tus credenciales
```

### Ejecutar la aplicación

```bash
# Desde el directorio raíz
cd src/baba_chatbot/api
python main.py

# O usar uvicorn directamente
uvicorn baba_chatbot.api.main:app --reload --host 0.0.0.0 --port 8000
```

### Probar con Swagger

1. Ejecutar la API: `python main.py` o `uvicorn ...`
2. Abrir navegador en: `http://localhost:8000/docs`
3. Explorar y probar los endpoints disponibles

## ⚙️ Configuración

### Variables de Entorno

Crear archivo `.env` en la raíz del proyecto:

```bash
# Twilio
TWILIO_ACCOUNT_SID=your_account_sid
TWILIO_AUTH_TOKEN=your_auth_token
TWILIO_PHONE_NUMBER=whatsapp:+1234567890

# OpenAI
OPENAI_API_KEY=sk-your-api-key
OPENAI_MODEL=gpt-4o-mini
OPENAI_TEMPERATURE=0.7

# Catálogo
CATALOG_FILE_PATH=./data/catalog/cars_extract.json
CATALOG_CSV_FILE_PATH=./path/to/sample.csv

# RAG
KNOWLEDGE_BASE_PATH=./config/rag/kb_sources
PROMPTS_PATH=./config/prompts
```

## 📚 Dependencias Principales

- **FastAPI** - Framework web moderno y rápido
- **Uvicorn** - Servidor ASGI de alto rendimiento
- **OpenAI Python SDK** - Cliente para API de OpenAI
- **Twilio Python SDK** - Cliente para API de Twilio
- **Pydantic** - Validación de datos y configuración
- **Python-dotenv** - Gestión de variables de entorno

## 🧪 Testing

```bash
# Ejecutar tests (cuando se implementen)
pytest tests/

# Con cobertura
pytest --cov=baba_chatbot tests/
```

## 📝 Diferencias con la versión C#/.NET

### Ventajas de la versión Python:

- ✅ Sintaxis más concisa y legible
- ✅ Ecosistema rico de librerías ML/AI
- ✅ FastAPI ofrece documentación automática
- ✅ Más fácil de integrar con notebooks y análisis de datos
- ✅ Deployment más sencillo en plataformas cloud

### Mantenidas del original:

- ✅ Misma arquitectura (Clean Architecture)
- ✅ Mismos principios de diseño (DDD)
- ✅ Misma funcionalidad de guardrails
- ✅ Misma integración con Twilio y OpenAI
- ✅ Mismos archivos de configuración y prompts

## 🐳 Docker

```bash
# Construir imagen (cuando se agregue Dockerfile)
docker build -t baba-chatbot-python .

# Ejecutar contenedor
docker run -p 8000:8000 --env-file .env baba-chatbot-python
```

## 📞 Soporte

Para problemas o dudas:
- Revisar la documentación en `docs/`
- Revisar logs de la aplicación
- Consultar ejemplos en las guías rápidas

---

**Desarrollado con ❤️ para Kavak**

**Migrado de C#/.NET a Python - Diciembre 2025**

