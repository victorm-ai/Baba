# Ejemplos de Uso - Sistema de Guardrails

Este documento muestra ejemplos prácticos de cómo funciona el sistema de guardrails.

## 📨 Ejemplos de Mensajes y Respuestas

### ✅ Ejemplo 1: Mensaje Apropiado (Caso Normal)

**Entrada del Usuario:**
```
"Hola, estoy buscando una SUV usada con precio menor a $300,000"
```

**Flujo:**
1. ContentModerator valida → ✅ Apropiado (contiene: "SUV", "precio")
2. LLM genera respuesta
3. GuardrailsValidator valida respuesta → ✅ Sin violaciones
4. Usuario recibe respuesta

**Respuesta del Bot:**
```
¡Hola! 👋 Encantado de ayudarte. Tenemos excelentes opciones de SUVs en tu rango de presupuesto.

Basándome en tus criterios, te recomiendo:

🚗 Honda CR-V 2020 - $285,000
   • 45,000 km
   • Automático, AWD, cámara reversa

🚗 Mazda CX-5 2019 - $295,000  
   • 38,000 km
   • Automático, sensor de punto ciego

¿Te gustaría conocer más detalles de alguno?
```

---

### ❌ Ejemplo 2: Contenido de Odio (Alta Severidad)

**Entrada del Usuario:**
```
"No quiero tratar con vendedores [término discriminatorio]"
```

**Flujo:**
1. ContentModerator detecta → ❌ Discurso de odio (Flag: HateSpeech)
2. Severidad: Alta
3. **Bloqueo inmediato**, no llega al LLM
4. Contador de violaciones: 1

**Respuesta del Bot:**
```
"No puedo continuar con este tipo de conversación. Si necesitas ayuda con la compra de un vehículo, estaré encantado de asistirte."
```

---

### ⚠️ Ejemplo 3: Tema Fuera del Negocio (Baja Severidad)

**Entrada del Usuario:**
```
"¿Cuál es tu receta favorita de tacos?"
```

**Flujo:**
1. ContentModerator detecta → ⚠️ Off-topic (contiene: "receta", "tacos")
2. Severidad: Baja
3. No contiene palabras de negocio automotriz
4. Contador de violaciones: 0 (solo advertencia)

**Respuesta del Bot:**
```
"Aprecio tu interés, pero mi especialidad es ayudarte con la compra de vehículos. ¿Puedo asistirte en encontrar el auto ideal para ti o resolver dudas sobre nuestros servicios?"
```

---

### ⚠️ Ejemplo 4: Lenguaje Ofensivo (Media Severidad)

**Entrada del Usuario (Primera vez):**
```
"Eres un idiota, dame el precio del Civic ya"
```

**Flujo:**
1. ContentModerator detecta → ⚠️ Harassment (contiene insultos)
2. Severidad: Media
3. Contador de violaciones: 1
4. Intenta redireccionar

**Respuesta del Bot:**
```
"Entiendo que puedes estar frustrado, pero necesito que mantengamos una conversación respetuosa para poder ayudarte. ¿Cómo puedo asistirte hoy con la compra de un vehículo?"
```

**Entrada del Usuario (Segunda vez con insultos):**
```
"[Más insultos]"
```

**Flujo:**
1. Contador de violaciones: 2
2. Severidad: Media → Alta (reincidencia)
3. Advertencia firme

**Respuesta del Bot:**
```
"No puedo continuar esta conversación. Si necesitas asistencia para comprar un vehículo en el futuro, estaremos disponibles. Que tengas buen día."
```

---

### 🛡️ Ejemplo 5: PII Detectado en Respuesta del LLM

**Entrada del Usuario:**
```
"¿Cómo puedo pagar?"
```

**Supongamos que el LLM genera (por error):**
```
"Puedes pagar con tarjeta 1234-5678-9012-3456 o transferir a contacto@example.com"
```

**Flujo:**
1. Usuario envía mensaje → ✅ Apropiado
2. LLM genera respuesta
3. GuardrailsValidator detecta:
   - ❌ Tarjeta de crédito
   - ❌ Email no autorizado
4. Enmascara automáticamente

**Usuario Recibe:**
```
"Puedes pagar con tarjeta [TARJETA OCULTA] o transferir a [EMAIL OCULTO]"
```

**Log generado:**
```
[Warning] Credit card number detected and masked
[Warning] Unauthorized email detected and masked
```

---

### 🚫 Ejemplo 6: Promesa No Autorizada

**Entrada del Usuario:**
```
"¿Me puedes dar un descuento?"
```

**Supongamos que el LLM genera:**
```
"¡Claro! Te garantizo un descuento del 20% en este auto solo para ti."
```

**Flujo:**
1. Usuario envía mensaje → ✅ Apropiado
2. LLM genera respuesta
3. GuardrailsValidator detecta:
   - ❌ "te garantizo" (promesa no autorizada)
   - ❌ "descuento del 20%" (descuento específico no aprobado)
   - ❌ "solo para ti" (precio especial no autorizado)
4. Requiere escalación a humano

**Usuario Recibe:**
```
"Déjame conectarte con un asesor especializado que podrá ayudarte mejor con opciones de financiamiento. Dame un momento..."
```

---

### ⚠️ Ejemplo 7: Sistema de Reincidencia (3 Strikes)

**Strike 1 - Off-topic:**
```
Usuario: "¿Qué tiempo hará mañana?"
Bot: "Aprecio tu interés, pero mi especialidad es ayudarte con vehículos..."
```

**Strike 2 - Lenguaje ofensivo:**
```
Usuario: "[insulto leve]"
Bot: "Entiendo que puedes estar frustrado, pero necesito que mantengamos una conversación respetuosa..."
```

**Strike 3 - Contenido violento:**
```
Usuario: "[amenaza]"
Bot: "No puedo continuar esta conversación. Si necesitas asistencia para comprar un vehículo en el futuro, estaremos disponibles. Que tengas buen día."
```

**Resultado:** Conversación terminada, usuario marcado para escalación.

---

### ✅ Ejemplo 8: Mensaje con Palabras Clave de Negocio

**Entrada del Usuario:**
```
"Necesito financiamiento para un auto seminuevo"
```

**Flujo:**
1. ContentModerator detecta palabras clave: "financiamiento", "auto", "seminuevo"
2. ✅ Claramente en tema de negocio
3. Severidad: None
4. Procesa normalmente

**Respuesta del Bot:**
```
¡Perfecto! 🚗 En Kavak tenemos opciones de financiamiento muy flexibles.

Te puedo ayudar con:
• Crédito con enganche desde 10%
• Plazos de 12 hasta 60 meses
• Aprobación en menos de 24 horas
• Tasa competitiva según tu perfil

¿Cuál es tu presupuesto mensual aproximado para el pago?
```

---

### 🔍 Ejemplo 9: Pregunta Personal (Off-topic Sutil)

**Entrada del Usuario:**
```
"¿Cuántos años tienes? ¿Dónde vives?"
```

**Flujo:**
1. ContentModerator detecta patrones personales: "cuántos años", "dónde vives"
2. No contiene palabras de negocio
3. ⚠️ Off-topic (pregunta personal)
4. Severidad: Baja

**Respuesta del Bot:**
```
"Aprecio tu interés, pero mi especialidad es ayudarte con la compra de vehículos. ¿Puedo asistirte en encontrar el auto ideal para ti o resolver dudas sobre nuestros servicios?"
```

---

### ✅ Ejemplo 10: Conversación Normal con Seguimiento

**Mensaje 1:**
```
Usuario: "Busco un sedán automático"
Bot: "Excelente elección. Tenemos varios sedanes automáticos. ¿Cuál es tu presupuesto?"
```

**Mensaje 2:**
```
Usuario: "Entre 200 y 250 mil pesos"
Bot: "Perfecto. Te recomiendo Honda Civic 2020 ($245k) y Toyota Corolla 2019 ($238k)..."
```

**Mensaje 3:**
```
Usuario: "El Civic me interesa, ¿tiene garantía?"
Bot: "Sí, el Honda Civic 2020 incluye garantía mecánica de 3 meses o 5,000 km..."
```

**Flujo:** 
- ✅ Todos los mensajes apropiados
- ✅ Respuestas del LLM validadas
- ✅ Sin violaciones
- ✅ Conversación fluida

---

## 🧪 Cómo Probar

### Opción 1: Swagger UI

1. Ejecutar la API:
   ```bash
   cd src/Baba.Chatbot.Api
   dotnet run
   ```

2. Ir a `https://localhost:7xxx/swagger`

3. Usar endpoint `POST /v1/webhook/twilio/incoming`

4. Ejemplo de request body:
   ```json
   {
     "From": "whatsapp:+5215551234567",
     "Body": "Quiero comprar un auto"
   }
   ```

### Opción 2: Unit Tests

```bash
cd tests/Baba.Chatbot.UnitTests
dotnet test --filter "FullyQualifiedName~Guardrails"
```

### Opción 3: Código Directo

```csharp
var moderator = new ContentModerator(logger);
var result = moderator.ModerateContent("Mensaje de prueba");

Console.WriteLine($"Apropiado: {result.IsAppropriate}");
Console.WriteLine($"Severidad: {result.Severity}");
Console.WriteLine($"Flags: {string.Join(", ", result.Flags)}");
Console.WriteLine($"Respuesta: {result.SuggestedResponse}");
```

---

## 📋 Checklist de Pruebas

Prueba estos escenarios para verificar el funcionamiento:

- [ ] Mensaje apropiado sobre vehículos
- [ ] Contenido de odio (debe bloquearse inmediatamente)
- [ ] Contenido violento (debe bloquearse inmediatamente)
- [ ] Contenido sexual (debe bloquearse inmediatamente)
- [ ] Lenguaje ofensivo (advertencia)
- [ ] Tema off-topic (redirección)
- [ ] Pregunta personal (redirección)
- [ ] PII en respuesta (debe enmascararse)
- [ ] Promesa no autorizada (debe escalar)
- [ ] Reincidencia (3 violaciones)
- [ ] Conversación normal multi-turno

---

## 🎯 Resultado Esperado

| Escenario | Severidad | Acción Esperada |
|-----------|-----------|-----------------|
| Negocio automotriz | None | ✅ Procesa normalmente |
| Off-topic | Low | ⚠️ Redirige amablemente |
| Lenguaje ofensivo | Medium | ⚠️ Advierte firmemente |
| Odio/Violencia/Sexual | High | ❌ Termina inmediatamente |
| 3 violaciones | High | ❌ Termina y escala |
| PII en respuesta | Medium | 🛡️ Enmascara |
| Promesa no autorizada | High | 🔼 Escala a humano |

---

**Nota**: Todos estos ejemplos están basados en las reglas implementadas en `ContentModerator.cs` y `GuardrailsValidator.cs`. Puedes ajustar las palabras clave y patrones según tus necesidades específicas.
