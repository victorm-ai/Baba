# Runbook: Instalación On-Premise

## Requisitos Previos

### Hardware
- CPU: 4 cores mínimo (8 cores recomendado)
- RAM: 8 GB mínimo (16 GB recomendado)
- Disco: 50 GB disponibles (SSD recomendado)

### Software
- Sistema Operativo: Windows Server 2019+ / Linux (Ubuntu 20.04+)
- .NET 8.0 Runtime o SDK
- Docker (opcional pero recomendado)
- PostgreSQL 14+ o SQL Server 2019+
- Ollama o servidor LLM compatible con OpenAI API

## Pasos de Instalación

### 1. Preparación del Entorno

#### Windows (PowerShell)
```powershell
# Crear directorio de instalación
New-Item -ItemType Directory -Path "C:\BabaChatbot" -Force
cd C:\BabaChatbot

# Descargar release
Invoke-WebRequest -Uri "https://releases.babachatbot.com/latest/win-x64.zip" -OutFile "baba-chatbot.zip"
Expand-Archive -Path "baba-chatbot.zip" -DestinationPath "."
```

#### Linux (Bash)
```bash
# Crear directorio de instalación
sudo mkdir -p /opt/babachatbot
cd /opt/babachatbot

# Descargar release
wget https://releases.babachatbot.com/latest/linux-x64.tar.gz
tar -xzf linux-x64.tar.gz
```

### 2. Configuración de Base de Datos

```powershell
# Windows
.\ops\scripts\init-db.ps1 -ServerName "localhost" -DatabaseName "BabaChatbot"
```

```bash
# Linux
./ops/scripts/init-db.sh -h localhost -d babachatbot
```

### 3. Configuración del Modelo LLM

#### Instalar Ollama
```bash
# Linux/Mac
curl -fsSL https://ollama.ai/install.sh | sh

# Windows
# Descargar desde https://ollama.ai/download
```

#### Descargar modelo
```bash
ollama pull llama2
# o
ollama pull mistral
```

### 4. Configuración de Variables de Entorno

Copiar archivo de configuración template:
```bash
cp config/appsettings.template.json src/Baba.Chatbot.Api/appsettings.Production.json
```

Editar `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BabaChatbot;..."
  },
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "tu_auth_token",
    "PhoneNumber": "+1234567890"
  },
  "Llm": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2",
    "Temperature": 0.7
  },
  "Rag": {
    "VectorStorePath": "/data/rag_index",
    "TopK": 5
  }
}
```

### 5. Iniciar Servicios

#### Opción A: Docker Compose (Recomendado)
```bash
cd ops/docker
docker-compose up -d
```

#### Opción B: Servicio Nativo

**Windows (como Servicio)**
```powershell
# Crear servicio
New-Service -Name "BabaChatbot" -BinaryPathName "C:\BabaChatbot\src\Baba.Chatbot.Api\Baba.Chatbot.Api.exe" -StartupType Automatic

# Iniciar servicio
Start-Service -Name "BabaChatbot"
```

**Linux (con systemd)**
```bash
# Crear archivo de servicio
sudo nano /etc/systemd/system/babachatbot.service

# Contenido:
[Unit]
Description=Baba Chatbot API
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/babachatbot/src/Baba.Chatbot.Api
ExecStart=/opt/babachatbot/src/Baba.Chatbot.Api/Baba.Chatbot.Api
Restart=always
RestartSec=10
User=www-data

[Install]
WantedBy=multi-user.target

# Habilitar e iniciar
sudo systemctl enable babachatbot
sudo systemctl start babachatbot
```

### 6. Verificación

```bash
# Health check
curl http://localhost:5000/health

# Readiness check
curl http://localhost:5000/health/ready
```

### 7. Configurar Webhook en Twilio

1. Ir a Twilio Console → Messaging → Settings
2. Configurar Webhook URL: `https://tu-dominio.com/v1/webhook/twilio/incoming`
3. Método: POST
4. Guardar cambios

## Configuración de SSL/HTTPS

### Opción A: Nginx Reverse Proxy
```nginx
server {
    listen 443 ssl;
    server_name tu-dominio.com;

    ssl_certificate /etc/ssl/certs/tu-dominio.crt;
    ssl_certificate_key /etc/ssl/private/tu-dominio.key;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Opción B: Configuración directa en Kestrel
Ver `appsettings.Production.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:5001",
        "Certificate": {
          "Path": "/etc/ssl/certs/tu-dominio.pfx",
          "Password": "tu_password"
        }
      }
    }
  }
}
```

## Monitoreo Post-Instalación

```bash
# Ver logs
tail -f /var/log/babachatbot/api.log

# Métricas (si Prometheus está configurado)
curl http://localhost:5000/metrics
```

## Actualización

```bash
# Detener servicio
sudo systemctl stop babachatbot

# Backup
tar -czf backup-$(date +%Y%m%d).tar.gz /opt/babachatbot

# Actualizar
wget https://releases.babachatbot.com/latest/linux-x64.tar.gz
tar -xzf linux-x64.tar.gz -C /opt/babachatbot

# Reiniciar
sudo systemctl start babachatbot
```

## Rollback

```bash
# Restaurar backup
sudo systemctl stop babachatbot
tar -xzf backup-YYYYMMDD.tar.gz -C /
sudo systemctl start babachatbot
```

