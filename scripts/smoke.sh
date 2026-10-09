#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
WEB_BASE_URL="${WEB_BASE_URL:-http://localhost:3000}"
MAX_RETRIES=15
RETRY_DELAY=2

echo "=== ÁTRIO SMOKE TEST ==="
echo "API Target: ${API_BASE_URL}"
echo "Web Target: ${WEB_BASE_URL}"

test_endpoint() {
    local url="$1"
    local desc="$2"
    local expected="$3"

    echo -n "Testando: ${desc} (${url})..."
    local attempt=1

    while [ "$attempt" -le "$MAX_RETRIES" ]; do
        local code
        code=$(curl --fail --show-error --silent --max-time 15 --write-out "%{http_code}" --output /dev/null "$url" 2>/dev/null || echo "000")

        if [ "$code" = "$expected" ]; then
            echo " [OK ${code}]"
            return 0
        fi

        echo -n "."
        sleep "$RETRY_DELAY"
        attempt=$((attempt + 1))
    done

    echo " [FALHA]"
    echo "Erro: Endpoint ${url} não respondeu ${expected} após ${MAX_RETRIES} tentativas."
    return 1
}

test_endpoint "${API_BASE_URL}/health/live" "API Liveness" "200"
test_endpoint "${API_BASE_URL}/health/ready" "API Readiness" "200"
test_endpoint "${API_BASE_URL}/api/v1/system/version" "API System Version" "200"
test_endpoint "${WEB_BASE_URL}/" "Web Application Root" "200"

echo "=== TODOS OS SMOKE TESTS PASSARAM COM SUCESSO! ==="
