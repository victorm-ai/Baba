using Baba.Chatbot.Integrations.Catalog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Test de Carga del Catálogo ===\n");

// Configurar
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Catalog:CsvFilePath"] = "F:/27 Kavak/baba-chatbot-net/src/Baba.Chatbot.Integrations/Catalog/sample_caso_ai_engineer.csv"
    })
    .Build();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger<CatalogRepository>();

// Crear repositorio
var repository = new CatalogRepository(configuration, logger);

// Probar búsqueda sin filtros
Console.WriteLine("1. Probando búsqueda sin filtros (debería devolver 10 vehículos)...");
var allVehicles = await repository.SearchVehiclesAsync(new Baba.Chatbot.Domain.ValueObjects.VehicleQuery());
Console.WriteLine($"   ✓ Se cargaron {allVehicles.Count} vehículos\n");

if (allVehicles.Count > 0)
{
    var first = allVehicles[0];
    Console.WriteLine($"   Ejemplo: {first.Brand} {first.Model} {first.Year}");
    Console.WriteLine($"   Precio: ${first.Price:N0}");
    Console.WriteLine($"   Stock ID: {first.StockId}");
    Console.WriteLine($"   Kilometraje: {first.Mileage:N0} km\n");
}

// Probar búsqueda por marca
Console.WriteLine("2. Probando búsqueda por marca (BMW)...");
var bmws = await repository.SearchVehiclesAsync(new Baba.Chatbot.Domain.ValueObjects.VehicleQuery { Brand = "BMW" });
Console.WriteLine($"   ✓ Se encontraron {bmws.Count} BMWs\n");

foreach (var bmw in bmws.Take(3))
{
    Console.WriteLine($"   - {bmw.Brand} {bmw.Model} {bmw.Year} - ${bmw.Price:N0}");
}

// Probar búsqueda por ID
if (allVehicles.Count > 0)
{
    var firstId = allVehicles[0].StockId;
    Console.WriteLine($"\n3. Probando búsqueda por ID ({firstId})...");
    var vehicle = await repository.GetVehicleByIdAsync(firstId);
    
    if (vehicle != null)
    {
        Console.WriteLine($"   ✓ Vehículo encontrado:");
        Console.WriteLine($"   Marca: {vehicle.Brand}");
        Console.WriteLine($"   Modelo: {vehicle.Model}");
        Console.WriteLine($"   Año: {vehicle.Year}");
        Console.WriteLine($"   Versión: {vehicle.Version}");
        Console.WriteLine($"   Precio: ${vehicle.Price:N0}");
        Console.WriteLine($"   Kilometraje: {vehicle.Mileage:N0} km");
        if (vehicle.Length.HasValue)
            Console.WriteLine($"   Dimensiones: {vehicle.Length}mm x {vehicle.Width}mm x {vehicle.Height}mm");
        if (vehicle.HasBluetooth.HasValue)
            Console.WriteLine($"   Bluetooth: {(vehicle.HasBluetooth.Value ? "Sí" : "No")}");
    }
}

Console.WriteLine("\n=== Test Completado Exitosamente ===");
