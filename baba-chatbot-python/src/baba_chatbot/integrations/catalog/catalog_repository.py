"""
Repositorio para cargar y consultar el catálogo de vehículos desde archivos CSV y JSON.
Implementa caché en memoria para optimizar el rendimiento.
"""
import os
import json
import csv
import logging
from typing import List, Optional, Dict
from ...application.abstractions.interfaces import ICatalogRepository
from ...domain.entities import Vehicle
from ...domain.value_objects import VehicleQuery


logger = logging.getLogger(__name__)


class CatalogRepository(ICatalogRepository):
    """
    Repositorio para cargar y consultar el catálogo de vehículos.
    """

    def __init__(
        self,
        catalog_file_path: str = "./data/catalog/cars_extract.json",
        csv_file_path: str = None
    ):
        """
        Inicializa el repositorio de catálogo.
        
        Args:
            catalog_file_path: Ruta al archivo JSON del catálogo
            csv_file_path: Ruta al archivo CSV del catálogo (opcional)
        """
        self._catalog_file_path = catalog_file_path
        self._csv_file_path = csv_file_path
        self._cached_vehicles: Optional[List[Vehicle]] = None

    async def search_vehicles(self, query: VehicleQuery) -> List[Vehicle]:
        """
        Busca vehículos en el catálogo aplicando los filtros especificados en el query.
        Si no hay filtros, devuelve los primeros 10 vehículos.
        
        Args:
            query: Criterios de búsqueda
            
        Returns:
            Lista de vehículos que coinciden con los criterios
        """
        all_vehicles = await self._load_vehicles()

        if not query.has_any_filter():
            return all_vehicles[:10]

        filtered = [v for v in all_vehicles if self._matches_query(v, query)]
        return filtered

    async def get_vehicle_by_id(self, vehicle_id: str) -> Optional[Vehicle]:
        """
        Obtiene un vehículo específico por su ID o stock_id.
        
        Args:
            vehicle_id: ID o stock_id del vehículo
            
        Returns:
            Vehículo encontrado o None si no existe
        """
        vehicles = await self._load_vehicles()
        return next(
            (v for v in vehicles if v.id == vehicle_id or v.stock_id == vehicle_id),
            None
        )

    async def _load_vehicles(self) -> List[Vehicle]:
        """
        Carga los vehículos desde archivos CSV y JSON con caché en memoria.
        
        Returns:
            Lista de vehículos cargados
        """
        if self._cached_vehicles is not None:
            return self._cached_vehicles

        try:
            vehicles = []

            # Cargar desde CSV si existe
            if self._csv_file_path and os.path.exists(self._csv_file_path):
                logger.info(f"Loading vehicles from CSV: {self._csv_file_path}")
                vehicles.extend(await self._load_from_csv(self._csv_file_path))

            # Cargar desde JSON si existe
            if os.path.exists(self._catalog_file_path):
                logger.info(f"Loading vehicles from JSON: {self._catalog_file_path}")
                vehicles.extend(await self._load_from_json(self._catalog_file_path))

            if not vehicles:
                logger.warning("No catalog files found or loaded")

            self._cached_vehicles = vehicles
            logger.info(f"Loaded {len(self._cached_vehicles)} vehicles from catalog")

            return self._cached_vehicles
        except Exception as ex:
            logger.error(f"Error loading catalog: {ex}")
            return []

    async def _load_from_csv(self, file_path: str) -> List[Vehicle]:
        """
        Carga vehículos desde un archivo CSV.
        Formato esperado: stock_id,km,price,make,model,year,version,bluetooth,largo,ancho,altura,car_play
        
        Args:
            file_path: Ruta al archivo CSV
            
        Returns:
            Lista de vehículos cargados
        """
        vehicles = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                
                for line_number, row in enumerate(reader, start=2):
                    try:
                        stock_id = row.get('stock_id', '').strip()
                        km = self._parse_int(row.get('km', ''))
                        price = self._parse_float(row.get('price', ''))
                        make = row.get('make', '').strip()
                        model = row.get('model', '').strip()
                        year = self._parse_int(row.get('year', '')) or 2024
                        version = row.get('version', '').strip()
                        bluetooth = self._parse_bool(row.get('bluetooth', ''))
                        length = self._parse_int(row.get('largo', ''))
                        width = self._parse_int(row.get('ancho', ''))
                        height = self._parse_int(row.get('altura', ''))
                        carplay = self._parse_bool(row.get('car_play', ''))

                        vehicle = Vehicle.create(
                            vehicle_id=stock_id,
                            brand=make,
                            model=model,
                            year=year,
                            price=price or 0.0
                        )

                        vehicle.update_details(
                            version=version,
                            mileage=km,
                            length=length,
                            width=width,
                            height=height,
                            has_bluetooth=bluetooth,
                            has_carplay=carplay,
                            stock_id=stock_id
                        )

                        vehicles.append(vehicle)
                    except Exception as ex:
                        logger.warning(f"Error parsing line {line_number}: {ex}")
        except Exception as ex:
            logger.error(f"Error reading CSV file {file_path}: {ex}")

        return vehicles

    async def _load_from_json(self, file_path: str) -> List[Vehicle]:
        """
        Carga vehículos desde un archivo JSON.
        
        Args:
            file_path: Ruta al archivo JSON
            
        Returns:
            Lista de vehículos cargados
        """
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
                
            vehicles = []
            for item in data:
                vehicle = self._map_to_entity(item)
                vehicles.append(vehicle)
            
            return vehicles
        except Exception as ex:
            logger.error(f"Error reading JSON file {file_path}: {ex}")
            return []

    def _parse_int(self, value: str) -> Optional[int]:
        """
        Parsea un valor string a entero manejando valores nulos o "NA".
        
        Args:
            value: Valor a parsear
            
        Returns:
            Entero parseado o None
        """
        if not value or value.strip().upper() == 'NA':
            return None
        
        try:
            return int(float(value.strip()))
        except (ValueError, AttributeError):
            return None

    def _parse_float(self, value: str) -> Optional[float]:
        """
        Parsea un valor string a float manejando valores nulos o "NA".
        
        Args:
            value: Valor a parsear
            
        Returns:
            Float parseado o None
        """
        if not value or value.strip().upper() == 'NA':
            return None
        
        try:
            return float(value.strip())
        except (ValueError, AttributeError):
            return None

    def _parse_bool(self, value: str) -> Optional[bool]:
        """
        Parsea un valor string a booleano manejando valores nulos, "NA" o enteros (1/0).
        
        Args:
            value: Valor a parsear
            
        Returns:
            Booleano parseado o None
        """
        if not value or value.strip().upper() == 'NA':
            return None
        
        value = value.strip().lower()
        
        if value in ('true', 'yes', '1'):
            return True
        elif value in ('false', 'no', '0'):
            return False
        
        try:
            return int(value) != 0
        except (ValueError, AttributeError):
            return None

    def _matches_query(self, vehicle: Vehicle, query: VehicleQuery) -> bool:
        """
        Verifica si un vehículo cumple con los criterios del query especificado.
        
        Args:
            vehicle: Vehículo a verificar
            query: Criterios de búsqueda
            
        Returns:
            True si el vehículo cumple con los criterios
        """
        if query.brand and query.brand.lower() not in vehicle.brand.lower():
            return False

        if query.model and query.model.lower() not in vehicle.model.lower():
            return False

        if query.max_price is not None and vehicle.price > query.max_price:
            return False

        if query.min_price is not None and vehicle.price < query.min_price:
            return False

        if query.min_year is not None and vehicle.year < query.min_year:
            return False

        if query.max_year is not None and vehicle.year > query.max_year:
            return False

        if query.max_mileage is not None and vehicle.mileage > query.max_mileage:
            return False

        return True

    def _map_to_entity(self, dto: Dict) -> Vehicle:
        """
        Mapea un DTO de JSON a la entidad de dominio Vehicle.
        
        Args:
            dto: Diccionario con datos del vehículo
            
        Returns:
            Instancia de Vehicle
        """
        vehicle = Vehicle.create(
            vehicle_id=dto.get('id', ''),
            brand=dto.get('brand', ''),
            model=dto.get('model', ''),
            year=dto.get('year', 2024),
            price=dto.get('price', 0.0)
        )
        return vehicle

