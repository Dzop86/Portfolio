#!/bin/sh
# Runs the unit tests and the CLI under Valgrind; any leak or invalid access fails with exit code 99.
# Usage: scripts/valgrind.sh BUILD_DIR   (a Debug build without sanitizers)
set -u
build=${1:-build}
data=$(dirname "$0")/../tests/data
status=0

check() {
  expected=$1
  shift
  valgrind -q --leak-check=full --show-leak-kinds=all --errors-for-leak-kinds=all --error-exitcode=99 "$@" >/dev/null
  code=$?
  if [ "$code" -ne "$expected" ]; then
    echo "FAIL ($code, expected $expected): $*"
    status=1
  else
    echo "ok: $*"
  fi
}

check 0 "$build/test_obj"
check 0 "$build/test_ply"
check 0 "$build/meshinfo" "$data/tetrahedron.ply"
check 0 "$build/meshinfo" "$data/cube.obj"
check 1 "$build/meshinfo" "$data/missing.obj"
check 2 "$build/meshinfo"
exit $status
