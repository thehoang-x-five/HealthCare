param(
    [string]$BackendProject = "C:\Users\THINKPAD\Documents\GitHub\HealthCare\HealthCare.csproj",
    [string]$FrontendRepo = "C:\Users\THINKPAD\Documents\GitHub\my-patients",
    [string]$BaseUrl = "https://127.0.0.1:7146",
    [switch]$SkipFrontendBuild,
    [switch]$SkipApiSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$repoRoot = Split-Path -Parent $BackendProject
$artifactsDir = Join-Path $repoRoot "artifacts\week5-smoke"
$backendBuildDir = Join-Path $artifactsDir "backend-build"
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
New-Item -ItemType Directory -Force -Path $backendBuildDir | Out-Null

$reportJson = Join-Path $artifactsDir "smoke-report.json"
$reportMd = Join-Path $artifactsDir "smoke-report.md"
$backendStdOut = Join-Path $artifactsDir "backend.stdout.log"
$backendStdErr = Join-Path $artifactsDir "backend.stderr.log"

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        $Body = $null
    )

    try {
        $responseFile = [System.IO.Path]::GetTempFileName()
        $requestFile = $null

        try {
            $curlArgs = @("-k", "-sS", "--http1.1", "-X", $Method, "-o", $responseFile, "-w", "%{http_code}")

            foreach ($key in $Headers.Keys) {
                $curlArgs += @("-H", "${key}: $($Headers[$key])")
            }

            if ($null -ne $Body) {
                $requestFile = [System.IO.Path]::GetTempFileName()
                ($Body | ConvertTo-Json -Depth 10) | Set-Content -Path $requestFile -Encoding UTF8
                $curlArgs += @("-H", "Content-Type: application/json", "--data-binary", "@$requestFile")
            }

            $curlArgs += $Uri
            $statusText = (& curl.exe @curlArgs).Trim()
            $statusCode = 0
            [void][int]::TryParse($statusText, [ref]$statusCode)

            $rawBody = if (Test-Path $responseFile) { Get-Content -Path $responseFile -Raw } else { "" }
            $parsedData = $null

            if ($statusCode -ge 200 -and $statusCode -lt 300 -and -not [string]::IsNullOrWhiteSpace($rawBody)) {
                try {
                    $parsedData = $rawBody | ConvertFrom-Json
                }
                catch {
                    $parsedData = $rawBody
                }
            }

            return [pscustomobject]@{
                StatusCode = $statusCode
                Success = ($statusCode -ge 200 -and $statusCode -lt 300)
                Data = $parsedData
                Error = if ($statusCode -ge 200 -and $statusCode -lt 300) { $null } else { $rawBody }
            }
        }
        finally {
            if ($requestFile -and (Test-Path $requestFile)) {
                Remove-Item $requestFile -Force
            }
            if (Test-Path $responseFile) {
                Remove-Item $responseFile -Force
            }
        }
    }
    catch {
        return [pscustomobject]@{
            StatusCode = 0
            Success = $false
            Data = $null
            Error = $_.Exception.Message
        }
    }
}

function Wait-For-Backend {
    param([string]$ProbeUrl)

    for ($i = 0; $i -lt 90; $i++) {
        try {
            $statusText = & curl.exe -k -s -o NUL -w "%{http_code}" $ProbeUrl
            $statusCode = 0
            [void][int]::TryParse($statusText, [ref]$statusCode)
            if ($statusCode -in 200, 401, 403) {
                return $true
            }
        }
        catch {
            Start-Sleep -Milliseconds 750
        }
        Start-Sleep -Milliseconds 750
    }

    return $false
}

function New-TestResult {
    param(
        [string]$Role,
        [string]$Endpoint,
        [int]$Expected,
        [int]$Actual,
        [bool]$Passed,
        [string]$Note
    )

    [pscustomobject]@{
        role = $Role
        endpoint = $Endpoint
        expected = $Expected
        actual = $Actual
        passed = $Passed
        note = $Note
    }
}

Write-Section "Backend build"
dotnet build $BackendProject --no-restore -o $backendBuildDir /clp:ErrorsOnly /nologo /m:1

if (-not $SkipFrontendBuild) {
    Write-Section "Frontend build"
    Push-Location $FrontendRepo
    try {
        npm run build
    }
    finally {
        Pop-Location
    }
}

$results = New-Object System.Collections.Generic.List[object]
$summary = [ordered]@{
    generatedAt = (Get-Date).ToString("s")
    baseUrl = $BaseUrl
    backendProject = $BackendProject
    frontendRepo = $FrontendRepo
    buildOnly = [bool]$SkipApiSmoke
    tests = @()
}

if (-not $SkipApiSmoke) {
    Write-Section "Backend API smoke"

    if (Test-Path $backendStdOut) { Remove-Item $backendStdOut -Force }
    if (Test-Path $backendStdErr) { Remove-Item $backendStdErr -Force }

    $originalUrls = $env:ASPNETCORE_URLS
    $originalEnv = $env:ASPNETCORE_ENVIRONMENT
    $env:ASPNETCORE_URLS = $BaseUrl
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    $backendProc = $null
    try {
        $backendDll = Join-Path $backendBuildDir "HealthCare.dll"
        if (-not (Test-Path $backendDll)) {
            throw "Built backend DLL not found at $backendDll"
        }

        $backendProc = Start-Process -FilePath "dotnet" `
            -ArgumentList @($backendDll) `
            -WorkingDirectory $backendBuildDir `
            -PassThru `
            -RedirectStandardOutput $backendStdOut `
            -RedirectStandardError $backendStdErr

        $probeUrl = ($BaseUrl.TrimEnd("/")) + "/api/patient?page=1&pageSize=1"
        if (-not (Wait-For-Backend -ProbeUrl $probeUrl)) {
            throw "Backend did not become ready in time. See $backendStdOut and $backendStdErr"
        }

        $roles = @(
            @{
                role = "admin"
                username = "admin"
                password = "Admin@123"
                optional = $true
                expected = @{
                    dashboard = 200
                    appointments = 200
                    patients = 200
                    reports = 200
                    adminUsers = 200
                }
            },
            @{
                role = "yta_hc"
                username = "yt_hc01"
                password = "P@ssw0rd"
                optional = $false
                expected = @{
                    dashboard = 200
                    appointments = 200
                    patients = 200
                    reports = 200
                    adminUsers = 403
                }
            },
            @{
                role = "yta_ls"
                username = "yt_ls01"
                password = "P@ssw0rd"
                optional = $false
                expected = @{
                    dashboard = 200
                    appointments = 403
                    patients = 200
                    reports = 403
                    adminUsers = 403
                }
            },
            @{
                role = "yta_cls"
                username = "yt_cls01"
                password = "P@ssw0rd"
                optional = $false
                expected = @{
                    dashboard = 200
                    appointments = 403
                    patients = 200
                    reports = 403
                    adminUsers = 403
                }
            },
            @{
                role = "bac_si"
                username = "bs_noi01"
                password = "P@ssw0rd"
                optional = $false
                expected = @{
                    dashboard = 200
                    appointments = 403
                    patients = 200
                    reports = 200
                    adminUsers = 403
                }
            },
            @{
                role = "ktv"
                username = "ktv_xn_01"
                password = "KTV@123"
                optional = $false
                expected = @{
                    dashboard = 200
                    appointments = 403
                    patients = 200
                    reports = 403
                    adminUsers = 403
                }
            }
        )

        $reportPayload = @{
            FromDate = (Get-Date).Date.AddDays(-7).ToString("o")
            ToDate = (Get-Date).Date.AddDays(1).AddSeconds(-1).ToString("o")
            GroupBy = "day"
        }

        foreach ($roleInfo in $roles) {
            $loginResp = Invoke-JsonRequest -Method "POST" -Uri (($BaseUrl.TrimEnd("/")) + "/api/auth/login") -Body @{
                TenDangNhap = $roleInfo.username
                MatKhau = $roleInfo.password
            }

            if (-not $loginResp.Success) {
                $note = "Login failed"
                if ($roleInfo.optional) {
                    $note = "Optional seeded admin account may not exist in the current DB snapshot"
                }

                $results.Add((New-TestResult -Role $roleInfo.role -Endpoint "POST /api/auth/login" -Expected 200 -Actual $loginResp.StatusCode -Passed $roleInfo.optional -Note $note))
                continue
            }

            $results.Add((New-TestResult -Role $roleInfo.role -Endpoint "POST /api/auth/login" -Expected 200 -Actual 200 -Passed $true -Note "Login succeeded"))

            $token = $loginResp.Data.AccessToken
            $headers = @{ Authorization = "Bearer $token" }

            $checks = @(
                @{ key = "dashboard"; method = "GET"; uri = "/api/dashboard/today"; body = $null; label = "GET /api/dashboard/today" },
                @{ key = "appointments"; method = "POST"; uri = "/api/appointments/search"; body = @{}; label = "POST /api/appointments/search" },
                @{ key = "patients"; method = "GET"; uri = "/api/patient?page=1&pageSize=1"; body = $null; label = "GET /api/patient" },
                @{ key = "reports"; method = "POST"; uri = "/api/reports/overview"; body = $reportPayload; label = "POST /api/reports/overview" },
                @{ key = "adminUsers"; method = "GET"; uri = "/api/admin/users?page=1&pageSize=1"; body = $null; label = "GET /api/admin/users" }
            )

            foreach ($check in $checks) {
                $expected = [int]$roleInfo.expected[$check.key]
                $resp = Invoke-JsonRequest -Method $check.method -Uri (($BaseUrl.TrimEnd("/")) + $check.uri) -Headers $headers -Body $check.body
                $actual = [int]$resp.StatusCode
                $passed = $actual -eq $expected
                $note = if ($resp.Success) { "OK" } else { ($resp.Error | Out-String).Trim() }
                $results.Add((New-TestResult -Role $roleInfo.role -Endpoint $check.label -Expected $expected -Actual $actual -Passed $passed -Note $note))
            }
        }
    }
    finally {
        if ($backendProc -and -not $backendProc.HasExited) {
            Stop-Process -Id $backendProc.Id -Force
        }

        $env:ASPNETCORE_URLS = $originalUrls
        $env:ASPNETCORE_ENVIRONMENT = $originalEnv
    }
}

$summary.tests = $results
$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $reportJson -Encoding UTF8

$lines = @()
$lines += "# Week 5 Smoke Report"
$lines += ""
$lines += "- Generated: $($summary.generatedAt)"
$lines += "- Base URL: $BaseUrl"
$lines += ""
$lines += "| Role | Endpoint | Expected | Actual | Pass | Note |"
$lines += "|---|---|---:|---:|:---:|---|"
foreach ($row in $results) {
    $passMark = if ($row.passed) { "✅" } else { "❌" }
    $note = ($row.note -replace '\r?\n', ' ') -replace '\|', '/'
    $lines += "| $($row.role) | $($row.endpoint) | $($row.expected) | $($row.actual) | $passMark | $note |"
}
$lines | Set-Content -Path $reportMd -Encoding UTF8

Write-Section "Artifacts"
Write-Host $reportJson
Write-Host $reportMd

$failed = @($results | Where-Object { -not $_.passed })
if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Smoke verification completed with failures." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Smoke verification completed successfully." -ForegroundColor Green
