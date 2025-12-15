# Troubleshooting Guide

## Problemas Comunes

### 1. API no responde / Error 500

#### Síntomas
- Webhook de Twilio timeout
- Error 500 en logs
- API no responde

#### Diagnóstico
```bash
# Verificar si el servicio está corriendo
systemctl status babachatbot  # Linux
Get-Service BabaChatbot       # Windows

# Revisar logs
tail -n 100 /var/log/babachatbot/api.log

# Health check
curl http://localhost:5000/health
```

#### Soluciones

**A. Servicio caído**
```bash
sudo systemctl restart babachatbot
```

**B. Error de conexión a base de datos**
```bash
# Verificar conectividad
psql -h localhost -U babachatbot -d babachatbot

# Verificar connection string en appsettings
cat src/Baba.Chatbot.Api/appsettings.Production.json | grep ConnectionStrings
```

**C. Puerto en uso**
```bash
# Linux: encontrar proceso usando puerto 5000
sudo lsof -i :5000

# Windows
netstat -ano | findstr :5000

# Matar proceso o cambiar puerto en appsettings
```

---

### 2. LLM no responde / Respuestas lentas

#### Síntomas
- Timeout al generar respuestas
- Mensajes de "Estoy pensando..." sin respuesta
- Alta latencia

#### Diagnóstico
```bash
# Verificar Ollama/LLM server
curl http://localhost:11434/api/tags

# Verificar uso de recursos
top  # Linux
Get-Process | Sort-Object CPU -Descending | Select-Object -First 10  # Windows
```

#### Soluciones

**A. Ollama no está corriendo**
```bash
# Iniciar Ollama
ollama serve &

# Verificar modelo descargado
ollama list
```

**B. Modelo no descargado**
```bash
ollama pull llama2
# o el modelo configurado en appsettings
```

**C. Recursos insuficientes**
- Modelo muy grande para RAM disponible
- Considerar modelo más pequeño (ej: `llama2:7b` en vez de `llama2:13b`)
- Aumentar RAM del servidor
- Configurar swap si es necesario

**D. Timeout muy corto**
```json
// appsettings.json
{
  "Llm": {
    "RequestTimeout": 60,  // Aumentar a 60 segundos
    "MaxTokens": 500       // Reducir tokens si es necesario
  }
}
```

---

### 3. Twilio Webhook Validation Failed

#### Síntomas
- Error 401 en webhook
- Logs: "Invalid Twilio signature"
- Mensajes no llegan a la API

#### Diagnóstico
```bash
# Verificar configuración de Twilio
cat src/Baba.Chatbot.Api/appsettings.Production.json | grep Twilio
```

#### Soluciones

**A. Auth Token incorrecto**
- Verificar en Twilio Console → Account → API credentials
- Actualizar `appsettings.Production.json`:
```json
{
  "Twilio": {
    "AccountSid": "ACxxxx",
    "AuthToken": "tu_token_correcto"
  }
}
```

**B. URL pública incorrecta**
- Twilio necesita HTTPS
- Verificar que el webhook en Twilio Console apunte a tu dominio público
- Ejemplo: `https://tu-dominio.com/v1/webhook/twilio/incoming`

**C. Reverse proxy mal configurado**
```nginx
# Asegurar que headers se propaguen correctamente
location /v1/webhook/twilio/ {
    proxy_pass http://localhost:5000;
    proxy_set_header X-Twilio-Signature $http_x_twilio_signature;
    proxy_set_header Host $host;
}
```

---

### 4. RAG / Vector Store no encuentra contexto

#### Síntomas
- Respuestas genéricas sin contexto específico
- Logs: "No relevant documents found"
- Recomendaciones de vehículos incorrectas

#### Diagnóstico
```bash
# Verificar si índice existe
ls -la data/rag_index/

# Verificar logs de RAG
grep "RAG" /var/log/babachatbot/api.log
```

#### Soluciones

**A. Índice no generado**
```bash
# Ejecutar script de ingesta
cd ops/scripts
./seed-catalog.sh

# O manualmente con Worker
cd src/Baba.Chatbot.Worker
dotnet run --ingest
```

**B. Path incorrecto**
```json
// appsettings.json
{
  "Rag": {
    "VectorStorePath": "/opt/babachatbot/data/rag_index",  // Path absoluto
    "Enabled": true
  }
}
```

**C. Permisos insuficientes**
```bash
# Dar permisos de lectura
sudo chown -R www-data:www-data /opt/babachatbot/data/rag_index/
sudo chmod -R 755 /opt/babachatbot/data/rag_index/
```

---

### 5. Catálogo de vehículos vacío

#### Síntomas
- "No tenemos vehículos disponibles"
- Búsquedas no retornan resultados

#### Diagnóstico
```bash
# Verificar archivo de catálogo
cat data/catalog/cars_extract.json | jq length

# O base de datos
psql -d babachatbot -c "SELECT COUNT(*) FROM vehicles;"
```

#### Soluciones

**A. Archivo vacío o no existe**
```bash
# Copiar datos de ejemplo
cp data/catalog/cars_extract.example.json data/catalog/cars_extract.json

# O descargar desde fuente
wget -O data/catalog/cars_extract.json https://tu-fuente.com/catalog.json
```

**B. Ejecutar seed**
```bash
cd ops/scripts
./seed-catalog.sh
```

**C. Error en parsing JSON**
- Validar JSON: `cat data/catalog/cars_extract.json | jq .`
- Corregir formato si hay errores

---

### 6. Alto uso de memoria

#### Síntomas
- OOM (Out of Memory) errors
- Servicio se reinicia frecuentemente
- Lentitud general

#### Diagnóstico
```bash
# Linux
free -h
ps aux | grep Baba.Chatbot

# Windows
Get-Process Baba.Chatbot.Api | Select-Object WorkingSet64
```

#### Soluciones

**A. Limitar cache en memoria**
```json
{
  "Caching": {
    "MaxSize": 100,  // MB
    "ExpirationMinutes": 60
  }
}
```

**B. Configurar GC**
```json
{
  "System.GC.Server": false,  // Usar workstation GC
  "System.GC.Concurrent": true
}
```

**C. Usar cache externo (Redis)**
```bash
# Instalar Redis
sudo apt install redis-server

# Configurar en appsettings
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

---

## Logs y Diagnóstico

### Ubicación de Logs
- **Linux**: `/var/log/babachatbot/`
- **Windows**: `C:\BabaChatbot\logs\`
- **Docker**: `docker logs baba-chatbot-api`

### Aumentar nivel de log
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Baba.Chatbot": "Debug",  // Más detalle
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Comandos útiles
```bash
# Logs en tiempo real
tail -f /var/log/babachatbot/api.log

# Buscar errores
grep "ERROR" /var/log/babachatbot/api.log

# Últimos 100 errores
grep "ERROR" /var/log/babachatbot/api.log | tail -100

# Filtrar por timestamp
awk '/2024-01-15T10:00:00/,/2024-01-15T11:00:00/' /var/log/babachatbot/api.log
```

---

## Contacto de Soporte

Si los problemas persisten:

1. Recopilar información:
   ```bash
   # Generar reporte de diagnóstico
   ./ops/scripts/diagnostic-report.sh > diagnostic-$(date +%Y%m%d).txt
   ```

2. Incluir:
   - Logs relevantes
   - Configuración (sin credenciales)
   - Pasos para reproducir
   - Versión del software

3. Enviar a: support@babachatbot.com

