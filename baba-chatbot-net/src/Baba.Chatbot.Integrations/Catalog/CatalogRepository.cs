using Baba.Chatbot.Application.Abstractions;
using Baba.Chatbot.Domain.Entities;
using Baba.Chatbot.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace Baba.Chatbot.Integrations.Catalog;

/// <summary>
/// Repositorio para cargar y consultar el catálogo de vehículos desde archivos CSV y JSON
/// Implementa caché en memoria para optimizar el rendimiento
/// </summary>
public class CatalogRepository : ICatalogRepository
{
    private readonly ILogger<CatalogRepository> _logger;
    private readonly string _catalogFilePath;
    private readonly string _csvFilePath;
    private List<Vehicle>? _cachedVehicles;

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de catálogo
    /// </summary>
    public CatalogRepository(IConfiguration configuration, ILogger<CatalogRepository> logger)
    {
        _logger = logger;
        _catalogFilePath = configuration["Catalog:FilePath"] ?? "./data/catalog/cars_extract.json";
        _csvFilePath = configuration["Catalog:CsvFilePath"] ?? "./src/Baba.Chatbot.Integrations/Catalog/sample_caso_ai_engineer.csv";
    }

    /// <summary>
    /// Busca vehículos en el catálogo aplicando los filtros especificados en el query
    /// Si no hay filtros, devuelve los primeros 10 vehículos
    /// </summary>
    public async Task<List<Vehicle>> SearchVehiclesAsync(VehicleQuery query, CancellationToken cancellationToken = default)
    {
        var allVehicles = await LoadVehiclesAsync(cancellationToken);

        if (!query.HasAnyFilter())
            return allVehicles.Take(10).ToList();

        var filtered = allVehicles.Where(v => MatchesQuery(v, query)).ToList();

        return filtered;
    }

    /// <summary>
    /// Obtiene un vehículo específico por su ID o stock_id
    /// </summary>
    public async Task<Vehicle?> GetVehicleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var vehicles = await LoadVehiclesAsync(cancellationToken);
        return vehicles.FirstOrDefault(v => v.Id == id || v.StockId == id);
    }

    /// <summary>
    /// Carga los vehículos desde archivos CSV y JSON con caché en memoria
    /// </summary>
    private async Task<List<Vehicle>> LoadVehiclesAsync(CancellationToken cancellationToken)
    {
        if (_cachedVehicles != null)
            return _cachedVehicles;

        try
        {
            var vehicles = new List<Vehicle>();

            if (File.Exists(_csvFilePath))
            {
                _logger.LogInformation("Loading vehicles from CSV: {Path}", _csvFilePath);
                vehicles.AddRange(await LoadFromCsvAsync(_csvFilePath, cancellationToken));
            }

            if (File.Exists(_catalogFilePath))
            {
                _logger.LogInformation("Loading vehicles from JSON: {Path}", _catalogFilePath);
                vehicles.AddRange(await LoadFromJsonAsync(_catalogFilePath, cancellationToken));
            }

            if (vehicles.Count == 0)
            {
                _logger.LogWarning("No catalog files found or loaded");
            }

            _cachedVehicles = vehicles;
            _logger.LogInformation("Loaded {Count} vehicles from catalog", _cachedVehicles.Count);

            return _cachedVehicles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading catalog");
            return new List<Vehicle>();
        }
    }

    /// <summary>
    /// Carga vehículos desde un archivo CSV
    /// Formato esperado: stock_id,km,price,make,model,year,version,bluetooth,largo,ancho,altura,car_play
    /// </summary>
    private async Task<List<Vehicle>> LoadFromCsvAsync(string filePath, CancellationToken cancellationToken)
    {
        var vehicles = new List<Vehicle>();
        
        using var reader = new StreamReader(filePath);
        
        var header = await reader.ReadLineAsync();
        if (header == null)
            return vehicles;

        int lineNumber = 1;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            lineNumber++;
            
            try
            {
                var values = line.Split(',');
                if (values.Length < 8)
                {
                    _logger.LogWarning("Skipping line {Line}: insufficient columns", lineNumber);
                    continue;
                }

                var stockId = values[0].Trim();
                var km = ParseInt(values[1]);
                var price = ParseDecimal(values[2]);
                var make = values[3].Trim();
                var model = values[4].Trim();
                var year = ParseInt(values[5]) ?? DateTime.Now.Year;
                var version = values.Length > 6 ? values[6].Trim() : "";
                var bluetooth = values.Length > 7 ? ParseBool(values[7]) : null;
                var length = values.Length > 8 ? ParseInt(values[8]) : null;
                var width = values.Length > 9 ? ParseInt(values[9]) : null;
                var height = values.Length > 10 ? ParseInt(values[10]) : null;
                var carPlay = values.Length > 11 ? ParseBool(values[11]) : null;

                var vehicle = Vehicle.Create(
                    stockId,
                    make,
                    model,
                    year,
                    price ?? 0
                );

                vehicle.UpdateDetails(
                    version: version,
                    mileage: km,
                    length: length,
                    width: width,
                    height: height,
                    hasBluetooth: bluetooth,
                    hasCarPlay: carPlay,
                    stockId: stockId
                );

                vehicles.Add(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing line {Line}", lineNumber);
            }
        }

        return vehicles;
    }

    /// <summary>
    /// Carga vehículos desde un archivo JSON
    /// </summary>
    private async Task<List<Vehicle>> LoadFromJsonAsync(string filePath, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var vehicleData = JsonSerializer.Deserialize<List<VehicleDto>>(json);

        return vehicleData?.Select(dto => MapToEntity(dto)).ToList() ?? new List<Vehicle>();
    }

    /// <summary>
    /// Parsea un valor string a entero manejando valores nulos o "NA"
    /// </summary>
    private int? ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NA", StringComparison.OrdinalIgnoreCase))
            return null;

        if (int.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Parsea un valor string a decimal manejando valores nulos o "NA"
    /// </summary>
    private decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NA", StringComparison.OrdinalIgnoreCase))
            return null;

        if (decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Parsea un valor string a booleano manejando valores nulos, "NA" o enteros (1/0)
    /// </summary>
    private bool? ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NA", StringComparison.OrdinalIgnoreCase))
            return null;

        if (bool.TryParse(value.Trim(), out var result))
            return result;

        if (int.TryParse(value.Trim(), out var intValue))
            return intValue != 0;

        return null;
    }

    /// <summary>
    /// Verifica si un vehículo cumple con los criterios del query especificado
    /// </summary>
    private bool MatchesQuery(Vehicle vehicle, VehicleQuery query)
    {
        if (query.Brand != null && !vehicle.Brand.Contains(query.Brand, StringComparison.OrdinalIgnoreCase))
            return false;

        if (query.Model != null && !vehicle.Model.Contains(query.Model, StringComparison.OrdinalIgnoreCase))
            return false;

        if (query.MaxPrice.HasValue && vehicle.Price > query.MaxPrice.Value)
            return false;

        if (query.MinPrice.HasValue && vehicle.Price < query.MinPrice.Value)
            return false;

        if (query.MinYear.HasValue && vehicle.Year < query.MinYear.Value)
            return false;

        if (query.MaxYear.HasValue && vehicle.Year > query.MaxYear.Value)
            return false;

        if (query.MaxMileage.HasValue && vehicle.Mileage > query.MaxMileage.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Mapea un DTO de JSON a la entidad de dominio Vehicle
    /// </summary>
    private Vehicle MapToEntity(VehicleDto dto)
    {
        var vehicle = Vehicle.Create(dto.id, dto.brand, dto.model, dto.year, dto.price);
        return vehicle;
    }

    /// <summary>
    /// DTO para deserializar vehículos desde JSON
    /// </summary>
    private record VehicleDto
    {
        public string id { get; init; } = string.Empty;
        public string brand { get; init; } = string.Empty;
        public string model { get; init; } = string.Empty;
        public int year { get; init; }
        public decimal price { get; init; }
        public int mileage { get; init; }
        public string transmission { get; init; } = string.Empty;
        public string fuelType { get; init; } = string.Empty;
        public string color { get; init; } = string.Empty;
        public string status { get; init; } = string.Empty;
    }
}

