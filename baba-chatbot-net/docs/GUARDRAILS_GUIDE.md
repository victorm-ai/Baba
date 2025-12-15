# Sistema de Guardrails y Moderación de Contenido

## Descripción General

Este documento describe el sistema de **Guardrails** implementado para el chatbot Baba de Kavak. El sistema protege al negocio y a los usuarios filtrando contenido inapropiado y manteniendo las conversaciones dentro del alcance del negocio automotriz.

## Componentes Principales

### 1. ContentModerator
Servicio que detecta y filtra contenido inapropiado en los mensajes de los usuarios.

**Ubicación**: `src/Baba.Chatbot.Application/Conversation/Guardrails/ContentModerator.cs`

**Funcionalidades**:
- ✅ Detección de discurso de odio
- ✅ Detección de contenido violento
- ✅ Detección de contenido sexual/erótico
- ✅ Detección de lenguaje ofensivo
- ✅ Detección de temas fuera del negocio (off-topic)

**Ejemplo de uso**:
```csharp
var moderator = new ContentModerator(logger);
var result = moderator.ModerateContent("mensaje del usuario");

if (!result.IsAppropriate)
{
    Console.WriteLine($"Contenido inapropiado detectado: {string.Join(", ", result.Flags)}");
    Console.WriteLine($"Respuesta sugerida: {result.SuggestedResponse}");
}
```

### 2. GuardrailsValidator
Valida tanto mensajes de entrada como respuestas del LLM.

**Ubicación**: `src/Baba.Chatbot.Application/Conversation/Guardrails/GuardrailsValidator.cs`

**Funcionalidades**:
- ✅ Validación de mensajes de usuarios
- ✅ Validación de respuestas del LLM
- ✅ Detección y enmascaramiento de PII (información personal)
- ✅ Detección de promesas no autorizadas
- ✅ Detección de información inventada sobre vehículos
- ✅ Validación de calidad de respuestas

**Ejemplo de uso**:
```csharp
var validator = new GuardrailsValidator(logger, contentModerator);

// Validar entrada del usuario
var inputValidation = validator.ValidateUserInput(userMessage);

// Validar respuesta del LLM
var responseValidation = await validator.ValidateResponseAsync(llmResponse);

if (!responseValidation.IsValid)
{
    Console.WriteLine($"Respuesta inválida. Violaciones: {string.Join(", ", responseValidation.Violations)}");
}

// Usar contenido limpio (con PII enmascarado)
var cleanResponse = responseValidation.CleanedContent;
```

### 3. ConversationOrchestrator
Orquesta el flujo completo de conversación con validaciones integradas.

**Ubicación**: `src/Baba.Chatbot.Application/Conversation/Orchestrator/ConversationOrchestrator.cs`

**Funcionalidades**:
- ✅ Procesa mensajes con validación automática
- ✅ Gestiona contador de violaciones por usuario
- ✅ Determina cuándo escalar a humano
- ✅ Retorna respuestas apropiadas según el contexto

**Ejemplo de uso**:
```csharp
var orchestrator = new ConversationOrchestrator(llmClient, validator, moderator, logger);

var result = await orchestrator.ProcessMessageAsync(
    userId: "whatsapp:+5215551234567",
    userMessage: "Hola, quiero comprar un auto",
    systemPrompt: systemPrompt,
    conversationHistory: null
);

if (result.Success)
{
    // Enviar respuesta al usuario
    await SendMessage(result.Message);
}
else if (result.RequiresEscalation)
{
    // Escalar a agente humano
    await EscalateToHuman(userId);
}
```

## Tipos de Validación

### 1. Contenido de Odio
**Detecta**: Discriminación racial, étnica, religiosa, por género, orientación sexual, etc.

**Respuesta**:
> "No puedo continuar con este tipo de conversación. Si necesitas ayuda con la compra de un vehículo, estaré encantado de asistirte."

**Severidad**: Alta (termina conversación)

### 2. Contenido Violento
**Detecta**: Amenazas, descripción de violencia, promoción de autolesiones.

**Respuesta**:
> "Este tipo de contenido no es apropiado para nuestra conversación. Si necesitas ayuda urgente, por favor contacta a las autoridades correspondientes."

**Severidad**: Alta (termina conversación)

### 3. Contenido Sexual/Erótico
**Detecta**: Lenguaje sexual explícito, insinuaciones inapropiadas, acoso sexual.

**Respuesta**:
> "Este tipo de conversación no es apropiada. Estoy aquí para ayudarte con la compra de vehículos. ¿Puedo asistirte con eso?"

**Severidad**: Alta (termina conversación)

### 4. Temas Fuera del Negocio (Off-Topic)
**Detecta**: Conversaciones sobre política, religión, deportes, entretenimiento, tareas escolares, etc.

**Respuesta**:
> "Aprecio tu interés, pero mi especialidad es ayudarte con la compra de vehículos. ¿Puedo asistirte en encontrar el auto ideal para ti o resolver dudas sobre nuestros servicios?"

**Severidad**: Baja (advertencia suave)

### 5. Información Personal (PII)
**Detecta y enmascara**:
- Números de tarjeta de crédito → `[TARJETA OCULTA]`
- Teléfonos no autorizados → `[TELÉFONO OCULTO]`
- Emails no autorizados → `[EMAIL OCULTO]`
- INE/CURP → `[ID OCULTO]` / `[CURP OCULTO]`

**Severidad**: Media (enmascara pero permite respuesta)

### 6. Promesas No Autorizadas
**Detecta en respuestas del LLM**:
- Descuentos no aprobados
- Garantías extendidas gratis
- Modificaciones de precio no autorizadas
- Aprobación de crédito sin evaluación

**Acción**: Requiere escalación a humano

## Sistema de Reincidencia

El sistema lleva un contador de violaciones por usuario:

| Violación | Acción |
|-----------|--------|
| **1ra vez** | Advertencia suave con redirección |
| **2da vez** | Advertencia firme |
| **3ra vez o más** | Terminar conversación y escalar |

**Ejemplo**:
```
1ra violación (off-topic): "Aprecio tu interés, pero mi especialidad es..."
2da violación (acoso): "Entiendo que puedes estar frustrado, pero necesito..."
3ra violación: "No puedo continuar esta conversación. Que tengas buen día."
```

## Configuración

Los servicios se registran automáticamente en el contenedor de inyección de dependencias:

```csharp
// En ServiceCollectionExtensions.cs
services.AddScoped<IContentModerator, ContentModerator>();
services.AddScoped<IGuardrailsValidator, GuardrailsValidator>();
services.AddScoped<ConversationOrchestrator>();
```

## Archivos de Configuración

### `config/prompts/guardrails.md`
Define las reglas de guardrails en lenguaje natural. Es un documento de referencia para el equipo.

### `config/prompts/system.md`
Incluye instrucciones para el LLM sobre qué temas puede y no puede manejar.

## Flujo de Procesamiento

```
1. Usuario envía mensaje
   ↓
2. ContentModerator valida entrada
   ↓
3. ¿Es apropiado?
   NO → Devolver respuesta de moderación
   SÍ ↓
4. LLM genera respuesta
   ↓
5. GuardrailsValidator valida respuesta
   ↓
6. Enmascarar PII si es necesario
   ↓
7. Validar calidad
   ↓
8. Enviar al usuario
```

## Logs y Monitoreo

El sistema genera logs detallados:

```csharp
// Logs de moderación
_logger.LogWarning("Hate speech detected in message");
_logger.LogWarning("User {UserId} has {ViolationCount} violations", userId, count);

// Logs de guardrails
_logger.LogWarning("Credit card number detected and masked");
_logger.LogError("LLM generated inappropriate content");

// Logs de escalación
_logger.LogWarning("Conversation requires human escalation for user {UserId}", userId);
```

### Métricas Recomendadas

- % de mensajes bloqueados por tipo de violación
- Tasa de reincidencia por usuario
- Frecuencia de escalación a humano
- False positives detectados manualmente

## Testing

### Casos de Prueba Recomendados

```csharp
// Test 1: Contenido apropiado
var result = moderator.ModerateContent("Quiero comprar un auto");
Assert.True(result.IsAppropriate);

// Test 2: Contenido de odio
var result = moderator.ModerateContent("mensaje con odio");
Assert.False(result.IsAppropriate);
Assert.Contains(ModerationFlag.HateSpeech, result.Flags);

// Test 3: Off-topic
var result = moderator.ModerateContent("¿Cuál es tu receta favorita?");
Assert.False(result.IsAppropriate);
Assert.Contains(ModerationFlag.OffTopic, result.Flags);

// Test 4: PII enmascarado
var validator = new GuardrailsValidator(logger, moderator);
var result = await validator.ValidateResponseAsync("Tu tarjeta es 1234-5678-9012-3456");
Assert.Contains("[TARJETA OCULTA]", result.CleanedContent);
```

## Extensibilidad

### Agregar Nuevas Palabras Clave

Edita los `HashSet` en `ContentModerator.cs`:

```csharp
private static readonly HashSet<string> _businessKeywords = new()
{
    "auto", "carro", "vehículo",
    // Agregar más palabras...
    "nuevo-termino"
};
```

### Agregar Nuevos Patrones de PII

Edita los `Regex` en `GuardrailsValidator.cs`:

```csharp
private static readonly Regex _nuevoPIIPattern = new(@"patron-regex", RegexOptions.Compiled);
```

### Agregar Nuevos Flags de Moderación

Edita el enum `ModerationFlag` en `ContentModerationResult.cs`:

```csharp
public enum ModerationFlag
{
    None,
    OffTopic,
    HateSpeech,
    // ... otros ...
    NuevoFlag  // Agregar aquí
}
```

## Mejores Prácticas

1. **Revisa logs semanalmente** para detectar falsos positivos
2. **Actualiza keywords** según nuevos patrones detectados
3. **Ajusta severidades** basándote en feedback del negocio
4. **Documenta cambios** en este archivo cuando modifiques reglas
5. **Prueba cambios** con casos reales antes de desplegar

## Preguntas Frecuentes

### ¿Cómo agrego una excepción para una palabra específica?

Si una palabra clave genera falsos positivos, puedes:
1. Refinar el contexto de detección
2. Agregar lógica de exclusión específica
3. Aumentar el umbral de detección

### ¿Puedo deshabilitar temporalmente los guardrails?

No se recomienda, pero puedes:
1. Comentar el registro del servicio en `ServiceCollectionExtensions.cs`
2. Usar un flag de configuración para bypass en desarrollo

### ¿Cómo pruebo los guardrails localmente?

Usa Swagger UI:
1. Ejecuta la aplicación
2. Ve a `/swagger`
3. Usa el endpoint `POST /v1/webhook/twilio/incoming`
4. Envía diferentes tipos de mensajes

## Soporte

Para preguntas sobre este sistema, contacta al equipo de desarrollo o revisa:
- Documentación de arquitectura: `docs/architecture/`
- Tests unitarios: `tests/Baba.Chatbot.UnitTests/`
- Logs de la aplicación: nivel `Warning` y superior

---

**Última actualización**: 2025-12-14  
**Versión**: 1.0  
**Autor**: Equipo de Desarrollo Baba Chatbot
