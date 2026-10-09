param (
    [string]$AppBaseUrl = $(if ($env:APP_BASE_URL) { $env:APP_BASE_URL } elseif ($env:WEB_BASE_URL) { $env:WEB_BASE_URL } else { "http://localhost:3000" }),
    [string]$ApiDirectUrl = $(if ($env:API_BASE_URL) { $env:API_BASE_URL } else { "" }),
    [int]$MaxRetries = 15,
    [int]$RetryDelaySeconds = 2
)

$ErrorActionPreference = "Stop"

Write-Host "=== ÁTRIO SMOKE TEST ===" -ForegroundColor Cyan
Write-Host "Public App Target (Same-Origin): $AppBaseUrl"
if ($ApiDirectUrl) {
    Write-Host "Direct API Target: $ApiDirectUrl"
}

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

function Test-VersionEndpointJson {
    param (
        [string]$Url,
        [string]$Description
    )

    Write-Host "`nTestando: $Description ($Url)..." -NoNewline
    $attempt = 1

    while ($attempt -le $MaxRetries) {
        try {
            $rawJson = & curl.exe --fail --show-error --silent --max-time 15 "$Url"
            if ($LASTEXITCODE -eq 0 -and (-not [string]::IsNullOrWhiteSpace($rawJson))) {
                # Rejeita HTML fallback
                if (-not $rawJson.TrimStart().StartsWith("<")) {
                    $parsed = $null
                    try {
                        $parsed = $rawJson | ConvertFrom-Json -ErrorAction Stop
                    }
                    catch {
                        $parsed = $null
                    }

                    # Exige objeto JSON não nulo (não array, não primitivo)
                    if ($null -ne $parsed -and ($parsed -is [System.Management.Automation.PSCustomObject]) -and (-not ($parsed -is [System.Array]))) {
                        $versionProp = $parsed.PSObject.Properties["version"]
                        $commitProp = $parsed.PSObject.Properties["commit"]

                        if ($null -ne $versionProp -and $null -ne $commitProp) {
                            $v = $versionProp.Value
                            $c = $commitProp.Value

                            # Exige strings não nulas e não vazias
                            if (($v -is [string]) -and (-not [string]::IsNullOrWhiteSpace($v)) -and `
                                ($c -is [string]) -and (-not [string]::IsNullOrWhiteSpace($c))) {
                                Write-Host " [OK - Version: $($v.Trim()), Commit: $($c.Trim())]" -ForegroundColor Green
                                return $true
                            }
                        }
                    }
                }
            }
        }
        catch {
            # curl or parse failed
        }

        Write-Host "." -NoNewline
        Start-Sleep -Seconds $RetryDelaySeconds
        $attempt++
    }

    Write-Host " [FALHA]" -ForegroundColor Red
    Write-Error "Endpoint $Url não retornou objeto JSON válido com propriedades 'version' e 'commit' contendo strings não vazias após $MaxRetries tentativas."
    return $false
}

# 1. Web Application Root (SPA)
Test-EndpointWithRetry -Url "$AppBaseUrl/" -Description "Web Application Root (Same-Origin)" -ExpectedStatusCode 200

# 2. System Version Endpoint através da mesma origem pública (rejeita HTML, exige JSON com version e commit)
Test-VersionEndpointJson -Url "$AppBaseUrl/api/v1/system/version" -Description "API System Version (Same-Origin JSON)"

# 3. Liveness e Readiness através da mesma origem pública
Test-EndpointWithRetry -Url "$AppBaseUrl/health/live" -Description "API Liveness (Same-Origin)" -ExpectedStatusCode 200
Test-EndpointWithRetry -Url "$AppBaseUrl/health/ready" -Description "API Readiness (Same-Origin)" -ExpectedStatusCode 200

# 4. Caso porta direta da API esteja informada, valida diretamente também
if ($ApiDirectUrl) {
    Test-VersionEndpointJson -Url "$ApiDirectUrl/api/v1/system/version" -Description "Direct API Version"
}

Write-Host "`n=== TODOS OS SMOKE TESTS PASSARAM COM SUCESSO! ===" -ForegroundColor Green
exit 0
