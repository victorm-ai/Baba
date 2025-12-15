# Script avanzado para desbloquear archivos en .NET
# Requiere ejecutarse como Administrador

Write-Host "=== Script de Desbloqueo Avanzado ===" -ForegroundColor Cyan
Write-Host "Verificando permisos de administrador..." -ForegroundColor Yellow

# Verificar si se está ejecutando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Este script debe ejecutarse como Administrador" -ForegroundColor Red
    Write-Host "Haz clic derecho en PowerShell y selecciona 'Ejecutar como administrador'" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "Permisos de administrador confirmados" -ForegroundColor Green
Write-Host ""

# Ruta del archivo problemático
$targetFile = "F:\27 Kavak\baba-chatbot-net\src\Baba.Chatbot.Api\bin\Debug\net9.0\Baba.Chatbot.Api.exe"

Write-Host "Paso 1: Apagando servidores de compilación de .NET..." -ForegroundColor Cyan
dotnet build-server shutdown
Start-Sleep -Seconds 2

Write-Host "Paso 2: Deteniendo procesos relacionados..." -ForegroundColor Cyan

# Detener procesos de .NET
$processesToKill = @(
    "Baba.Chatbot.Api",
    "Baba.Chatbot.Api.exe",
    "dotnet",
    "MSBuild",
    "VBCSCompiler",
    "ServiceHub.RoslynCodeAnalysisService",
    "ServiceHub.Host.CLR.x86",
    "ServiceHub.Host.CLR",
    "ServiceHub.SettingsHost",
    "ServiceHub.Host.AnyCPU"
)

foreach ($procName in $processesToKill) {
    $processes = Get-Process -Name $procName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "  Deteniendo: $procName" -ForegroundColor Yellow
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

Start-Sleep -Seconds 3

Write-Host "Paso 3: Verificando si el archivo existe..." -ForegroundColor Cyan
if (Test-Path $targetFile -ErrorAction SilentlyContinue) {
    Write-Host "  El archivo existe y está bloqueado" -ForegroundColor Red
    
    Write-Host "Paso 4: Intentando eliminar el archivo con cmd..." -ForegroundColor Cyan
    cmd /c "del /F /Q `"$targetFile`"" 2>$null
    
    Start-Sleep -Seconds 1
    
    if (Test-Path $targetFile -ErrorAction SilentlyContinue) {
        Write-Host "  Aún bloqueado. Intentando con attrib..." -ForegroundColor Yellow
        cmd /c "attrib -r -s -h `"$targetFile`"" 2>$null
        cmd /c "del /F /Q `"$targetFile`"" 2>$null
    }
    
    Start-Sleep -Seconds 1
    
    if (Test-Path $targetFile -ErrorAction SilentlyContinue) {
        Write-Host "  ERROR: No se pudo eliminar el archivo" -ForegroundColor Red
        Write-Host "  Esto puede ser causado por:" -ForegroundColor Yellow
        Write-Host "    - Antivirus escaneando el archivo" -ForegroundColor Yellow
        Write-Host "    - Windows Defender en tiempo real" -ForegroundColor Yellow
        Write-Host "    - Proceso del sistema bloqueando el archivo" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  SOLUCIONES ADICIONALES:" -ForegroundColor Cyan
        Write-Host "    1. Agrega la carpeta del proyecto a exclusiones de Windows Defender" -ForegroundColor White
        Write-Host "    2. Desactiva temporalmente el antivirus" -ForegroundColor White
        Write-Host "    3. Usa la herramienta Process Monitor de Sysinternals" -ForegroundColor White
        Write-Host "    4. Reinicia el explorador de Windows: taskkill /F /IM explorer.exe && start explorer.exe" -ForegroundColor White
    } else {
        Write-Host "  Archivo eliminado exitosamente" -ForegroundColor Green
    }
} else {
    Write-Host "  El archivo no existe o no es accesible" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Paso 5: Limpiando carpetas bin y obj..." -ForegroundColor Cyan
$binPath = "F:\27 Kavak\baba-chatbot-net\src\Baba.Chatbot.Api\bin"
$objPath = "F:\27 Kavak\baba-chatbot-net\src\Baba.Chatbot.Api\obj"

if (Test-Path $binPath) {
    Remove-Item -Path $binPath -Recurse -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path $binPath)) {
        Write-Host "  bin/ eliminado" -ForegroundColor Green
    }
}

if (Test-Path $objPath) {
    Remove-Item -Path $objPath -Recurse -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path $objPath)) {
        Write-Host "  obj/ eliminado" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Proceso completado ===" -ForegroundColor Green
Write-Host "Intenta compilar nuevamente en Visual Studio" -ForegroundColor Cyan
Write-Host ""
Read-Host "Presiona Enter para salir"
