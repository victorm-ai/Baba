"""
Integración con LLM (Large Language Models)
"""
from .llm_client import LlmClient
from .knowledge_repository import KnowledgeRepository
from .prompt_repository import PromptRepository

__all__ = ["LlmClient", "KnowledgeRepository", "PromptRepository"]

