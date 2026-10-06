#!/bin/sh
# Smoke test of the API image: starts it, checks the user, /health and one mesh, then removes it.
# Usage: scripts/smoke.sh IMAGE   (run from the repository root or anywhere: paths are resolved here)
set -eu
image=${1:-meshapi}
data=$(cd "$(dirname "$0")/../../lib-c/tests/data" && pwd)
name=meshapi-smoke-$$
docker run -d --name "$name" -p 8000:8000 "$image" >/dev/null
trap 'docker rm -f "$name" >/dev/null' EXIT

for i in $(seq 1 30); do curl -fsS http://localhost:8000/health >/dev/null 2>&1 && break; sleep 1; done
curl -fsS http://localhost:8000/health | grep -q '"libmesh":"loaded"' || { docker logs "$name"; exit 1; }

uid=$(docker exec "$name" id -u)
[ "$uid" != 0 ] || { echo "FAIL: the API runs as root"; exit 1; }

curl -fsS --data-binary @"$data/cube.stl" http://localhost:8000/v1/mesh/stats | grep -q '"euler_characteristic":2' \
  || { echo "FAIL: /v1/mesh/stats"; exit 1; }
curl -fsS --data-binary @"$data/torus.obj" http://localhost:8000/v1/mesh/topology | grep -q '"genus":1' \
  || { echo "FAIL: /v1/mesh/topology"; exit 1; }
curl -fsS http://localhost:8000/health | grep -q '"libtopo":"loaded"' || { echo "FAIL: libtopo not loaded"; exit 1; }
code=$(printf 'v 0 0 0\nf 1 2 3\n' | curl -s -o /dev/null -w '%{http_code}' --data-binary @- http://localhost:8000/v1/mesh/stats)
[ "$code" = 422 ] || { echo "FAIL: broken mesh gave $code"; exit 1; }

# The image's own health check must pass too.
for i in $(seq 1 30); do
  state=$(docker inspect -f '{{.State.Health.Status}}' "$name")
  [ "$state" = healthy ] && break
  sleep 1
done
[ "$state" = healthy ] || { echo "FAIL: health check is $state"; exit 1; }
echo "smoke test passed (uid $uid)"
