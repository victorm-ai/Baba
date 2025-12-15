# Guía de Instalación y Configuración

## Instalación

### 1. Requisitos Previos

- Python 3.11 o superior
- pip (gestor de paquetes de Python)
- Git (opcional)

### 2. Clonar o Descargar el Proyecto

```bash
cd "F:\27 Kavak\baba-chatbot-python"
```

### 3. Crear Entorno Virtual

```bash
# Crear entorno virtual
python -m venv venv

# Activar entorno virtual
# En Windows:
venv\Scripts\activate

# En Linux/Mac:
source venv/bin/activate
```

### 4. Instalar Dependencias

```bash
pip install -r requirements.txt
```

### 5. Configurar Variables de Entorno

Copiar el archivo de ejemplo y configurar:

```bash
copy .env.example .env
```

Editar el archivo `.env` con tus credenciales:

```env
# Twilio
TWILIO_ACCOUNT_SID=ACab624f868560c544ca5c6a29ba17d558
TWILIO_AUTH_TOKEN=tu_auth_token_real
TWILIO_PHONE_NUMBER=whatsapp:+5213314857864

# OpenAI
OPENAI_API_KEY=sk-tu-api-key-real
OPENAI_MODEL=gpt-4o-mini
OPENAI_TEMPERATURE=0.7

# Catálogo
CATALOG_FILE_PATH=./data/catalog/cars_extract.json
CATALOG_CSV_FILE_PATH=F:/27 Kavak/baba-chatbot-net/src/Baba.Chatbot.Integrations/Catalog/sample_caso_ai_engineer.csv
```

### 6. Verificar Archivos de Configuración

Asegurarse de que existen los siguientes directorios y archivos:

- `config/prompts/system.md`
- `config/prompts/guardrails.md`
- `config/prompts/response-style.md`
- `config/prompts/value-prop.md`
- `config/rag/kb_sources/kavak-website-info.md`
- `data/catalog/` (directorio para archivos de catálogo)

## Ejecutar la Aplicación

### Opción 1: Usando el script run.py

```bash
python run.py
```

### Opción 2: Usando uvicorn directamente

```bash
# Desde la raíz del proyecto
uvicorn baba_chatbot.api.main:app --reload --host 0.0.0.0 --port 8000
```

### Opción 3: Desde el módulo API

```bash
cd src/baba_chatbot/api
python main.py
```

## Verificar la Instalación

1. Abrir navegador en: `http://localhost:8000`
2. Deberías ver un mensaje JSON con información de la API
3. Acceder a la documentación: `http://localhost:8000/docs`
4. Verificar salud: `http://localhost:8000/health`

## Probar con Swagger UI

1. Ir a `http://localhost:8000/docs`
2. Expandir el endpoint `/v1/webhook/twilio/incoming`
3. Hacer clic en "Try it out"
4. Llenar los campos:
   - `From`: `whatsapp:+5213312345678`
   - `Body`: `Hola, quiero comprar un auto`
5. Hacer clic en "Execute"
6. Ver la respuesta del chatbot

## Solución de Problemas

### Error: Module not found

```bash
# Asegurarse de que PYTHONPATH incluye src/
export PYTHONPATH="${PYTHONPATH}:${PWD}/src"

# En Windows PowerShell:
$env:PYTHONPATH = "$PWD\src"
```

### Error: OpenAI API Key not configured

- Verificar que `.env` existe y contiene `OPENAI_API_KEY`
- Asegurarse de que el valor es válido (empieza con `sk-`)

### Error: Prompt file not found

- Verificar que los archivos en `config/prompts/` existen
- Copiarlos desde el proyecto original si es necesario

### Error: Knowledge base directory not found

- Verificar que `config/rag/kb_sources/` existe
- Crear el directorio si no existe: `mkdir -p config/rag/kb_sources`

## Deployment

### Docker

```bash
# Construir imagen
docker build -t baba-chatbot-python .

# Ejecutar contenedor
docker run -p 8000:8000 --env-file .env baba-chatbot-python
```

### Producción

Para producción, se recomienda:

1. Usar Gunicorn con workers de Uvicorn:
```bash
pip install gunicorn
gunicorn baba_chatbot.api.main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

2. Configurar variables de entorno de manera segura (no usar .env en producción)

3. Usar un proxy inverso (nginx) para HTTPS

4. Configurar logs apropiados

5. Implementar health checks y monitoring

