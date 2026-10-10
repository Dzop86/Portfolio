#!/bin/sh
# End to end against a running API (docker compose): health, a search, and /ask without a key.
#   projects/rag/scripts/smoke.sh http://localhost:8003
set -eu
base=${1:-http://localhost:8003}
curl -fsS "$base/health" | grep -q '"status":"ok"'
curl -fsS -G "$base/search" --data-urlencode "q=Quelle version minimale de Node faut-il ?" --data-urlencode "k=3" | grep -q 'D8. Node 22 minimum'
test "$(curl -s -o /dev/null -w '%{http_code}' "$base/search?q=x")" = 422
if [ -z "${ANTHROPIC_API_KEY:-}" ]; then
  test "$(curl -s -o /dev/null -w '%{http_code}' -H 'content-type: application/json' -d '{"question":"Which Node version?"}' "$base/ask")" = 503
fi
echo "smoke OK"
