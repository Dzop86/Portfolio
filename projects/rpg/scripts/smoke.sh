#!/usr/bin/env bash
# End-to-end check of a running accounts and characters API: sign up, sign in, create, list and
# delete characters, the server's refusals. Run from projects/rpg.
#   scripts/smoke.sh http://localhost:8002
set -euo pipefail
base=${1:?usage: scripts/smoke.sh BASE_URL}

fail() { echo "FAIL: $*" >&2; exit 1; }
json() { curl -fsS -H 'Content-Type: application/json' "$@"; }
status() { curl -sS -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' "$@"; }

curl -fsS "$base/health" | grep -q '"ok"' || fail "health"
curl -fsS "$base/openapi/v1.json" | jq -e '.paths["/api/characters/{id}"]' > /dev/null || fail "OpenAPI document"
curl -fsS -o /dev/null "$base/swagger/index.html" || fail "Swagger UI"

name="smoke_$RANDOM$RANDOM"
credentials=$(jq -nc --arg n "$name" '{name: $n, password: "smoke test password"}')
json -o /dev/null -d "$credentials" "$base/api/accounts" || fail "sign-up"
[[ $(status -d "$credentials" "$base/api/accounts") == 409 ]] || fail "the same name twice"
token=$(json -d "$credentials" "$base/api/tokens" | jq -r .token)
auth=(-H "Authorization: Bearer $token")

[[ $(status "$base/api/characters") == 401 ]] || fail "characters without a token"
# Character names are letters only, unique on the server.
hero=Smoke
for _ in 1 2 3 4 5 6 7 8 9 10; do hero+=$(printf "\\x$(printf %x $((97 + RANDOM % 26)))"); done
id=$(json "${auth[@]}" -d "$(jq -nc --arg n "$hero" '{name: $n, look: "female-c", class: "mage", colour: 3}')" "$base/api/characters" | jq -r .id)
json "${auth[@]}" "$base/api/characters" | jq -e --arg n "$hero" 'length == 1 and .[0].name == $n and .[0].look == "female-c" and .[0].class == "mage" and .[0].colour == 3' > /dev/null \
  || fail "character list"
[[ $(status "${auth[@]}" -d '{"name": "R2D2", "look": "female-c", "class": "mage"}' "$base/api/characters") == 400 ]] || fail "invalid name"
[[ $(status "${auth[@]}" -d '{"name": "Valide", "look": "female-c", "class": "dragon"}' "$base/api/characters") == 400 ]] || fail "unknown class"
[[ $(status -X DELETE "${auth[@]}" "$base/api/characters/$id") == 204 ]] || fail "delete"
json "${auth[@]}" "$base/api/characters" | jq -e 'length == 0' > /dev/null || fail "list after delete"

# Kestrel refuses bodies over 64 kB before the API reads them.
jq -nc '{name: ("x" * 100000), look: "female-c"}' | \
  { [[ $(status "${auth[@]}" --data-binary @- "$base/api/characters") == 413 ]] || fail "oversized body"; }

echo "OK: $name created and deleted $hero"
