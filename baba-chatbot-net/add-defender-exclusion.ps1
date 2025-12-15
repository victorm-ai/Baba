# Script para agregar exclusiones a Windows Defender
# DEBE ejecutarse como Administrador

Write-Host "=== Agregar Exclusión a Windows Defender ===" -ForegroundColor Cyan
Write-Host ""

# Verificar si se está ejecutando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: Este script debe ejecutarse como Administrador" -ForegroundColor Red
    Write-Host "Haz clic derecho en PowerShell y selecciona 'Ejecutar como administrador'" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

$projectPath = "F:\27 Kavak\baba-chatbot-net"

Write-Host "Agregando exclusión de carpeta en Windows Defender..." -ForegroundColor Yellow
Write-Host "Ruta: $projectPath" -ForegroundColor White
Write-Host ""

try {
    # Agregar exclusión de carpeta
    Add-MpPreference -ExclusionPath $projectPath
    Write-Host "Exclusión agregada exitosamente" -ForegroundColor Green
    Write-Host ""
    
    # Verificar la exclusión
    Write-Host "Verificando exclusiones actuales..." -ForegroundColor Cyan
    $prefs = Get-MpPreference
    $exclusions = $prefs.ExclusionPath
    
    if ($exclusions -contains $projectPath) {
        Write-Host "Confirmado: La carpeta está excluida de Windows Defender" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Carpetas excluidas:" -ForegroundColor Cyan
    $exclusions | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    
} catch {
    Write-Host "ERROR: No se pudo agregar la exclusión" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Proceso completado ===" -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANTE: Después de agregar la exclusión, intenta:" -ForegroundColor Yellow
Write-Host "1. Cerrar Visual Studio" -ForegroundColor White
Write-Host "2. Ejecutar: .\force-unlock.ps1" -ForegroundColor White
Write-Host "3. Abrir Visual Studio y compilar" -ForegroundColor White
Write-Host ""
Read-Host "Presiona Enter para salir"
