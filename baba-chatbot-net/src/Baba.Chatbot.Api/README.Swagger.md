# Documentación Swagger - Baba Chatbot API

## 🚀 Acceso a Swagger UI

Una vez que la API esté en ejecución, puedes acceder a la interfaz de Swagger en:

```
http://localhost:5000/swagger
```

o

```
https://localhost:5001/swagger
```

## 📋 Características Implementadas

### 1. **Interfaz Interactiva**
- Prueba todos los endpoints directamente desde el navegador
- Visualización de esquemas de request/response
- Validación en tiempo real
- Ejemplos de uso incluidos

### 2. **Documentación Completa**
- Descripciones detalladas de cada endpoint
- Parámetros requeridos y opcionales
- Códigos de respuesta HTTP
- Modelos de datos con ejemplos

### 3. **Agrupación por Tags**
- **Health**: Endpoints de health check y readiness
- **Twilio Webhooks**: Endpoints para integración con Twilio

## 🔍 Endpoints Disponibles

### Health Checks

#### `GET /health`
Verifica el estado básico de la API.

**Respuesta de ejemplo:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-12-13T10:30:00Z"
}
```

#### `GET /health/ready`
Verifica la disponibilidad de todos los servicios dependientes.

**Respuesta de ejemplo:**
```json
{
  "status": "Ready",
  "checks": {
    "database": "Healthy",
    "llm": "Healthy",
    "vectorStore": "Healthy"
  }
}
```

### Twilio Webhooks

#### `POST /v1/webhook/twilio/incoming`
Procesa mensajes entrantes desde Twilio (SMS/WhatsApp).

**Content-Type:** `application/x-www-form-urlencoded`

**Parámetros del formulario:**
- `From`: Número de teléfono del remitente
- `To`: Número de teléfono del destinatario
- `Body`: Contenido del mensaje
- `MessageSid`: ID único del mensaje en Twilio

**Respuesta:** TwiML (XML)

#### `POST /v1/webhook/twilio/status`
Recibe actualizaciones del estado de mensajes enviados.

**Content-Type:** `application/x-www-form-urlencoded`

**Parámetros del formulario:**
- `MessageSid`: ID único del mensaje
- `MessageStatus`: Estado del mensaje (sent, delivered, read, failed, etc.)

## 🛠️ Cómo Probar los Endpoints

### Usando Swagger UI

1. **Navega a** `/swagger`
2. **Expande** el endpoint que deseas probar
3. **Click** en "Try it out"
4. **Ingresa** los parámetros requeridos
5. **Click** en "Execute"
6. **Revisa** la respuesta en la sección "Responses"

### Ejemplo: Probar Health Check

1. Ve a http://localhost:5000/swagger
2. Busca `GET /health`
3. Click en "Try it out"
4. Click en "Execute"
5. Verás la respuesta del servidor

### Ejemplo: Simular Webhook de Twilio

1. Ve a http://localhost:5000/swagger
2. Busca `POST /v1/webhook/twilio/incoming`
3. Click en "Try it out"
4. En el formulario, ingresa:
   - **From**: +521234567890
   - **To**: +525555555555
   - **Body**: Hola, quiero información sobre autos
   - **MessageSid**: SM1234567890abcdef
5. Click en "Execute"
6. Verás la respuesta TwiML

## 📦 Exportar Definición OpenAPI

Swagger genera automáticamente una especificación OpenAPI 3.0. Puedes descargarla en:

```
http://localhost:5000/swagger/v1/swagger.json
```

Esta especificación puede usarse para:
- Generar clientes en otros lenguajes
- Importar en Postman
- Documentación automática
- Testing automatizado

## 🔧 Configuración Personalizada

La configuración de Swagger se encuentra en `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Configuración de versión y metadatos
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Baba Chatbot API",
        Description = "API para el chatbot de Kavak...",
        Contact = new OpenApiContact { ... }
    });
    
    // Habilitar anotaciones
    options.EnableAnnotations();
    
    // Incluir comentarios XML
    options.IncludeXmlComments(xmlPath);
});
```

## 📝 Mejores Prácticas

### Para Desarrolladores

1. **Documenta tus endpoints** usando atributos `[SwaggerOperation]`
2. **Agrega ejemplos** con `[SwaggerResponse]`
3. **Usa comentarios XML** para documentación detallada
4. **Define modelos claros** con propiedades documentadas

### Ejemplo de Controller Documentado

```csharp
/// <summary>
/// Descripción del endpoint
/// </summary>
[HttpGet]
[SwaggerOperation(
    Summary = "Resumen corto",
    Description = "Descripción detallada",
    OperationId = "GetSomething",
    Tags = new[] { "MiTag" }
)]
[SwaggerResponse(200, "Descripción de éxito", typeof(MiModelo))]
[SwaggerResponse(404, "No encontrado")]
public IActionResult Get()
{
    // Implementación
}
```

## 🔐 Seguridad

En producción, considera:
- Limitar el acceso a Swagger usando autenticación
- Usar variables de entorno para habilitar/deshabilitar Swagger
- Implementar rate limiting
- Configurar CORS apropiadamente

## 🐛 Troubleshooting

### Swagger no carga
- Verifica que el puerto sea correcto
- Asegúrate de que la app esté corriendo
- Revisa los logs en la consola

### Endpoints no aparecen
- Verifica que los controllers tengan `[ApiController]`
- Asegúrate de que `AddControllers()` esté en `Program.cs`
- Revisa que los routes estén bien definidos

### Errores de validación
- Revisa los modelos de request
- Verifica los Content-Types
- Asegúrate de enviar todos los campos requeridos

## 📚 Referencias

- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Specification](https://swagger.io/specification/)
- [ASP.NET Core Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/web-api/)

