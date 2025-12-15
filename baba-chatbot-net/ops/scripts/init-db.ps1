# Script para inicializar la base de datos
# Uso: .\init-db.ps1 -ServerName "localhost" -DatabaseName "BabaChatbot" -Username "baba" -Password "your_password"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName,
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "baba",
    
    [Parameter(Mandatory=$false)]
    [string]$Password
)

Write-Host "Inicializando base de datos: $DatabaseName en $ServerName" -ForegroundColor Green

# Verificar si SQL Server está disponible
try {
    if ($Password) {
        $connectionString = "Server=$ServerName;Database=master;User Id=$Username;Password=$Password;TrustServerCertificate=true;"
    } else {
        $connectionString = "Server=$ServerName;Database=master;Integrated Security=True;TrustServerCertificate=true;"
    }
    
    # Intentar conexión
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "✓ Conexión a SQL Server exitosa" -ForegroundColor Green
    
    # Crear base de datos si no existe
    $createDbQuery = @"
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    CREATE DATABASE [$DatabaseName]
    PRINT 'Base de datos creada exitosamente'
END
ELSE
BEGIN
    PRINT 'Base de datos ya existe'
END
"@
    
    $command = $connection.CreateCommand()
    $command.CommandText = $createDbQuery
    $command.ExecuteNonQuery() | Out-Null
    Write-Host "✓ Base de datos creada/verificada" -ForegroundColor Green
    
    $connection.Close()
    
    # Ejecutar migraciones con Entity Framework
    Write-Host "Ejecutando migraciones..." -ForegroundColor Yellow
    
    $apiProjectPath = Join-Path $PSScriptRoot "..\..\src\Baba.Chatbot.Api"
    
    if (Test-Path $apiProjectPath) {
        Push-Location $apiProjectPath
        
        try {
            dotnet ef database update --verbose
            Write-Host "✓ Migraciones aplicadas exitosamente" -ForegroundColor Green
        }
        catch {
            Write-Host "✗ Error al aplicar migraciones: $_" -ForegroundColor Red
            exit 1
        }
        finally {
            Pop-Location
        }
    } else {
        Write-Host "⚠ No se encontró el proyecto API en: $apiProjectPath" -ForegroundColor Yellow
        Write-Host "  Las migraciones deben ejecutarse manualmente" -ForegroundColor Yellow
    }
    
    Write-Host "`n✓ Inicialización completada" -ForegroundColor Green
    Write-Host "Connection String: Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=***" -ForegroundColor Cyan
    
} catch {
    Write-Host "✗ Error: $_" -ForegroundColor Red
    exit 1
}

