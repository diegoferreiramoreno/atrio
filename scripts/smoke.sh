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

# Valida dependências necessárias do script: curl e um interpretador com parser JSON real (node, python3 ou jq)
if ! command -v curl >/dev/null 2>&1; then
    echo "Erro de dependência: 'curl' é obrigatório para execução do smoke test." >&2
    exit 1
fi

JSON_PARSER=""
if command -v node >/dev/null 2>&1; then
    JSON_PARSER="node"
elif command -v python3 >/dev/null 2>&1; then
    JSON_PARSER="python3"
elif command -v jq >/dev/null 2>&1; then
    JSON_PARSER="jq"
else
    echo "Erro de dependência: É necessário ter 'node', 'python3' ou 'jq' instalado para parsing seguro de JSON no smoke test." >&2
    exit 1
fi

parse_and_validate_version_json() {
    local raw="$1"

    if [ "$JSON_PARSER" = "node" ]; then
        node -e '
            const raw = process.argv[1];
            try {
                const data = JSON.parse(raw);
                if (data === null || typeof data !== "object" || Array.isArray(data)) process.exit(1);
                if (typeof data.version !== "string" || data.version.trim() === "") process.exit(1);
                if (typeof data.commit !== "string" || data.commit.trim() === "") process.exit(1);
                console.log(`Version: ${data.version.trim()}, Commit: ${data.commit.trim()}`);
                process.exit(0);
            } catch {
                process.exit(1);
            }
        ' "$raw" 2>/dev/null
    elif [ "$JSON_PARSER" = "python3" ]; then
        python3 -c '
import json, sys
raw = sys.argv[1]
try:
    data = json.loads(raw)
    if not isinstance(data, dict):
        sys.exit(1)
    v = data.get("version")
    c = data.get("commit")
    if not isinstance(v, str) or not v.strip():
        sys.exit(1)
    if not isinstance(c, str) or not c.strip():
        sys.exit(1)
    print(f"Version: {v.strip()}, Commit: {c.strip()}")
    sys.exit(0)
except Exception:
    sys.exit(1)
' "$raw" 2>/dev/null
    elif [ "$JSON_PARSER" = "jq" ]; then
        echo "$raw" | jq -e '
            if type == "object" and
               (.version | type == "string" and (test("^\\s*$") | not)) and
               (.commit | type == "string" and (test("^\\s*$") | not))
            then
               "Version: \(.version), Commit: \(.commit)"
            else
               empty | halt_error(1)
            end
        ' -r 2>/dev/null
    fi
}

test_version_json() {
    local url="$1"
    local desc="$2"

    echo -n "Testando: ${desc} (${url})..."
    local attempt=1

    while [ "$attempt" -le "$MAX_RETRIES" ]; do
        local response
        response=$(curl --fail --show-error --silent --max-time 15 "$url" 2>/dev/null || true)

        if [ -n "$response" ]; then
            local parsed_info
            if parsed_info=$(parse_and_validate_version_json "$response"); then
                echo " [OK - ${parsed_info}]"
                return 0
            fi
        fi

        echo -n "."
        sleep "$RETRY_DELAY"
        attempt=$((attempt + 1))
    done

    echo " [FALHA]"
    echo "Erro: Endpoint ${url} não retornou objeto JSON válido com propriedades 'version' e 'commit' contendo strings não vazias após ${MAX_RETRIES} tentativas."
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
