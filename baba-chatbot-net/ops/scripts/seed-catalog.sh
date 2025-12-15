#!/bin/bash

# Script para cargar el catálogo de vehículos en la base de datos
# Uso: ./seed-catalog.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CATALOG_FILE="$SCRIPT_DIR/../../data/catalog/cars_extract.json"
API_PROJECT="$SCRIPT_DIR/../../src/Baba.Chatbot.Api"

echo "================================================"
echo "Seed Catalog - Baba Chatbot"
echo "================================================"

# Verificar que existe el archivo de catálogo
if [ ! -f "$CATALOG_FILE" ]; then
    echo "❌ No se encontró el archivo de catálogo: $CATALOG_FILE"
    exit 1
fi

echo "✅ Archivo de catálogo encontrado"

# Verificar que existe el proyecto
if [ ! -d "$API_PROJECT" ]; then
    echo "❌ No se encontró el proyecto API: $API_PROJECT"
    exit 1
fi

echo "✅ Proyecto API encontrado"

# Opción 1: Usar la API directamente (si está corriendo)
echo ""
echo "Intentando cargar catálogo vía API..."

API_URL="${API_URL:-http://localhost:5000}"
HEALTH_ENDPOINT="$API_URL/health"

if curl -s -f "$HEALTH_ENDPOINT" > /dev/null 2>&1; then
    echo "✅ API está disponible en $API_URL"
    
    # Aquí iría la lógica para subir el catálogo vía API
    # Por ahora, mostramos el mensaje
    echo "⚠️  Endpoint de carga de catálogo no implementado aún"
    echo "   Se requiere implementar POST /api/admin/catalog/seed"
else
    echo "⚠️  API no está disponible en $API_URL"
fi

# Opción 2: Usar EF Core para seed directo
echo ""
echo "Alternativa: Ejecutar seed con Entity Framework..."
echo ""
echo "Para cargar el catálogo manualmente:"
echo "1. Navega al proyecto API:"
echo "   cd $API_PROJECT"
echo ""
echo "2. Ejecuta el comando de seed:"
echo "   dotnet run --seed-catalog"
echo ""
echo "O implementa un DbContext seeder que lea $CATALOG_FILE"

# Opción 3: Script SQL directo (PostgreSQL)
echo ""
echo "================================================"
echo "Alternativa: Cargar directamente a PostgreSQL"
echo "================================================"

if command -v jq &> /dev/null; then
    echo "Generando script SQL..."
    
    # Generar script SQL desde JSON (ejemplo básico)
    cat "$CATALOG_FILE" | jq -r '.[] | 
        "INSERT INTO Vehicles (Id, Brand, Model, Year, Price, Mileage, Transmission, FuelType, Color, Status) 
         VALUES (\"\(.id)\", \"\(.brand)\", \"\(.model)\", \(.year), \(.price), \(.mileage), \"\(.transmission)\", \"\(.fuelType)\", \"\(.color)\", \"\(.status)\");"' \
         > /tmp/seed_vehicles.sql
    
    echo "✅ Script SQL generado en /tmp/seed_vehicles.sql"
    echo ""
    echo "Para ejecutar:"
    echo "psql -h localhost -U baba -d babachatbot -f /tmp/seed_vehicles.sql"
else
    echo "⚠️  'jq' no está instalado. No se puede generar SQL automáticamente"
    echo "   Instalar: sudo apt install jq (Linux) o brew install jq (Mac)"
fi

echo ""
echo "================================================"
echo "Proceso de seed completado (revisa opciones arriba)"
echo "================================================"

