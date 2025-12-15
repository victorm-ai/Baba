"""
Resultados y enumeraciones para moderación de contenido y validación de guardrails.
"""
from enum import Enum
from typing import List
from dataclasses import dataclass, field


class ModerationFlag(Enum):
    """Tipos de violaciones detectadas por el sistema de moderación."""
    HATE_SPEECH = "hate_speech"
    VIOLENCE = "violence"
    SEXUAL_CONTENT = "sexual_content"
    HARASSMENT = "harassment"
    OFF_TOPIC = "off_topic"


class ModerationSeverity(Enum):
    """Niveles de severidad de las violaciones."""
    NONE = 0
    LOW = 1
    MEDIUM = 2
    HIGH = 3


@dataclass
class ContentModerationResult:
    """
    Resultado de la moderación de contenido de un mensaje.
    """
    is_appropriate: bool = True
    severity: ModerationSeverity = ModerationSeverity.NONE
    flags: List[ModerationFlag] = field(default_factory=list)
    suggested_response: str = ""

    def __post_init__(self):
        """Asegura que flags sea una lista mutable."""
        if not isinstance(self.flags, list):
            self.flags = list(self.flags) if self.flags else []


@dataclass
class GuardrailsValidationResult:
    """
    Resultado de la validación de guardrails de una respuesta del LLM.
    """
    is_valid: bool = True
    violations: List[str] = field(default_factory=list)
    cleaned_content: str = ""
    requires_human_escalation: bool = False

    def __post_init__(self):
        """Asegura que violations sea una lista mutable."""
        if not isinstance(self.violations, list):
            self.violations = list(self.violations) if self.violations else []

