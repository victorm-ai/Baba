# Script para liberar archivos bloqueados de .NET sin reiniciar
# Uso: .\kill-locked-process.ps1

Write-Host "=== Liberando procesos de Baba.Chatbot.Api ===" -ForegroundColor Cyan

# Detener procesos de dotnet que estén ejecutando tu aplicación
$processName = "Baba.Chatbot.Api"
$dotnetProcesses = Get-Process -Name $processName -ErrorAction SilentlyContinue

if ($dotnetProcesses) {
    Write-Host "Encontrados $($dotnetProcesses.Count) proceso(s) de $processName" -ForegroundColor Yellow
    foreach ($proc in $dotnetProcesses) {
        Write-Host "  - Deteniendo proceso ID: $($proc.Id)" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force
    }
    Write-Host "Procesos detenidos exitosamente" -ForegroundColor Green
} else {
    Write-Host "No se encontraron procesos de $processName ejecutándose" -ForegroundColor Gray
}

# Detener procesos de dotnet.exe que puedan estar bloqueando
$dotnetExeProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | 
    Where-Object { $_.Path -notlike "*\sdk\*" -and $_.CommandLine -like "*Baba.Chatbot*" }

if ($dotnetExeProcesses) {
    Write-Host "Encontrados procesos de dotnet relacionados con Baba.Chatbot" -ForegroundColor Yellow
    foreach ($proc in $dotnetExeProcesses) {
        Write-Host "  - Deteniendo proceso dotnet ID: $($proc.Id)" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force
    }
}

# Esperar un momento para que se liberen los archivos
Start-Sleep -Seconds 2

# Limpiar directorios bin y obj (opcional pero recomendado)
$cleanBin = Read-Host "¿Deseas limpiar los directorios bin y obj? (s/n)"
if ($cleanBin -eq 's' -or $cleanBin -eq 'S') {
    Write-Host "Limpiando directorios de compilación..." -ForegroundColor Cyan
    
    $projectPath = "F:\27 Kavak\baba-chatbot-net\src\Baba.Chatbot.Api"
    
    if (Test-Path "$projectPath\bin") {
        Remove-Item -Path "$projectPath\bin" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  - bin/ eliminado" -ForegroundColor Green
    }
    
    if (Test-Path "$projectPath\obj") {
        Remove-Item -Path "$projectPath\obj" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  - obj/ eliminado" -ForegroundColor Green
    }
    
    Write-Host "Limpieza completada" -ForegroundColor Green
}

Write-Host "`n=== Proceso completado ===" -ForegroundColor Green
Write-Host "Ahora puedes compilar tu proyecto nuevamente" -ForegroundColor Cyan

