# Catálogo de Vehículos - Integración

Este módulo gestiona la carga y consulta del catálogo de vehículos disponibles.

## Archivos Soportados

### 1. CSV (`sample_caso_ai_engineer.csv`)
Formato de archivo CSV con los siguientes campos:
- `stock_id`: Identificador único del vehículo
- `km`: Kilometraje
- `price`: Precio en pesos
- `make`: Marca del vehículo
- `model`: Modelo
- `year`: Año
- `version`: Versión/trim del vehículo
- `bluetooth`: Disponibilidad de Bluetooth (NA si no aplica)
- `largo`: Longitud del vehículo en mm
- `ancho`: Ancho del vehículo en mm
- `altura`: Altura del vehículo en mm
- `car_play`: Disponibilidad de CarPlay (NA si no aplica)

### 2. JSON (Opcional)
También soporta carga desde archivos JSON con el formato legacy.

## Configuración

En `appsettings.json`:

```json
{
  "Catalog": {
    "FilePath": "./data/catalog/cars_extract.json",
    "CsvFilePath": "./src/Baba.Chatbot.Integrations/Catalog/sample_caso_ai_engineer.csv"
  }
}
```

## Uso en el Chatbot

El catálogo se integra automáticamente con el chatbot a través de **Function Calling** de OpenAI.

### Herramientas Disponibles

#### 1. `search_vehicles`
Busca vehículos según criterios específicos.

**Parámetros:**
- `brand`: Marca del vehículo (ej: "Toyota", "Honda")
- `model`: Modelo del vehículo (ej: "Corolla", "Civic")
- `min_price`: Precio mínimo en pesos
- `max_price`: Precio máximo en pesos
- `min_year`: Año mínimo
- `max_year`: Año máximo
- `max_mileage`: Kilometraje máximo

**Ejemplo de conversación:**
```
Usuario: "¿Tienes Honda Civic disponibles?"
Baba: [Llama a search_vehicles con brand="Honda", model="Civic"]
      "¡Sí! Tengo 2 Honda Civic disponibles:
       
       🚗 Honda Civic 2013 - $192,999
          • 93,481 km
          • 4555mm de largo
       
       ¿Te gustaría más información sobre alguno?"
```

#### 2. `get_vehicle_details`
Obtiene detalles completos de un vehículo específico.

**Parámetros:**
- `vehicle_id`: ID o stock_id del vehículo

**Ejemplo de conversación:**
```
Usuario: "Dime más sobre el vehículo 299048"
Baba: [Llama a get_vehicle_details con vehicle_id="299048"]
      "Claro, aquí están los detalles del Honda Civic:
       
       📋 Especificaciones:
       • Año: 2013
       • Precio: $192,999
       • Kilometraje: 93,481 km
       • Versión: 1.8 EX-L AT 4DRS
       
       📐 Dimensiones:
       • Largo: 4555 mm
       • Ancho: 1755 mm
       • Alto: 1450 mm
       
       ¿Te interesa agendar una prueba de manejo?"
```

## API REST

También se exponen endpoints REST para consultar el catálogo:

### Buscar vehículos
```http
GET /v1/catalog/search?brand=Toyota&maxPrice=300000
```

### Obtener vehículo por ID
```http
GET /v1/catalog/243587
```

## Caché

El catálogo se carga una vez en memoria y se mantiene en caché durante la ejecución de la aplicación. Para recargar el catálogo, reinicia la aplicación.

## Extensión

Para agregar nuevos campos al catálogo:

1. Actualiza la entidad `Vehicle` en `Domain/Entities/Vehicle.cs`
2. Actualiza el método `LoadFromCsvAsync` en `CatalogRepository.cs`
3. Actualiza las herramientas en `LlmClient.cs` para incluir los nuevos campos
