param (
    [string]$ApiBaseUrl = $(if ($env:API_BASE_URL) { $env:API_BASE_URL } else { "http://localhost:5000" }),
    [string]$WebBaseUrl = $(if ($env:WEB_BASE_URL) { $env:WEB_BASE_URL } else { "http://localhost:3000" }),
    [int]$MaxRetries = 15,
    [int]$RetryDelaySeconds = 2
)

$ErrorActionPreference = "Stop"

Write-Host "=== ÁTRIO SMOKE TEST ===" -ForegroundColor Cyan
Write-Host "API Target: $ApiBaseUrl"
Write-Host "Web Target: $WebBaseUrl"

function Test-EndpointWithRetry {
    param (
        [string]$Url,
        [string]$Description,
        [int]$ExpectedStatusCode = 200
    )

    Write-Host "`nTestando: $Description ($Url)..." -NoNewline
    $attempt = 1

    while ($attempt -le $MaxRetries) {
        try {
            $response = & curl.exe --fail --show-error --silent --max-time 15 --write-out "%{http_code}" --output nul "$Url"
            if ($LASTEXITCODE -eq 0 -and $response -eq "$ExpectedStatusCode") {
                Write-Host " [OK $response]" -ForegroundColor Green
                return $true
            }
        }
        catch {
            # curl command failed
        }

        Write-Host "." -NoNewline
        Start-Sleep -Seconds $RetryDelaySeconds
        $attempt++
    }

    Write-Host " [FALHA]" -ForegroundColor Red
    Write-Error "Endpoint $Url não respondeu status $ExpectedStatusCode após $MaxRetries tentativas."
    return $false
}

# 1. Liveness
Test-EndpointWithRetry -Url "$ApiBaseUrl/health/live" -Description "API Liveness" -ExpectedStatusCode 200

# 2. Readiness (PostgreSQL Reachable)
Test-EndpointWithRetry -Url "$ApiBaseUrl/health/ready" -Description "API Readiness" -ExpectedStatusCode 200

# 3. System Version Endpoint
Test-EndpointWithRetry -Url "$ApiBaseUrl/api/v1/system/version" -Description "API System Version" -ExpectedStatusCode 200

# 4. Web Root
Test-EndpointWithRetry -Url "$WebBaseUrl/" -Description "Web Application Root" -ExpectedStatusCode 200

Write-Host "`n=== TODOS OS SMOKE TESTS PASSARAM COM SUCESSO! ===" -ForegroundColor Green
exit 0
