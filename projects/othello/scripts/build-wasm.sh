#!/bin/sh
# Compiles the Othello engine and AI to WebAssembly for the portfolio's demo, with a pinned Emscripten
# in Docker. Usage: scripts/build-wasm.sh [OUT_DIR]   (default: the site assets, src/assets/wasm)
# The output is committed; CI rebuilds it and fails if it differs (as for lib-c, D15).
set -eu
here=$(cd "$(dirname "$0")/.." && pwd)
out=${1:-$here/../../src/assets/wasm}
mkdir -p "$out"
out=$(cd "$out" && pwd)
EMSDK_IMAGE=emscripten/emsdk:6.0.11

exports=_othjs_reset,_othjs_cell,_othjs_is_legal,_othjs_to_move,_othjs_must_pass,_othjs_game_over,\
_othjs_count,_othjs_plies,_othjs_play,_othjs_pass,_othjs_undo,_othjs_ai_move,_othjs_perft

docker run --rm -u "$(id -u):$(id -g)" -e EM_CACHE=/tmp/em-cache \
  -v "$here:/src:ro" -v "$out:/out" -w /src "$EMSDK_IMAGE" \
  emcc -O2 -std=c11 -Wall -Wextra -Werror -Iinclude \
    src/board.c src/ai.c tools/wasm_api.c \
    -o /out/othello.js \
    -sMODULARIZE=1 -sEXPORT_ES6=1 -sEXPORT_NAME=createOthello -sENVIRONMENT=web,node \
    -sFILESYSTEM=0 -sEXPORTED_FUNCTIONS="$exports"
echo "WebAssembly build written to $out"
