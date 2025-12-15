"""
Cliente para interactuar con modelos de lenguaje de OpenAI.
Implementa generación de respuestas con RAG y function calling para búsqueda de vehículos.
"""
import json
import logging
from typing import List, Optional, Any, Dict
from openai import AsyncOpenAI
from ...application.abstractions.interfaces import ILlmClient, ICatalogRepository
from ...domain.value_objects import VehicleQuery
from .knowledge_repository import KnowledgeRepository


logger = logging.getLogger(__name__)


class LlmClient(ILlmClient):
    """
    Cliente para interactuar con modelos de lenguaje de OpenAI.
    """

    def __init__(
        self,
        api_key: str,
        model: str = "gpt-4o-mini",
        temperature: float = 0.7,
        knowledge_repository: Optional[KnowledgeRepository] = None,
        catalog_repository: Optional[ICatalogRepository] = None
    ):
        """
        Inicializa el cliente LLM.
        
        Args:
            api_key: API key de OpenAI
            model: Modelo a utilizar
            temperature: Temperatura para generación (0-1)
            knowledge_repository: Repositorio de conocimiento para RAG
            catalog_repository: Repositorio de catálogo para function calling
        """
        self._client = AsyncOpenAI(api_key=api_key)
        self._model = model
        self._temperature = temperature
        self._knowledge_repository = knowledge_repository
        self._catalog_repository = catalog_repository
        
        logger.info(f"LlmClient initialized with model: {model}")

    async def generate_response(
        self,
        system_prompt: str,
        user_message: str,
        conversation_history: Optional[List[str]] = None
    ) -> str:
        """
        Genera una respuesta usando el LLM con contexto RAG y herramientas de búsqueda de vehículos.
        Enriquece el prompt del sistema con información relevante de la base de conocimiento.
        
        Args:
            system_prompt: Prompt del sistema con instrucciones
            user_message: Mensaje del usuario
            conversation_history: Historial de conversación opcional
            
        Returns:
            Respuesta generada por el LLM
        """
        try:
            # Buscar contexto relevante en RAG si está disponible
            relevant_context = ""
            if self._knowledge_repository:
                relevant_context = self._knowledge_repository.search_relevant_context(
                    user_message,
                    max_chunks=3
                )

            # Enriquecer el prompt del sistema con contexto RAG
            enhanced_system_prompt = system_prompt
            if relevant_context:
                enhanced_system_prompt = (
                    f"{system_prompt}\n\n---\n\n"
                    "## INFORMACIÓN DE CONTEXTO (Base de Conocimiento)\n\n"
                    "Usa esta información para responder con datos precisos:\n\n"
                    f"{relevant_context}\n\n---\n\n"
                    "IMPORTANTE: Usa SOLO la información del contexto cuando esté disponible. "
                    "Si la información no está en el contexto, indica que necesitas verificar con un asesor."
                )
                logger.info(f"Enhanced prompt with {len(relevant_context)} characters of context")

            # Construir mensajes
            messages = [
                {"role": "system", "content": enhanced_system_prompt}
            ]

            # Agregar historial de conversación si existe
            if conversation_history:
                for i, msg in enumerate(conversation_history):
                    role = "user" if i % 2 == 0 else "assistant"
                    messages.append({"role": role, "content": msg})

            messages.append({"role": "user", "content": user_message})

            logger.debug(f"Sending chat completion request with {len(messages)} messages")

            # Preparar herramientas (function calling) si hay catálogo disponible
            tools = []
            if self._catalog_repository:
                tools = [
                    {
                        "type": "function",
                        "function": {
                            "name": "search_vehicles",
                            "description": "Busca vehículos en el inventario según criterios específicos como marca, modelo, precio, año, etc.",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "brand": {
                                        "type": "string",
                                        "description": "Marca del vehículo (ej: Toyota, Honda, BMW)"
                                    },
                                    "model": {
                                        "type": "string",
                                        "description": "Modelo del vehículo (ej: Corolla, Civic, Serie 3)"
                                    },
                                    "min_price": {
                                        "type": "number",
                                        "description": "Precio mínimo en pesos"
                                    },
                                    "max_price": {
                                        "type": "number",
                                        "description": "Precio máximo en pesos"
                                    },
                                    "min_year": {
                                        "type": "integer",
                                        "description": "Año mínimo del vehículo"
                                    },
                                    "max_year": {
                                        "type": "integer",
                                        "description": "Año máximo del vehículo"
                                    },
                                    "max_mileage": {
                                        "type": "integer",
                                        "description": "Kilometraje máximo permitido"
                                    }
                                },
                                "required": []
                            }
                        }
                    },
                    {
                        "type": "function",
                        "function": {
                            "name": "get_vehicle_details",
                            "description": "Obtiene información detallada de un vehículo específico usando su ID o stock_id",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "vehicle_id": {
                                        "type": "string",
                                        "description": "ID o stock_id del vehículo"
                                    }
                                },
                                "required": ["vehicle_id"]
                            }
                        }
                    }
                ]

            # Llamar a la API de OpenAI
            completion_args = {
                "model": self._model,
                "messages": messages,
                "temperature": self._temperature
            }
            
            if tools:
                completion_args["tools"] = tools

            completion = await self._client.chat.completions.create(**completion_args)

            # Verificar si hay llamadas a funciones
            if completion.choices[0].finish_reason == "tool_calls":
                response = await self._handle_tool_calls(messages, completion)
                return response

            response_text = completion.choices[0].message.content
            
            logger.info(f"Generated response: {len(response_text)} characters")
            
            return response_text
        except Exception as ex:
            logger.error(f"Error generating LLM response: {ex}")
            raise

    async def _handle_tool_calls(
        self,
        messages: List[Dict[str, Any]],
        completion: Any
    ) -> str:
        """
        Maneja las llamadas a herramientas (function calling) del LLM.
        Ejecuta las funciones solicitadas y envía los resultados de vuelta al modelo.
        
        Args:
            messages: Lista de mensajes de la conversación
            completion: Objeto de completion con las tool calls
            
        Returns:
            Respuesta final del LLM después de procesar las tool calls
        """
        if not self._catalog_repository:
            return "Lo siento, no puedo consultar el catálogo en este momento."

        # Agregar el mensaje del asistente con las tool calls
        assistant_message = completion.choices[0].message
        messages.append({
            "role": "assistant",
            "content": assistant_message.content,
            "tool_calls": [
                {
                    "id": tc.id,
                    "type": "function",
                    "function": {
                        "name": tc.function.name,
                        "arguments": tc.function.arguments
                    }
                }
                for tc in assistant_message.tool_calls
            ]
        })

        # Ejecutar cada tool call
        for tool_call in assistant_message.tool_calls:
            function_name = tool_call.function.name
            function_args = tool_call.function.arguments

            logger.info(f"Processing tool call: {function_name} with args: {function_args}")

            try:
                function_result = await self._execute_tool(function_name, function_args)
            except Exception as ex:
                logger.error(f"Error executing tool {function_name}: {ex}")
                function_result = json.dumps({"error": "Error ejecutando la búsqueda"})

            # Agregar resultado de la tool call
            messages.append({
                "role": "tool",
                "tool_call_id": tool_call.id,
                "content": function_result
            })

        # Obtener respuesta final del LLM
        final_completion = await self._client.chat.completions.create(
            model=self._model,
            messages=messages,
            temperature=self._temperature
        )

        return final_completion.choices[0].message.content

    async def _execute_tool(self, function_name: str, function_args: str) -> str:
        """
        Ejecuta una función/herramienta específica según su nombre.
        Soporta búsqueda de vehículos y obtención de detalles.
        
        Args:
            function_name: Nombre de la función a ejecutar
            function_args: Argumentos de la función en formato JSON
            
        Returns:
            Resultado de la función en formato JSON
        """
        if not self._catalog_repository:
            return json.dumps({"error": "Catálogo no disponible"})

        try:
            if function_name == "search_vehicles":
                params = json.loads(function_args)
                
                query = VehicleQuery(
                    brand=params.get("brand"),
                    model=params.get("model"),
                    min_price=params.get("min_price"),
                    max_price=params.get("max_price"),
                    min_year=params.get("min_year"),
                    max_year=params.get("max_year"),
                    max_mileage=params.get("max_mileage")
                )

                vehicles = await self._catalog_repository.search_vehicles(query)
                
                # Limitar a 10 resultados
                limited_vehicles = [
                    {
                        "id": v.stock_id,
                        "marca": v.brand,
                        "modelo": v.model,
                        "año": v.year,
                        "version": v.version,
                        "precio": v.price,
                        "kilometraje": v.mileage,
                        "dimensiones": {
                            "largo": v.length,
                            "ancho": v.width,
                            "alto": v.height
                        },
                        "caracteristicas": {
                            "bluetooth": v.has_bluetooth,
                            "carplay": v.has_carplay
                        }
                    }
                    for v in vehicles[:10]
                ]

                return json.dumps({
                    "total": len(vehicles),
                    "resultados_mostrados": len(limited_vehicles),
                    "vehiculos": limited_vehicles
                }, ensure_ascii=False)

            elif function_name == "get_vehicle_details":
                params = json.loads(function_args)
                vehicle_id = params.get("vehicle_id")
                
                if not vehicle_id:
                    return json.dumps({"error": "ID de vehículo requerido"})

                vehicle = await self._catalog_repository.get_vehicle_by_id(vehicle_id)
                
                if not vehicle:
                    return json.dumps({"error": "Vehículo no encontrado"})

                return json.dumps({
                    "id": vehicle.stock_id,
                    "marca": vehicle.brand,
                    "modelo": vehicle.model,
                    "año": vehicle.year,
                    "version": vehicle.version,
                    "precio": vehicle.price,
                    "kilometraje": vehicle.mileage,
                    "dimensiones": {
                        "largo": vehicle.length,
                        "ancho": vehicle.width,
                        "alto": vehicle.height
                    },
                    "caracteristicas": {
                        "bluetooth": vehicle.has_bluetooth,
                        "carplay": vehicle.has_carplay
                    }
                }, ensure_ascii=False)

            else:
                return json.dumps({"error": f"Función desconocida: {function_name}"})
        except Exception as ex:
            logger.error(f"Error in _execute_tool for {function_name}: {ex}")
            return json.dumps({"error": "Error interno al procesar la solicitud"})

    async def generate_structured_response(self, prompt: str, response_type: type):
        """
        Genera una respuesta estructurada del LLM según un esquema específico.
        
        Args:
            prompt: Prompt para el LLM
            response_type: Tipo de respuesta esperada
            
        Raises:
            NotImplementedError: Esta funcionalidad aún no está implementada
        """
        logger.warning("Structured response generation not yet implemented")
        raise NotImplementedError("Structured response generation not yet implemented")

