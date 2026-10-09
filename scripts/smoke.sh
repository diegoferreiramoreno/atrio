#!/usr/bin/env bash
set -euo pipefail

APP_BASE_URL="${APP_BASE_URL:-${WEB_BASE_URL:-http://localhost:3000}}"
API_DIRECT_URL="${API_BASE_URL:-}"
MAX_RETRIES=15
RETRY_DELAY=2

echo "=== ÁTRIO SMOKE TEST ==="
echo "Public App Target (Same-Origin): ${APP_BASE_URL}"
if [ -n "${API_DIRECT_URL}" ]; then
    echo "Direct API Target: ${API_DIRECT_URL}"
fi

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

test_version_json() {
    local url="$1"
    local desc="$2"

    echo -n "Testando: ${desc} (${url})..."
    local attempt=1

    while [ "$attempt" -le "$MAX_RETRIES" ]; do
        local response
        response=$(curl --fail --show-error --silent --max-time 15 "$url" 2>/dev/null || true)

        # Rejeita HTML ou vazio, exige campos version e commit
        if [ -n "$response" ] && ! echo "$response" | grep -q "^[[:space:]]*<" && echo "$response" | grep -q '"version"' && echo "$response" | grep -q '"commit"'; then
            echo " [OK - JSON verificado com version e commit]"
            return 0
        fi

        echo -n "."
        sleep "$RETRY_DELAY"
        attempt=$((attempt + 1))
    done

    echo " [FALHA]"
    echo "Erro: Endpoint ${url} não retornou JSON válido com 'version' e 'commit' após ${MAX_RETRIES} tentativas."
    return 1
}

# 1. Web Application Root (SPA)
test_endpoint "${APP_BASE_URL}/" "Web Application Root (Same-Origin)" "200"

# 2. System Version Endpoint através da mesma origem pública (rejeita HTML, exige JSON)
test_version_json "${APP_BASE_URL}/api/v1/system/version" "API System Version (Same-Origin JSON)"

# 3. Liveness e Readiness através da mesma origem pública
test_endpoint "${APP_BASE_URL}/health/live" "API Liveness (Same-Origin)" "200"
test_endpoint "${APP_BASE_URL}/health/ready" "API Readiness (Same-Origin)" "200"

# 4. Caso porta direta da API esteja informada, valida diretamente também
if [ -n "${API_DIRECT_URL}" ]; then
    test_version_json "${API_DIRECT_URL}/api/v1/system/version" "Direct API Version"
fi

echo "=== TODOS OS SMOKE TESTS PASSARAM COM SUCESSO! ==="
