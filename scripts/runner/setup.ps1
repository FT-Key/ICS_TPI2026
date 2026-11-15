#Requires -Version 5.1
<#
.SYNOPSIS
    Registra este repositorio como runner self-hosted dentro de WSL.

.DESCRIPTION
    Se ejecuta una sola vez:
        .\scripts\runner\setup.ps1

    Descarga la ultima version del runner de GitHub, lo registra contra el
    repositorio detectado en origin y le asigna el label ics-wsl.

    No instala .NET, Node ni Docker: los workflows usan actions/setup-dotnet y
    actions/setup-node, que descargan las herramientas en cada job. Docker solo
    hace falta para el job de docker-build.

.PARAMETER Distro
    Distro de WSL. Por defecto Ubuntu.

.PARAMETER Label
    Label con el que los workflows seleccionan el runner. Por defecto ics-wsl.

.PARAMETER Name
    Nombre visible del runner en GitHub.

.PARAMETER Force
    Vuelve a registrar aunque ya exista un .runner en la carpeta.

.PARAMETER DryRun
    Muestra los comandos sin ejecutarlos.
#>
[CmdletBinding()]
param(
    [string]$Distro = 'Ubuntu',
    [string]$Label = 'ics-wsl',
    [string]$Name = "ics-wsl-$env:COMPUTERNAME",
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))

function Write-Step { param([string]$Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Ok { param([string]$Message) Write-Host $Message -ForegroundColor Green }
function Write-WarnLine { param([string]$Message) Write-Host $Message -ForegroundColor Yellow }
function Write-FailLine { param([string]$Message) Write-Host $Message -ForegroundColor Red }

$remote = (& git -C $repoRoot remote get-url origin 2>$null | Out-String).Trim()
if ($remote -notmatch 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?/*$') {
    Write-FailLine "No pude detectar el repositorio desde origin: $remote"
    exit 1
}
$slug = "$($Matches.owner)/$($Matches.repo)"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-FailLine 'Falta GitHub CLI. Instalalo con: winget install GitHub.cli'
    exit 1
}
& gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-FailLine 'GitHub CLI no esta autenticado. Ejecuta: gh auth login'
    exit 1
}

$homeDir = (& wsl.exe -d $Distro -- bash -lc 'echo $HOME' 2>$null | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($homeDir)) {
    Write-FailLine "No pude entrar a la distro '$Distro'. Verifica con: wsl -l -v"
    exit 1
}
$runnerDir = "$homeDir/actions-runner"

$alreadyRegistered = $false
& wsl.exe -d $Distro -- test -f "$runnerDir/.runner" *> $null
if ($LASTEXITCODE -eq 0) { $alreadyRegistered = $true }

if ($alreadyRegistered -and -not $Force) {
    Write-Ok "El runner ya esta registrado en $runnerDir"
    Write-Host 'No hace falta volver a configurarlo. Arrancalo con:' -ForegroundColor White
    Write-Host '  .\runner.ps1' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Para forzar un registro nuevo usa -Force.' -ForegroundColor DarkGray
    exit 0
}

$version = (& gh api repos/actions/runner/releases/latest --jq '.tag_name' 2>$null | Out-String).Trim().TrimStart('v')
if ([string]::IsNullOrWhiteSpace($version)) {
    Write-FailLine 'No pude obtener la ultima version del runner.'
    exit 1
}

$arch = (& wsl.exe -d $Distro -- uname -m 2>$null | Out-String).Trim()
$assetArch = if ($arch -eq 'aarch64') { 'arm64' } else { 'x64' }
$asset = "actions-runner-linux-$assetArch-$version.tar.gz"
$downloadUrl = "https://github.com/actions/runner/releases/download/v$version/$asset"

$token = (& gh api -X POST "repos/$slug/actions/runners/registration-token" --jq '.token' 2>$null | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-FailLine 'No pude generar el token de registro. Verifica que tu usuario sea admin del repositorio.'
    exit 1
}

Write-Host ''
Write-Step "Repositorio : $slug"
Write-Step "Runner      : $Name"
Write-Step "Label       : $Label"
Write-Step "Version     : $version ($assetArch)"
Write-Step "Carpeta     : $runnerDir"
Write-Host ''

if ($DryRun) {
    Write-WarnLine 'DryRun: no se descarga ni se registra nada.'
    Write-Host ''
    Write-Host "  curl -fsSL -o runner.tar.gz $downloadUrl"
    Write-Host "  tar xzf runner.tar.gz"
    Write-Host "  ./config.sh --unattended --url https://github.com/$slug --token <token> --labels $Label --name $Name --work _work --replace"
    exit 0
}

$setupScript = @'
set -e
RUNNER_DIR="__RUNNER_DIR__"
mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"

if [ ! -f ./run.sh ]; then
  echo "Descargando runner __VERSION__..."
  curl -fsSL -o runner.tar.gz "__DOWNLOAD_URL__"
  tar xzf runner.tar.gz
  rm -f runner.tar.gz
else
  echo "Runner ya descargado, reutilizando binarios."
fi

./config.sh --unattended \
  --url "https://github.com/__SLUG__" \
  --token "__TOKEN__" \
  --labels "__LABEL__" \
  --name "__NAME__" \
  --work "_work" \
  --replace

echo ""
echo "Registro completado."
'@

$setupScript = $setupScript.
    Replace('__RUNNER_DIR__', $runnerDir).
    Replace('__VERSION__', $version).
    Replace('__DOWNLOAD_URL__', $downloadUrl).
    Replace('__SLUG__', $slug).
    Replace('__TOKEN__', $token).
    Replace('__LABEL__', $Label).
    Replace('__NAME__', $Name)

$hasIcu = $false
& wsl.exe -d $Distro -- bash -lc 'ldconfig -p | grep -q libicu' *> $null
if ($LASTEXITCODE -eq 0) { $hasIcu = $true }
if (-not $hasIcu) {
    Write-WarnLine 'Falta libicu en WSL: el runner de GitHub lo necesita.'
    Write-Host '  Instalalo con:' -ForegroundColor White
    Write-Host "    wsl -d $Distro -u root -- bash -lc 'apt-get update && apt-get install -y libicu-dev libssl-dev'" -ForegroundColor Cyan
    Write-Host '  Despues volve a ejecutar este script.' -ForegroundColor DarkGray
    exit 1
}

Write-Step 'Registrando runner en WSL...'
$encoded = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($setupScript))
$output = (& wsl.exe -d $Distro -- bash -lc "echo $encoded | base64 -d | bash" 2>&1 | Out-String)
Write-Host $output

if ($LASTEXITCODE -ne 0) {
    Write-FailLine 'El registro fallo. Revisa el mensaje de arriba.'
    exit 1
}

& wsl.exe -d $Distro -- test -f "$runnerDir/.runner" *> $null
if ($LASTEXITCODE -ne 0) {
    Write-FailLine 'No se creo el archivo .runner. El registro no quedo completo.'
    exit 1
}

Write-Host ''
Write-Ok 'Runner registrado correctamente.'
Write-Host ''

$dockerAvailable = $false
& wsl.exe -d $Distro -- bash -lc 'command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1' *> $null
if ($LASTEXITCODE -eq 0) { $dockerAvailable = $true }

if ($dockerAvailable) {
    Write-Ok 'Docker disponible dentro de WSL.'
} else {
    Write-WarnLine 'Docker NO esta disponible dentro de WSL.'
    Write-Host '  Opcion 1: Docker Desktop > Settings > Resources > WSL Integration > habilitar Ubuntu.' -ForegroundColor DarkGray
    Write-Host '  Opcion 2: dejar el job docker-build en ubuntu-latest.' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host 'Siguiente paso:' -ForegroundColor White
Write-Host '  .\runner.ps1' -ForegroundColor Cyan
Write-Host ''
