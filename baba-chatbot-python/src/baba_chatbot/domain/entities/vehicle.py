"""
Entidad de dominio que representa un vehículo disponible en el catálogo.
Incluye características técnicas, especificaciones, estado y certificación.
"""
from enum import Enum
from typing import List, Optional
from dataclasses import dataclass, field


class VehicleStatus(Enum):
    """Estado actual de disponibilidad de un vehículo"""
    AVAILABLE = "available"
    RESERVED = "reserved"
    SOLD = "sold"
    MAINTENANCE = "maintenance"


@dataclass
class Vehicle:
    """
    Entidad de dominio que representa un vehículo disponible en el catálogo.
    """
    id: str
    brand: str
    model: str
    year: int
    price: float
    version: str = ""
    mileage: int = 0
    transmission: str = ""
    fuel_type: str = ""
    color: str = ""
    doors: int = 0
    seats: int = 0
    engine: str = ""
    horsepower: int = 0
    features: List[str] = field(default_factory=list)
    location: str = ""
    status: VehicleStatus = VehicleStatus.AVAILABLE
    certification_score: float = 0.0
    previous_owners: int = 0
    has_accidents: bool = False
    
    # Dimensiones
    length: Optional[int] = None
    width: Optional[int] = None
    height: Optional[int] = None
    
    # Características adicionales
    has_bluetooth: Optional[bool] = None
    has_carplay: Optional[bool] = None
    
    stock_id: str = ""

    @staticmethod
    def create(
        vehicle_id: str,
        brand: str,
        model: str,
        year: int,
        price: float
    ) -> 'Vehicle':
        """
        Crea una nueva instancia de vehículo con los datos básicos requeridos.
        
        Args:
            vehicle_id: Identificador único del vehículo
            brand: Marca del vehículo
            model: Modelo del vehículo
            year: Año del vehículo
            price: Precio del vehículo
            
        Returns:
            Nueva instancia de Vehicle
        """
        return Vehicle(
            id=vehicle_id,
            brand=brand,
            model=model,
            year=year,
            price=price,
            status=VehicleStatus.AVAILABLE
        )

    def update_details(
        self,
        version: Optional[str] = None,
        mileage: Optional[int] = None,
        length: Optional[int] = None,
        width: Optional[int] = None,
        height: Optional[int] = None,
        has_bluetooth: Optional[bool] = None,
        has_carplay: Optional[bool] = None,
        stock_id: Optional[str] = None
    ) -> None:
        """
        Actualiza detalles opcionales del vehículo como versión, kilometraje y características.
        
        Args:
            version: Versión del vehículo
            mileage: Kilometraje del vehículo
            length: Longitud del vehículo en mm
            width: Ancho del vehículo en mm
            height: Altura del vehículo en mm
            has_bluetooth: Si tiene Bluetooth
            has_carplay: Si tiene CarPlay
            stock_id: ID de stock
        """
        if version is not None:
            self.version = version
        if mileage is not None:
            self.mileage = mileage
        if length is not None:
            self.length = length
        if width is not None:
            self.width = width
        if height is not None:
            self.height = height
        if has_bluetooth is not None:
            self.has_bluetooth = has_bluetooth
        if has_carplay is not None:
            self.has_carplay = has_carplay
        if stock_id is not None:
            self.stock_id = stock_id

    def is_available(self) -> bool:
        """Verifica si el vehículo está disponible para venta."""
        return self.status == VehicleStatus.AVAILABLE

    def reserve(self) -> None:
        """Marca el vehículo como reservado si está disponible."""
        if self.status == VehicleStatus.AVAILABLE:
            self.status = VehicleStatus.RESERVED

    def sell(self) -> None:
        """Marca el vehículo como vendido."""
        self.status = VehicleStatus.SOLD

