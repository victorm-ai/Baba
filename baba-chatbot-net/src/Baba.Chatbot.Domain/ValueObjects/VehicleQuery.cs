namespace Baba.Chatbot.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa los criterios de búsqueda para filtrar vehículos
/// Inmutable por ser un record
/// </summary>
public record VehicleQuery
{
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? MinYear { get; init; }
    public int? MaxYear { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MaxMileage { get; init; }
    public string? Transmission { get; init; }
    public string? FuelType { get; init; }
    public string? BodyType { get; init; }
    public List<string> RequiredFeatures { get; init; } = new();

    /// <summary>
    /// Verifica si se especificó al menos un filtro de búsqueda
    /// </summary>
    public bool HasAnyFilter() =>
        Brand != null ||
        Model != null ||
        MinYear.HasValue ||
        MaxYear.HasValue ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        MaxMileage.HasValue ||
        Transmission != null ||
        FuelType != null ||
        BodyType != null ||
        RequiredFeatures.Count > 0;
}

