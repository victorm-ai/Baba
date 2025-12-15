#!/bin/bash

# Script para inicializar la base de datos PostgreSQL
# Uso: ./init-db.sh -h localhost -d babachatbot -u baba -p password

set -e

# Valores por defecto
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="babachatbot"
DB_USER="baba"
DB_PASSWORD=""

# Parsear argumentos
while getopts "h:P:d:u:p:" opt; do
  case $opt in
    h) DB_HOST="$OPTARG" ;;
    P) DB_PORT="$OPTARG" ;;
    d) DB_NAME="$OPTARG" ;;
    u) DB_USER="$OPTARG" ;;
    p) DB_PASSWORD="$OPTARG" ;;
    \?) echo "Uso: $0 -h host -P port -d database -u user -p password" >&2
        exit 1 ;;
  esac
done

echo "================================================"
echo "Inicializando Base de Datos"
echo "================================================"
echo "Host: $DB_HOST:$DB_PORT"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo "================================================"

# Verificar si PostgreSQL está disponible
if ! command -v psql &> /dev/null; then
    echo "❌ PostgreSQL client (psql) no está instalado"
    exit 1
fi

# Configurar variable de entorno para password si se proporcionó
if [ -n "$DB_PASSWORD" ]; then
    export PGPASSWORD="$DB_PASSWORD"
fi

# Verificar conexión
echo "Verificando conexión a PostgreSQL..."
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c '\q' 2>/dev/null; then
    echo "✅ Conexión exitosa"
else
    echo "❌ No se pudo conectar a PostgreSQL"
    exit 1
fi

# Crear base de datos si no existe
echo "Creando base de datos si no existe..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres <<EOF
SELECT 'CREATE DATABASE $DB_NAME'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DB_NAME')\gexec
EOF

echo "✅ Base de datos creada/verificada"

# Crear extensiones necesarias
echo "Creando extensiones..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" <<EOF
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
EOF

echo "✅ Extensiones creadas"

# Ejecutar migraciones con Entity Framework
API_PROJECT_PATH="$(dirname "$0")/../../src/Baba.Chatbot.Api"

if [ -d "$API_PROJECT_PATH" ]; then
    echo "Ejecutando migraciones de Entity Framework..."
    cd "$API_PROJECT_PATH"
    
    if dotnet ef database update --verbose; then
        echo "✅ Migraciones aplicadas exitosamente"
    else
        echo "❌ Error al aplicar migraciones"
        exit 1
    fi
else
    echo "⚠️  No se encontró el proyecto API en: $API_PROJECT_PATH"
    echo "   Las migraciones deben ejecutarse manualmente"
fi

echo ""
echo "================================================"
echo "✅ Inicialización completada"
echo "================================================"
echo "Connection String:"
echo "Server=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;User Id=$DB_USER;Password=***"
echo "================================================"

# Limpiar variable de password
unset PGPASSWORD

