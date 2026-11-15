#Requires -Version 5.1
<#
.SYNOPSIS
    Arranca el runner self-hosted del repo y muestra el avance de la cola de GitHub Actions.

.DESCRIPTION
    Comando unico desde la raiz del repositorio:
        .\runner.ps1

    El script levanta el runner dentro de WSL, muestra cuantos jobs quedan
    pendientes y el porcentaje completado de la sesion, y espera a que la cola
    termine antes de apagarse cuando presionas Ctrl+C.

    Modos:
        .\runner.ps1                 Arranca el runner y muestra el panel.
        .\runner.ps1 -StatusOnly     Solo muestra estado y cola, sin arrancar.
        .\runner.ps1 -Stop           Detiene un runner que quedo corriendo.
        .\runner.ps1 -Force          Permite salir aunque haya jobs en curso.

.PARAMETER Distro
    Distro de WSL donde esta instalado el runner. Por defecto Ubuntu.

.PARAMETER PollSeconds
    Cada cuantos segundos consulta GitHub. Por defecto 30.

.PARAMETER StatusOnly
    No arranca el runner: imprime el estado y la cola actual.

.PARAMETER Stop
    Detiene el proceso del runner en WSL y sale.

.PARAMETER Force
    Fuerza la salida aunque haya un job ejecutandose.
#>
[CmdletBinding()]
param(
    [string]$Distro = 'Ubuntu',
    [int]$PollSeconds = 30,
    [switch]$StatusOnly,
    [switch]$Stop,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step { param([string]$Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Ok { param([string]$Message) Write-Host $Message -ForegroundColor Green }
function Write-WarnLine { param([string]$Message) Write-Host $Message -ForegroundColor Yellow }
function Write-FailLine { param([string]$Message) Write-Host $Message -ForegroundColor Red }
function Write-Dim { param([string]$Message) Write-Host $Message -ForegroundColor DarkGray }

function Get-RepoSlug {
    $remote = (& git -C $repoRoot remote get-url origin 2>$null | Out-String).Trim()
    if (-not $remote) {
        throw "No encuentro el remoto 'origin'. Ejecuta este script desde la raiz del repositorio."
    }
    if ($remote -notmatch 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+?)(?:\.git)?/*$') {
        throw "El remoto no parece ser de GitHub: $remote"
    }
    return "$($Matches.owner)/$($Matches.repo)"
}

function Invoke-GhApiJson {
    param([string]$Path)
    $raw = (& gh api $Path 2>$null) -join "`n"
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }
    try {
        return $raw | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Get-RunnerState {
    param([string]$Slug, [string]$Label)
    $response = Invoke-GhApiJson -Path "repos/$Slug/actions/runners"
    if (-not $response) { return $null }
    $runners = @($response.runners)
    $mine = $runners | Where-Object {
        $names = @($_.labels | ForEach-Object { $_.name })
        $names -contains $Label
    } | Select-Object -First 1
    if (-not $mine) { return $null }
    return $mine
}

function Get-QueueInfo {
    param([string]$Slug)

    $queued = Invoke-GhApiJson -Path "repos/$Slug/actions/runs?status=queued&per_page=20"
    $running = Invoke-GhApiJson -Path "repos/$Slug/actions/runs?status=in_progress&per_page=20"

    $queuedRuns = @()
    $runningRuns = @()
    if ($queued) { $queuedRuns = @($queued.workflow_runs) }
    if ($running) { $runningRuns = @($running.workflow_runs) }

    $pendingJobs = 0
    $inspected = 0
    foreach ($run in @($queuedRuns + $runningRuns)) {
        if ($inspected -ge 8) { break }
        $inspected++
        $jobs = Invoke-GhApiJson -Path "repos/$Slug/actions/runs/$($run.id)/jobs?per_page=100"
        if ($jobs) {
            $pendingJobs += @($jobs.jobs | Where-Object { $_.status -ne 'completed' }).Count
        }
    }

    return [pscustomobject]@{
        QueuedRuns  = $queuedRuns.Count
        RunningRuns = $runningRuns.Count
        PendingJobs = $pendingJobs
        Truncated   = (($queuedRuns.Count + $runningRuns.Count) -gt 8)
    }
}

function Start-RunnerProcess {
    param([string]$DistroName, [string]$RunnerDir, [string]$LogPath)

    $arguments = @(
        '-d', $DistroName, '--',
        'bash', '-lc', "cd '$RunnerDir' && exec ./run.sh"
    )

    $errorLog = "$LogPath.err"
    $process = Start-Process -FilePath 'wsl.exe' `
        -ArgumentList $arguments `
        -RedirectStandardOutput $LogPath `
        -RedirectStandardError $errorLog `
        -WindowStyle Hidden `
        -PassThru

    return [pscustomobject]@{ Process = $process; ErrorLog = $errorLog }
}

function Open-LogReader {
    param([string]$Path)

    for ($i = 0; $i -lt 30; $i++) {
        if (Test-Path $Path) {
            try {
                $stream = [System.IO.File]::Open(
                    $Path,
                    [System.IO.FileMode]::Open,
                    [System.IO.FileAccess]::Read,
                    [System.IO.FileShare]::ReadWrite
                )
                return New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
            } catch {
                Start-Sleep -Milliseconds 200
            }
        } else {
            Start-Sleep -Milliseconds 200
        }
    }
    return $null
}

function Get-NewLogLines {
    param($Reader)
    $lines = New-Object System.Collections.Generic.List[string]
    if (-not $Reader) { return $lines }
    while (-not $Reader.EndOfStream) {
        $lines.Add($Reader.ReadLine())
    }
    return $lines
}

function Add-Event {
    param($Events, [string]$Text, [string]$Color = 'Gray')
    $Events.Add([pscustomobject]@{
            Time  = (Get-Date).ToString('HH:mm:ss')
            Text  = $Text
            Color = $Color
        }) | Out-Null
    while ($Events.Count -gt 6) { $Events.RemoveAt(0) }
}

function Get-ProgressBar {
    param([int]$Percent, [int]$Width = 30)
    $filled = [int][math]::Round(($Width * $Percent) / 100.0)
    if ($filled -lt 0) { $filled = 0 }
    if ($filled -gt $Width) { $filled = $Width }
    return ('[' + ('#' * $filled) + ('-' * ($Width - $filled)) + ']')
}

function Write-Dashboard {
    param(
        [string]$Slug,
        $RunnerState,
        $Queue,
        [int]$Succeeded,
        [int]$Failed,
        [string]$CurrentJob,
        $Events,
        [string]$LogPath,
        [bool]$Draining
    )

    $done = $Succeeded + $Failed
    $pending = 0
    if ($Queue) { $pending = $Queue.PendingJobs }
    $total = $done + $pending
    $percent = if ($total -gt 0) { [int][math]::Round(100.0 * $done / $total) } else { 100 }

    Clear-Host
    Write-Host '============================================================' -ForegroundColor DarkCyan
    Write-Host "  Runner self-hosted  |  $Slug" -ForegroundColor Cyan
    Write-Host '============================================================' -ForegroundColor DarkCyan

    if ($RunnerState) {
        $status = if ($RunnerState.status -eq 'online') { 'online' } else { 'offline' }
        $busy = if ($RunnerState.busy) { 'ocupado' } else { 'libre' }
        $color = if ($RunnerState.status -eq 'online') { 'Green' } else { 'Yellow' }
        Write-Host ("  Estado     {0}  |  {1}" -f $status, $busy) -ForegroundColor $color
    } else {
        Write-Host '  Estado     registrando / conectando...' -ForegroundColor Yellow
    }

    if ($Queue) {
        $truncated = if ($Queue.Truncated) { ' (parcial)' } else { '' }
        Write-Host ("  Cola       {0} en cola  |  {1} en ejecucion  |  {2} jobs pendientes{3}" -f `
                $Queue.QueuedRuns, $Queue.RunningRuns, $Queue.PendingJobs, $truncated)
    } else {
        Write-Host '  Cola       sin datos (revisa permisos de gh)'
    }

    Write-Host ("  Sesion     {0}/{1} jobs  |  {2} ok  |  {3} fallidos  |  {4}%" -f `
            $done, $total, $Succeeded, $Failed, $percent)
    Write-Host ("  {0} {1}%" -f (Get-ProgressBar -Percent $percent), $percent) -ForegroundColor Cyan

    if ($CurrentJob) {
        Write-Host ("  Actual     {0}" -f $CurrentJob) -ForegroundColor White
    } elseif ($pending -gt 0) {
        Write-Host '  Actual     esperando asignacion de job...' -ForegroundColor DarkYellow
    } else {
        Write-Host '  Actual     sin trabajos pendientes' -ForegroundColor Green
    }

    Write-Host '------------------------------------------------------------' -ForegroundColor DarkGray
    if ($Events.Count -eq 0) {
        Write-Host '  (sin eventos todavia)' -ForegroundColor DarkGray
    } else {
        foreach ($event in $Events) {
            Write-Host ("  {0}  {1}" -f $event.Time, $event.Text) -ForegroundColor $event.Color
        }
    }
    Write-Host '------------------------------------------------------------' -ForegroundColor DarkGray
    Write-Dim "  Log: $LogPath"
    if ($Draining) {
        Write-WarnLine '  Cola en curso: se cierra sola cuando termine. Ctrl+C de nuevo para forzar.'
    } else {
        Write-Dim '  Ctrl+C: espera a que termine la cola y sale.'
    }
}

# ---------------------------------------------------------------- preparacion

try {
    $slug = Get-RepoSlug
} catch {
    Write-FailLine $_.Exception.Message
    exit 1
}

$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    Write-FailLine 'Falta GitHub CLI. Instalalo con: winget install GitHub.cli'
    exit 1
}

& gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-FailLine 'GitHub CLI no esta autenticado. Ejecuta: gh auth login'
    exit 1
}

$wsl = Get-Command wsl.exe -ErrorAction SilentlyContinue
if (-not $wsl) {
    Write-FailLine 'No encuentro WSL. Instalalo con: wsl --install'
    exit 1
}

$homeDir = (& wsl.exe -d $Distro -- bash -lc 'echo $HOME' 2>$null | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($homeDir)) {
    Write-FailLine "No pude entrar a la distro '$Distro'. Verifica con: wsl -l -v"
    exit 1
}

$runnerDir = "$homeDir/actions-runner"
$label = 'ics-wsl'

$runnerInstalled = $false
& wsl.exe -d $Distro -- test -f "$runnerDir/run.sh" *> $null
if ($LASTEXITCODE -eq 0) { $runnerInstalled = $true }

$runnerRegistered = $false
& wsl.exe -d $Distro -- test -f "$runnerDir/.runner" *> $null
if ($LASTEXITCODE -eq 0) { $runnerRegistered = $true }

$listenerPid = (& wsl.exe -d $Distro -- pgrep -f 'actions-runner/bin/Runner.Listener' 2>$null | Out-String).Trim()

if ($Stop) {
    if (-not $listenerPid) {
        Write-Ok 'No hay ningun runner corriendo.'
        exit 0
    }
    Write-Step "Deteniendo runner (PID $listenerPid)..."
    & wsl.exe -d $Distro -- pkill -TERM -f 'actions-runner/bin/Runner.Listener' *> $null
    Start-Sleep -Seconds 3
    $stillRunning = (& wsl.exe -d $Distro -- pgrep -f 'actions-runner/bin/Runner.Listener' 2>$null | Out-String).Trim()
    if ($stillRunning) {
        & wsl.exe -d $Distro -- pkill -KILL -f 'actions-runner/bin/Runner.Listener' *> $null
    }
    Write-Ok 'Runner detenido.'
    exit 0
}

$runnerState = Get-RunnerState -Slug $slug -Label $label
$queue = Get-QueueInfo -Slug $slug

if ($StatusOnly) {
    Write-Host ''
    Write-Step "Repositorio : $slug"
    Write-Step "Runner      : $label"
    if ($runnerState) {
        $stateText = if ($runnerState.busy) { 'ocupado' } else { 'libre' }
        Write-Host ("Estado      : {0} | {1}" -f $runnerState.status, $stateText)
    } else {
        Write-WarnLine 'Estado      : no registrado o sin permisos para verlo'
    }
    if ($queue) {
        Write-Host ("Cola        : {0} en cola | {1} en ejecucion | {2} jobs pendientes" -f `
                $queue.QueuedRuns, $queue.RunningRuns, $queue.PendingJobs)
    }
    Write-Host ''
    if ($listenerPid) { Write-Ok "Proceso local : corriendo (PID $listenerPid)" } else { Write-Dim 'Proceso local : detenido' }
    exit 0
}

if (-not $runnerInstalled -or -not $runnerRegistered) {
    Write-FailLine 'El runner todavia no esta instalado o registrado en WSL.'
    Write-Host ''
    Write-Host '  Ejecuta una sola vez:`n' -ForegroundColor White
    Write-Host '    .\scripts\runner\setup.ps1' -ForegroundColor Cyan
    Write-Host ''
    if (-not $runnerInstalled) { Write-Dim '  Falta: descarga del runner en ~/actions-runner' }
    if (-not $runnerRegistered) { Write-Dim '  Falta: registro del runner contra GitHub' }
    exit 1
}

if ($listenerPid) {
    Write-WarnLine "Ya hay un runner corriendo (PID $listenerPid)."
    Write-Dim 'Usa .\runner.ps1 -Stop para detenerlo o -StatusOnly para ver la cola.'
    exit 1
}

if ($runnerState -and $runnerState.status -ne 'online') {
    Write-WarnLine "El runner figura '$($runnerState.status)' en GitHub. Puede tardar unos segundos en conectarse."
}

# ---------------------------------------------------------------- ejecucion

$sessionId = [guid]::NewGuid().ToString('N').Substring(0, 8)
$logPath = Join-Path $env:TEMP "ics-runner-$sessionId.log"

Write-Step "Levantando runner en WSL ($Distro)..."
$started = Start-RunnerProcess -DistroName $Distro -RunnerDir $runnerDir -LogPath $logPath
$process = $started.Process
$errorLog = $started.ErrorLog

$reader = Open-LogReader -Path $logPath
$events = New-Object System.Collections.ArrayList
$succeeded = 0
$failed = 0
$currentJob = $null
$draining = $false
$exitLoop = $false
$lastPoll = [datetime]::MinValue
$canReadKeys = $false

try {
    [Console]::TreatControlCAsInput = $true
    $null = [Console]::KeyAvailable
    $canReadKeys = $true
} catch {
    $canReadKeys = $false
}

try {
    Add-Event -Events $events -Text "Runner iniciado (sesion $sessionId)" -Color 'Cyan'

    while (-not $exitLoop) {
        # Log del runner
        foreach ($line in (Get-NewLogLines -Reader $reader)) {
            if ($line -match 'Running job:\s*(.+?)\s*$') {
                $currentJob = $Matches[1]
                Add-Event -Events $events -Text "Job iniciado: $currentJob" -Color 'White'
            } elseif ($line -match 'Job\s+(.+?)\s+completed with result:\s*(\w+)') {
                $jobName = $Matches[1]
                $result = $Matches[2]
                if ($result -eq 'Succeeded') {
                    $succeeded++
                    Add-Event -Events $events -Text "$jobName -> $result" -Color 'Green'
                } else {
                    $failed++
                    Add-Event -Events $events -Text "$jobName -> $result" -Color 'Red'
                }
                $currentJob = $null
            } elseif ($line -match 'Listening for Jobs') {
                Add-Event -Events $events -Text 'Runner conectado a GitHub' -Color 'Green'
            } elseif ($line -match 'Runner reconnected') {
                Add-Event -Events $events -Text 'Runner reconectado' -Color 'Green'
            } elseif ($line -match '(?i)(error|exception|failed to)') {
                Add-Event -Events $events -Text ($line.Trim()) -Color 'DarkYellow'
            }
        }

        # Ctrl+C
        if ($canReadKeys) {
            try {
                if ([Console]::KeyAvailable) {
                    $key = [Console]::ReadKey($true)
                    if ($key.Key -eq 'C' -and (($key.Modifiers -band [ConsoleModifiers]::Control) -ne 0)) {
                        if ($Force) {
                            $exitLoop = $true
                        } elseif ($draining) {
                            Write-WarnLine 'Forzando salida...'
                            $exitLoop = $true
                        } else {
                            $draining = $true
                            $lastPoll = [datetime]::MinValue
                        }
                    }
                }
            } catch {
                $canReadKeys = $false
            }
        }

        # Poll a GitHub
        if (((Get-Date) - $lastPoll).TotalSeconds -ge $PollSeconds) {
            $lastPoll = Get-Date
            $runnerState = Get-RunnerState -Slug $slug -Label $label
            $queue = Get-QueueInfo -Slug $slug

            Write-Dashboard `
                -Slug $slug `
                -RunnerState $runnerState `
                -Queue $queue `
                -Succeeded $succeeded `
                -Failed $failed `
                -CurrentJob $currentJob `
                -Events $events `
                -LogPath $logPath `
                -Draining $draining

            $pendingNow = if ($queue) { $queue.PendingJobs } else { 0 }
            $busy = ($null -ne $currentJob) -or ($runnerState -and $runnerState.busy)

            if ($draining -and $queue -and $pendingNow -eq 0 -and -not $busy) {
                Add-Event -Events $events -Text 'Cola vacia. Cerrando...' -Color 'Green'
                $exitLoop = $true
            }
        }

        if ($process.HasExited) {
            $tail = ''
            if (Test-Path $errorLog) {
                $tail = (Get-Content $errorLog -Tail 5 -ErrorAction SilentlyContinue) -join ' | '
            }
            Write-FailLine "El proceso del runner termino inesperadamente (codigo $($process.ExitCode)). $tail"
            $exitLoop = $true
        }

        Start-Sleep -Milliseconds 500
    }
} finally {
    if ($canReadKeys) {
        try { [Console]::TreatControlCAsInput = $false } catch { }
    }

    $listener = (& wsl.exe -d $Distro -- pgrep -f 'actions-runner/bin/Runner.Listener' 2>$null | Out-String).Trim()
    if ($listener) {
        Write-Step 'Deteniendo runner...'
        & wsl.exe -d $Distro -- pkill -TERM -f 'actions-runner/bin/Runner.Listener' *> $null
        Start-Sleep -Seconds 3
        $still = (& wsl.exe -d $Distro -- pgrep -f 'actions-runner/bin/Runner.Listener' 2>$null | Out-String).Trim()
        if ($still) {
            & wsl.exe -d $Distro -- pkill -KILL -f 'actions-runner/bin/Runner.Listener' *> $null
        }
    }

    if ($reader) { $reader.Dispose() }

    Write-Host ''
    Write-Host '------------------------------------------------------------' -ForegroundColor DarkCyan
    Write-Host ("  Sesion finalizada: {0} jobs ok, {1} fallidos." -f $succeeded, $failed) -ForegroundColor Cyan
    Write-Dim "  Log completo: $logPath"
    Write-Host '------------------------------------------------------------' -ForegroundColor DarkCyan
}
