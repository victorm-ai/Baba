# Guía Rápida - Baba Chatbot Python

## Inicio Rápido (5 minutos)

### 1. Instalar y Configurar

```bash
# Navegar al proyecto
cd "F:\27 Kavak\baba-chatbot-python"

# Crear y activar entorno virtual
python -m venv venv
venv\Scripts\activate

# Instalar dependencias
pip install -r requirements.txt

# Copiar y configurar .env
copy .env.example .env
# Editar .env con tus credenciales
```

### 2. Ejecutar

```bash
# Opción 1: Script simple
python run.py

# Opción 2: Uvicorn con recarga automática
uvicorn baba_chatbot.api.main:app --reload
```

### 3. Probar

Abrir en el navegador:
- API: `http://localhost:8000`
- Documentación: `http://localhost:8000/docs`
- Health Check: `http://localhost:8000/health`

## Estructura del Proyecto

```
baba-chatbot-python/
├── src/baba_chatbot/           # Código fuente
│   ├── api/                    # API FastAPI
│   ├── application/            # Lógica de negocio
│   ├── domain/                 # Entidades y value objects
│   └── integrations/           # Integraciones externas
├── config/                     # Configuración
│   ├── prompts/                # Prompts del sistema
│   └── rag/                    # Base de conocimiento
├── data/                       # Datos (catálogo)
├── docs/                       # Documentación
├── tests/                      # Tests unitarios
├── requirements.txt            # Dependencias Python
├── .env.example               # Ejemplo de configuración
├── Dockerfile                 # Imagen Docker
└── README.md                  # Documentación principal
```

## Comandos Útiles

### Desarrollo

```bash
# Ejecutar con recarga automática
uvicorn baba_chatbot.api.main:app --reload --port 8000

# Ejecutar en modo debug
uvicorn baba_chatbot.api.main:app --reload --log-level debug

# Ver logs en tiempo real
tail -f logs/app.log
```

### Testing

```bash
# Ejecutar tests (cuando se implementen)
pytest

# Con cobertura
pytest --cov=baba_chatbot

# Solo un archivo
pytest tests/test_guardrails.py
```

### Docker

```bash
# Construir imagen
docker build -t baba-chatbot .

# Ejecutar contenedor
docker run -p 8000:8000 --env-file .env baba-chatbot

# Ver logs
docker logs -f <container-id>
```

## Probar el Endpoint de Twilio

### Usando Swagger UI

1. Ir a `http://localhost:8000/docs`
2. Expandir POST `/v1/webhook/twilio/incoming`
3. Click "Try it out"
4. Llenar:
   ```
   From: whatsapp:+5213312345678
   Body: Hola, quiero un auto Toyota
   ```
5. Click "Execute"

### Usando curl

```bash
curl -X POST "http://localhost:8000/v1/webhook/twilio/incoming" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "From=whatsapp%3A%2B5213312345678&Body=Hola%2C+quiero+un+auto+Toyota"
```

### Usando Postman

1. Método: POST
2. URL: `http://localhost:8000/v1/webhook/twilio/incoming`
3. Body: x-www-form-urlencoded
   - `From`: `whatsapp:+5213312345678`
   - `Body`: `Hola, quiero un auto Toyota`

## Ejemplos de Mensajes

### Mensajes Válidos

```
✅ "Hola, quiero comprar un auto"
✅ "Busco un Toyota Corolla del 2020"
✅ "¿Tienen autos con menos de 50,000 km?"
✅ "Necesito financiamiento para un vehículo"
```

### Mensajes que Activan Guardrails

```
❌ "¿Cuál es tu receta favorita?" (Off-topic)
❌ "Eres un idiota" (Lenguaje ofensivo)
❌ "¿Cuántos años tienes?" (Pregunta personal)
❌ "Traduce esto al inglés" (Tarea no relacionada)
```

## Variables de Entorno Clave

```bash
# Obligatorias
OPENAI_API_KEY=sk-...         # API key de OpenAI
TWILIO_ACCOUNT_SID=AC...      # SID de Twilio
TWILIO_AUTH_TOKEN=...         # Token de Twilio

# Opcionales
OPENAI_MODEL=gpt-4o-mini      # Modelo a usar
OPENAI_TEMPERATURE=0.7        # Temperatura (0-1)
CATALOG_CSV_FILE_PATH=...     # Ruta al CSV del catálogo
```

## Troubleshooting Rápido

### Error: Module not found

```bash
# Configurar PYTHONPATH
export PYTHONPATH="${PWD}/src"  # Linux/Mac
$env:PYTHONPATH = "$PWD\src"    # Windows PowerShell
```

### Error: OpenAI API Key

- Verificar que `.env` existe
- Verificar que `OPENAI_API_KEY` está configurada
- Verificar que la key es válida (empieza con `sk-`)

### Error: Prompts not found

```bash
# Copiar desde proyecto original
Copy-Item -Path "../baba-chatbot-net/config/prompts/*" -Destination "config/prompts/" -Recurse
```

### Puerto 8000 ocupado

```bash
# Usar otro puerto
uvicorn baba_chatbot.api.main:app --port 8001
```

## Recursos Adicionales

- **Documentación completa**: Ver `docs/INSTALACION.md`
- **Guía de migración**: Ver `docs/MIGRACION.md`
- **README principal**: Ver `README.md`
- **FastAPI docs**: https://fastapi.tiangolo.com/
- **OpenAI Python SDK**: https://github.com/openai/openai-python

## Siguiente Paso

¿Todo funcionando? 🎉

1. Probar diferentes mensajes en Swagger
2. Revisar logs para entender el flujo
3. Modificar prompts en `config/prompts/`
4. Agregar más vehículos al catálogo
5. Implementar tests unitarios

---

**¿Problemas?** Revisar `docs/INSTALACION.md` o contactar al equipo.

