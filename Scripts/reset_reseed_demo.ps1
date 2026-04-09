param(
    [switch]$KeepRunning
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts\reseed-demo"
$stdoutLog = Join-Path $artifactsDir "backend-seed.stdout.log"
$stderrLog = Join-Path $artifactsDir "backend-seed.stderr.log"

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Stop-HealthCareBackend {
    $ports = @(7146, 5286)
    $connections = @()

    foreach ($port in $ports) {
        $connections += Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    }

    $pids = $connections |
        Select-Object -ExpandProperty OwningProcess -Unique |
        Where-Object { $_ -and $_ -ne 0 }

    foreach ($processId in $pids) {
        try {
            $process = Get-Process -Id $processId -ErrorAction Stop
            Write-Step "Stopping backend process PID $processId ($($process.ProcessName))"
            Stop-Process -Id $processId -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not stop PID ${processId}: $($_.Exception.Message)"
        }
    }
}

function Wait-ForBackendReady([int]$ProcessId, [string]$StdoutPath, [int]$TimeoutSeconds = 90) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        if (-not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
            throw "Backend exited before seed completed. Check logs at $StdoutPath and $stderrLog"
        }

        if (Test-Path $StdoutPath) {
            $content = Get-Content -Path $StdoutPath -Raw -ErrorAction SilentlyContinue
            if ($content -match "Now listening on:\s+https://localhost:7146" -or $content -match "Now listening on:\s+http://localhost:5286") {
                return
            }
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for backend readiness after reseed. Check logs at $StdoutPath and $stderrLog"
}

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
Remove-Item -LiteralPath $stdoutLog -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stderrLog -Force -ErrorAction SilentlyContinue

Set-Location $repoRoot

Write-Step "Stopping existing backend on ports 7146/5286"
Stop-HealthCareBackend

Write-Step "Building current source so reseed uses the latest DataSeed.cs"
dotnet build .\HealthCare.csproj

Write-Step "Dropping local database via EF"
dotnet ef database drop --force --no-build

Write-Step "Recreating schema via EF migrations"
dotnet ef database update --no-build

Write-Step "Starting backend once so startup seeding can populate demo data"
$process = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--no-build", "--launch-profile", "https") `
    -WorkingDirectory $repoRoot `
    -RedirectStandardOutput $stdoutLog `
    -RedirectStandardError $stderrLog `
    -PassThru

try {
    Wait-ForBackendReady -ProcessId $process.Id -StdoutPath $stdoutLog
    Write-Step "Demo seed applied successfully"
}
finally {
    if (-not $KeepRunning) {
        if (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) {
            Write-Step "Stopping temporary backend process"
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ""
Write-Host "Demo DB has been reset and reseeded." -ForegroundColor Green
Write-Host "Stdout log: $stdoutLog"
Write-Host "Stderr log: $stderrLog"
if ($KeepRunning) {
    Write-Host "Backend is still running on https://localhost:7146" -ForegroundColor Yellow
}
