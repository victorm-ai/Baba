namespace Baba.Chatbot.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un vehículo disponible en el catálogo
/// Incluye características técnicas, especificaciones, estado y certificación
/// </summary>
public class Vehicle
{
    public string Id { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public string Version { get; private set; }
    public decimal Price { get; private set; }
    public int Mileage { get; private set; }
    public string Transmission { get; private set; }
    public string FuelType { get; private set; }
    public string Color { get; private set; }
    public int Doors { get; private set; }
    public int Seats { get; private set; }
    public string Engine { get; private set; }
    public int Horsepower { get; private set; }
    public List<string> Features { get; private set; } = new();
    public string Location { get; private set; }
    public VehicleStatus Status { get; private set; }
    public decimal CertificationScore { get; private set; }
    public int PreviousOwners { get; private set; }
    public bool HasAccidents { get; private set; }
    
    // Dimensiones
    public int? Length { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    
    // Características adicionales
    public bool? HasBluetooth { get; private set; }
    public bool? HasCarPlay { get; private set; }
    
    public string StockId { get; private set; } = string.Empty;

    /// <summary>
    /// Constructor privado para encapsular creación
    /// </summary>
    private Vehicle()
    {
        Id = string.Empty;
        Brand = string.Empty;
        Model = string.Empty;
        Version = string.Empty;
        Transmission = string.Empty;
        FuelType = string.Empty;
        Color = string.Empty;
        Engine = string.Empty;
        Location = string.Empty;
    }

    /// <summary>
    /// Crea una nueva instancia de vehículo con los datos básicos requeridos
    /// </summary>
    public static Vehicle Create(
        string id,
        string brand,
        string model,
        int year,
        decimal price)
    {
        return new Vehicle
        {
            Id = id,
            Brand = brand,
            Model = model,
            Year = year,
            Price = price,
            Status = VehicleStatus.Available
        };
    }
    
    /// <summary>
    /// Actualiza detalles opcionales del vehículo como versión, kilometraje y características
    /// </summary>
    public void UpdateDetails(
        string? version = null,
        int? mileage = null,
        int? length = null,
        int? width = null,
        int? height = null,
        bool? hasBluetooth = null,
        bool? hasCarPlay = null,
        string? stockId = null)
    {
        if (version != null) Version = version;
        if (mileage.HasValue) Mileage = mileage.Value;
        if (length.HasValue) Length = length;
        if (width.HasValue) Width = width;
        if (height.HasValue) Height = height;
        if (hasBluetooth.HasValue) HasBluetooth = hasBluetooth;
        if (hasCarPlay.HasValue) HasCarPlay = hasCarPlay;
        if (stockId != null) StockId = stockId;
    }

    /// <summary>
    /// Verifica si el vehículo está disponible para venta
    /// </summary>
    public bool IsAvailable() => Status == VehicleStatus.Available;

    /// <summary>
    /// Marca el vehículo como reservado si está disponible
    /// </summary>
    public void Reserve()
    {
        if (Status == VehicleStatus.Available)
        {
            Status = VehicleStatus.Reserved;
        }
    }

    public void Sell()
    {
        Status = VehicleStatus.Sold;
    }
}

/// <summary>
/// Estado actual de disponibilidad de un vehículo
/// </summary>
public enum VehicleStatus
{
    Available,
    Reserved,
    Sold,
    Maintenance
}

