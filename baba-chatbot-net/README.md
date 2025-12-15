# Baba Chatbot

Chatbot inteligente para asistencia de ventas de vehículos mediante WhatsApp, con sistema avanzado de guardrails y moderación de contenido.

## 📊 Estado del Proyecto

**Fecha de actualización:** 14 de diciembre de 2025  
**Estado:** ✅ Funcional - Listo para pruebas y refinamiento  
**Versión:** 1.0.0-beta

### ✨ Características Implementadas

- ✅ **API REST** con ASP.NET Core 8.0
- ✅ **Integración con Twilio** para WhatsApp
- ✅ **Cliente LLM** (compatible con OpenAI/Ollama)
- ✅ **Catálogo de vehículos** con búsqueda semántica
- ✅ **Sistema RAG** (Retrieval-Augmented Generation)
- ✅ **Guardrails y moderación de contenido** completo
- ✅ **Sistema de orquestación** de conversaciones
- ✅ **Validación de PII** (Información Personal Identificable)
- ✅ **Sistema de reincidencia** (3 strikes)
- ✅ **Documentación Swagger/OpenAPI**

## 🏗️ Arquitectura

Este proyecto sigue los principios de **Clean Architecture** y **DDD** (Domain-Driven Design).

### Estructura de Proyectos

```
src/
├── Baba.Chatbot.Api/              # 🌐 Punto de entrada, API REST, controladores
│   ├── Controllers/               #    - TwilioWebhookController
│   ├── Extensions/                #    - Inyección de dependencias
│   └── Program.cs                 #    - Configuración de la aplicación
│
├── Baba.Chatbot.Application/      # 🧠 Casos de uso y lógica de negocio
│   ├── Conversation/              
│   │   ├── Guardrails/            #    - Moderación y validación
│   │   └── Orchestrator/          #    - Orquestación de conversaciones
│   └── Abstractions/              #    - Interfaces
│
├── Baba.Chatbot.Domain/           # 💎 Entidades y value objects
│   ├── Entities/                  #    - Vehicle
│   └── ValueObjects/              #    - VehicleQuery
│
└── Baba.Chatbot.Integrations/     # 🔌 Integraciones externas
    ├── Catalog/                   #    - Repositorio de vehículos
    ├── Llm/                       #    - Cliente LLM y RAG
    └── Twilio/                    #    - Cliente Twilio
```

### 🛡️ Sistema de Guardrails

El chatbot incluye un sistema completo de moderación y validación:

| Componente | Funcionalidad |
|------------|---------------|
| **ContentModerator** | Detecta discurso de odio, violencia, contenido sexual, lenguaje ofensivo y temas off-topic |
| **GuardrailsValidator** | Valida mensajes, detecta PII, promesas no autorizadas e información inventada |
| **ConversationOrchestrator** | Orquesta el flujo completo con sistema de reincidencia (3 strikes) |

**Documentación detallada:**
- [Guía completa de Guardrails](docs/GUARDRAILS_GUIDE.md)
- [Guía rápida](GUARDRAILS_README.md)
- [Resumen de implementación](RESUMEN_GUARDRAILS.md)
- [Ejemplos prácticos](EJEMPLOS_GUARDRAILS.md)

## 🚀 Inicio Rápido

### Requisitos

- **.NET 8.0 SDK** o superior
- **Docker** (opcional, para contenedores)
- **Ollama** o endpoint compatible con OpenAI (para LLM)
- **Twilio Account** (para integración WhatsApp)

### Instalación

```bash
# Clonar el repositorio
git clone <repository-url>
cd baba-chatbot-net

# Restaurar dependencias
dotnet restore

# Compilar la solución
dotnet build

# Ejecutar la API
cd src/Baba.Chatbot.Api
dotnet run
```

### Probar con Swagger

1. Ejecutar la API: `dotnet run`
2. Abrir navegador en: `https://localhost:7xxx/swagger`
3. Ver [Guía de Swagger](QUICK_START_SWAGGER.md) para ejemplos detallados

## ⚙️ Configuración

### Archivo de Configuración

Copiar y personalizar el archivo de configuración:

```bash
cp config/appsettings.template.json src/Baba.Chatbot.Api/appsettings.json
```

### Variables de Entorno Principales

```bash
# Twilio
TWILIO_ACCOUNT_SID=your_account_sid
TWILIO_AUTH_TOKEN=your_auth_token
TWILIO_PHONE_NUMBER=+1234567890

# LLM (OpenAI/Ollama)
LLM_ENDPOINT=http://localhost:11434/v1
LLM_MODEL=llama2
LLM_API_KEY=optional_api_key

# Catálogo
CATALOG_PATH=data/catalog/cars_extract.json
```

Ver [Configuración de OpenAI](CONFIGURACION_OPENAI.md) para más detalles.

## 📚 Documentación

### Guías de Usuario

- [🚀 Quick Start con Swagger](QUICK_START_SWAGGER.md)
- [🔧 Guía de Visual Studio](GUIA_VISUAL_STUDIO.md)
- [🛡️ Guía de Guardrails](docs/GUARDRAILS_GUIDE.md)
- [➕ Agregar páginas web al RAG](AGREGAR_PAGINAS_WEB.md)

### Documentación Técnica

- **Arquitectura:** [`docs/architecture/`](docs/architecture/)
  - Diagramas C4: Context, Container, Component
- **API:** [`docs/api/openapi.yaml`](docs/api/openapi.yaml)
- **Runbooks:** [`docs/runbooks/`](docs/runbooks/)
  - [Instalación on-premise](docs/runbooks/onprem-install.md)
  - [Troubleshooting](docs/runbooks/troubleshooting.md)

### Prompts y Configuración

- [`config/prompts/system.md`](config/prompts/system.md) - Prompt del sistema
- [`config/prompts/guardrails.md`](config/prompts/guardrails.md) - Reglas de guardrails
- [`config/prompts/response-style.md`](config/prompts/response-style.md) - Estilo de respuestas
- [`config/prompts/value-prop.md`](config/prompts/value-prop.md) - Propuesta de valor

## 🧪 Testing

```bash
# Ejecutar test del catálogo
cd test
dotnet run

# O usar el script directo
dotnet script TestCatalog.csx
```

### Ejemplos de Prueba

```bash
# Probar API con archivos HTTP
# Ver: src/Baba.Chatbot.Api/test-api-examples.http
```

## 🐳 Docker

```bash
# Construir imagen
docker build -f ops/docker/Dockerfile.api -t baba-chatbot-api .

# Ejecutar con docker-compose
cd ops/docker
docker-compose up
```

## 📊 Observabilidad

- **Prometheus:** Métricas de la aplicación
- **Grafana:** Dashboards y visualización
- Configuración en [`ops/observability/`](ops/observability/)

## 🛠️ Scripts de Utilidad

```powershell
# Windows - Agregar exclusión de Windows Defender
.\add-defender-exclusion.ps1

# Windows - Desbloquear archivos
.\force-unlock.ps1

# Windows - Matar procesos bloqueados
.\kill-locked-process.ps1
```

## 📝 Changelog

Ver archivos de cambios:
- [CHANGELOG_CATALOG.md](CHANGELOG_CATALOG.md) - Historial del catálogo
- [RESUMEN_GUARDRAILS.md](RESUMEN_GUARDRAILS.md) - Implementación de guardrails

## 🤝 Contribución

Este proyecto está en desarrollo activo. Para contribuir:

1. Revisar la [estructura del proyecto](ESTRUCTURA_SIMPLIFICADA.md)
2. Seguir los principios de Clean Architecture
3. Agregar tests para nuevas funcionalidades
4. Actualizar documentación relevante

## 📞 Soporte

Para problemas o dudas:
- Revisar la [guía de troubleshooting](docs/runbooks/troubleshooting.md)
- Consultar los archivos de documentación en [`docs/`](docs/)
- Revisar ejemplos en las guías rápidas

---

**Desarrollado con ❤️ para Kavak**

