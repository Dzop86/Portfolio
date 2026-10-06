#!/bin/sh
# Compiles lib-c to WebAssembly for the portfolio demo, with a pinned Emscripten in Docker.
# Usage: scripts/build-wasm.sh [OUT_DIR]   (default: the site assets, src/assets/wasm)
# The output is committed; CI rebuilds it and fails if it differs (see D15).
set -eu
here=$(cd "$(dirname "$0")/.." && pwd)
out=${1:-$here/../../src/assets/wasm}
mkdir -p "$out"
out=$(cd "$out" && pwd)
EMSDK_IMAGE=emscripten/emsdk:6.0.11

exports=_malloc,_free,_meshjs_read,_meshjs_status_string,_meshjs_error_line,_meshjs_vertex_count,\
_meshjs_polygon_count,_meshjs_triangle_count,_meshjs_edge_count,_meshjs_boundary_edge_count,\
_meshjs_euler_characteristic,_meshjs_bbox

docker run --rm -u "$(id -u):$(id -g)" -e EM_CACHE=/tmp/em-cache \
  -v "$here:/src:ro" -v "$out:/out" -w /src "$EMSDK_IMAGE" \
  emcc -O2 -std=c11 -Wall -Wextra -Werror -Iinclude \
    src/mesh.c src/obj.c src/ply.c src/topology.c tools/wasm_api.c \
    -o /out/meshlib.js \
    -sMODULARIZE=1 -sEXPORT_ES6=1 -sEXPORT_NAME=createMeshLib -sENVIRONMENT=web,node \
    -sFILESYSTEM=0 -sALLOW_MEMORY_GROWTH=1 -sMAXIMUM_MEMORY=512MB \
    -sEXPORTED_FUNCTIONS="$exports" -sEXPORTED_RUNTIME_METHODS=UTF8ToString,HEAPU8
echo "WebAssembly build written to $out"
