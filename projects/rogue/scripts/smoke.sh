#!/usr/bin/env bash
# End-to-end check of a running score API: sign up, sign in, draw a seed, let the terminal client's
# autopilot play it, submit the run, find it on the leaderboard. Run from projects/rogue.
#   scripts/smoke.sh http://localhost:8001
# ROGUE_CLI is the command that starts the terminal client (default: dotnet run, Release build).
set -euo pipefail
base=${1:?usage: scripts/smoke.sh BASE_URL}
cli=${ROGUE_CLI:-dotnet run --project src/Rogue.Cli -c Release --}
run_file=smoke-run.json
big_file=smoke-big.json
trap 'rm -f "$run_file" "$big_file"' EXIT

fail() { echo "FAIL: $*" >&2; exit 1; }
json() { curl -fsS -H 'Content-Type: application/json' "$@"; }

curl -fsS "$base/health" | grep -q '"ok"' || fail "health"
curl -fsS "$base/openapi/v1.json" | jq -e '.paths["/api/runs/{id}/submission"]' > /dev/null || fail "OpenAPI document"
curl -fsS -o /dev/null "$base/swagger/index.html" || fail "Swagger UI"

name="smoke_$RANDOM$RANDOM"
credentials=$(jq -nc --arg n "$name" '{name: $n, password: "smoke test password"}')
json -o /dev/null -d "$credentials" "$base/api/accounts" || fail "sign-up"
token=$(json -d "$credentials" "$base/api/tokens" | jq -r .token)
auth=(-H "Authorization: Bearer $token")

ticket=$(json -X POST "${auth[@]}" "$base/api/runs")
id=$(jq -r .id <<< "$ticket")
seed=$(jq -r .seed <<< "$ticket")
echo "Run $id, seed $seed"

$cli --bot --seed "$seed" --lang en --save "$run_file" | tail -1
result=$(json "${auth[@]}" -d @"$run_file" "$base/api/runs/$id/submission") || fail "submission"
echo "Server: $result"
score=$(jq -r .score <<< "$result")
expected=$($cli --replay "$run_file" --lang en | sed -E 's/.*score ([0-9]+)\.$/\1/')
[[ "$score" == "$expected" ]] || fail "the server scored $score, the client $expected"

curl -fsS "$base/api/scores?limit=100" | jq -e --arg n "$name" --argjson s "$score" 'any(.[]; .player == $n and .score == $s)' > /dev/null \
  || fail "leaderboard"

# Kestrel refuses bodies over 256 kB before the API reads them.
jq -nc --arg s "$seed" '{format: "rogue-run", version: 1, seed: $s, actions: ("." * 300000)}' > "$big_file"
other=$(json -X POST "${auth[@]}" "$base/api/runs" | jq -r .id)
status=$(curl -sS -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' "${auth[@]}" --data-binary @"$big_file" "$base/api/runs/$other/submission")
[[ "$status" == 413 ]] || fail "oversized body answered $status, not 413"

echo "OK: $name scored $score"
