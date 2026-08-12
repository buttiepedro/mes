#requires -Version 5.1
<#
.SYNOPSIS
  Levanta el entorno local de Nexo: infraestructura (docker compose) + las 4 APIs del MVP.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts\run-local.ps1
  powershell -ExecutionPolicy Bypass -File scripts\run-local.ps1 -Migrate   # además aplica migraciones EF
  powershell -ExecutionPolicy Bypass -File scripts\run-local.ps1 -NoBuild   # arranca sin recompilar (más rápido)
.NOTES
  Las APIs quedan corriendo en segundo plano. Para frenarlas: scripts\stop-local.ps1
#>
param([switch]$Migrate, [switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# PATH robusto (dotnet + docker) por si la sesión no los tiene cargados.
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' +
            [Environment]::GetEnvironmentVariable('Path','User') +
            ";C:\Program Files\dotnet;C:\Program Files\Docker\Docker\resources\bin;$env:USERPROFILE\.dotnet\tools"

Write-Host '== Infra (docker compose) ==' -ForegroundColor Cyan
docker compose -f docker-compose.cloud.yml up -d postgres redpanda redpanda-console minio jaeger | Out-Null
for ($i = 0; $i -lt 30; $i++) {
  if ((docker inspect --format '{{.State.Health.Status}}' nexo-postgres-1 2>$null) -eq 'healthy') { break }
  Start-Sleep -Seconds 2
}
Write-Host '   Postgres :5433 · Redpanda :9092 (console :8080) · MinIO :9000/9001 · Jaeger :16686'

if (-not $NoBuild) {
  Write-Host '== Build ==' -ForegroundColor Cyan
  dotnet build nexo.sln --nologo -v q
}

$services = @(
  @{ Name = 'MesApi';      Port = 5085; Proj = 'src\cloud\Nexo.MesApi\Nexo.MesApi.Api' },
  @{ Name = 'RulesEngine'; Port = 5086; Proj = 'src\cloud\Nexo.RulesEngine' },
  @{ Name = 'EventEngine'; Port = 5084; Proj = 'src\Services\Nexo.EventEngine\Nexo.EventEngine.Api' }
)

if ($Migrate) {
  Write-Host '== Migraciones EF (idempotente) ==' -ForegroundColor Cyan
  foreach ($s in $services) {
    $infra = $s.Proj -replace '\.Api$', '.Infrastructure'
    if (-not (Test-Path (Join-Path $root $infra))) { continue }  # servicios sin EF (p. ej. EventEngine)
    dotnet ef database update -p $infra -s $s.Proj
  }
}

$logdir = Join-Path $root '.local-logs'
New-Item -ItemType Directory -Path $logdir -Force | Out-Null
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$launched = @()

Write-Host '== APIs ==' -ForegroundColor Cyan
foreach ($s in $services) {
  $env:ASPNETCORE_URLS = "http://localhost:$($s.Port)"
  $runArgs = @('run', '--no-launch-profile', '--project', $s.Proj)
  if ($NoBuild) { $runArgs = @('run', '--no-build', '--no-launch-profile', '--project', $s.Proj) }
  $p = Start-Process 'dotnet' -ArgumentList $runArgs `
        -RedirectStandardOutput (Join-Path $logdir "$($s.Name).out.log") `
        -RedirectStandardError  (Join-Path $logdir "$($s.Name).err.log") `
        -PassThru -WindowStyle Hidden
  $launched += [pscustomobject]@{ Service = $s.Name; Port = $s.Port; PID = $p.Id }
}
$launched | Export-Clixml (Join-Path $logdir 'pids.xml')

Write-Host '   esperando health...'
foreach ($s in $services) {
  $ok = $false
  for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 2
    try {
      $r = Invoke-WebRequest "http://localhost:$($s.Port)/health/ready" -UseBasicParsing -TimeoutSec 2
      if ($r.StatusCode -eq 200) { $ok = $true; break }
    } catch { }
  }
  $mark = if ($ok) { 'OK ' } else { 'ERR' }
  Write-Host ("   [{0}] {1,-11} http://localhost:{2}/swagger" -f $mark, $s.Service, $s.Port)
  if (-not $ok) { Write-Host ("        ver $logdir\$($s.Service).err.log") -ForegroundColor Yellow }
}

Write-Host "`nListo. Para frenar las APIs: scripts\stop-local.ps1 (agregá -Infra para bajar también los contenedores)." -ForegroundColor Green
