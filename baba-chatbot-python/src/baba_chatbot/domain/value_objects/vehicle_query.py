"""
Value Object que representa una consulta de búsqueda de vehículos.
"""
from dataclasses import dataclass
from typing import Optional


@dataclass
class VehicleQuery:
    """
    Value Object que encapsula los criterios de búsqueda de vehículos.
    """
    brand: Optional[str] = None
    model: Optional[str] = None
    min_price: Optional[float] = None
    max_price: Optional[float] = None
    min_year: Optional[int] = None
    max_year: Optional[int] = None
    max_mileage: Optional[int] = None

    def has_any_filter(self) -> bool:
        """
        Verifica si la consulta tiene algún filtro aplicado.
        
        Returns:
            True si al menos un filtro está definido, False en caso contrario
        """
        return any([
            self.brand is not None,
            self.model is not None,
            self.min_price is not None,
            self.max_price is not None,
            self.min_year is not None,
            self.max_year is not None,
            self.max_mileage is not None
        ])

