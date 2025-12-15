# Grafana Dashboards

Este directorio contiene dashboards pre-configurados de Grafana para monitorear el sistema Baba Chatbot.

## Dashboards Disponibles

1. **API Performance**: Métricas de rendimiento de la API
   - Request rate
   - Response time (p50, p95, p99)
   - Error rate
   - Active connections

2. **LLM Metrics**: Métricas del modelo de lenguaje
   - Latencia de generación
   - Tokens procesados
   - Cache hit rate
   - Errores de inferencia

3. **RAG Performance**: Métricas del sistema RAG
   - Búsquedas realizadas
   - Tiempos de retrieval
   - Relevancia de documentos
   - Cache effectiveness

4. **Conversation Analytics**: Analítica de conversaciones
   - Conversaciones activas
   - Intenciones detectadas
   - Tasa de resolución
   - Customer satisfaction score

5. **Infrastructure**: Métricas de infraestructura
   - CPU usage
   - Memory usage
   - Disk I/O
   - Network traffic

## Importar Dashboards

Los dashboards se provisionan automáticamente al iniciar Grafana con Docker Compose.

### Acceso a Grafana

- URL: http://localhost:3000
- Usuario: admin
- Password: admin (cambiar en primer login)

## Crear Dashboards Personalizados

Coloca archivos JSON de dashboards en:
```
ops/observability/grafana/dashboards/
```

Grafana los cargará automáticamente al iniciar.

