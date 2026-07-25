#requires -Version 5.1
<#
.SYNOPSIS
  Frena las APIs locales de Nexo levantadas por run-local.ps1.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
  powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1 -Infra   # además baja los contenedores
#>
param([switch]$Infra)

$root = Split-Path -Parent $PSScriptRoot
$logdir = Join-Path $root '.local-logs'
$pidsFile = Join-Path $logdir 'pids.xml'

if (Test-Path $pidsFile) {
  foreach ($e in (Import-Clixml $pidsFile)) {
    try {
      Stop-Process -Id $e.PID -Force -ErrorAction Stop
      Write-Host ("detenido {0,-11} (PID {1})" -f $e.Service, $e.PID)
    } catch {
      Write-Host ("{0,-11} ya no estaba corriendo" -f $e.Service)
    }
  }
  Remove-Item $pidsFile -Force -ErrorAction SilentlyContinue
} else {
  Write-Host 'No hay .local-logs\pids.xml; no hay APIs registradas para frenar.'
}

if ($Infra) {
  $env:Path += ';C:\Program Files\Docker\Docker\resources\bin'
  Set-Location $root
  docker compose stop
}
