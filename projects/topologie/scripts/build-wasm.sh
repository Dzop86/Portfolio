#!/bin/sh
# Compiles the topology library (and lib-c's reader) to WebAssembly for the portfolio viewer, with a
# pinned Emscripten in Docker. Usage: scripts/build-wasm.sh [OUT_DIR]   (default: src/assets/wasm)
# The output is committed; CI rebuilds it and fails if it differs (see D15 and D16).
set -eu
projects=$(cd "$(dirname "$0")/../.." && pwd)
out=${1:-$projects/../src/assets/wasm}
mkdir -p "$out"
out=$(cd "$out" && pwd)
EMSDK_IMAGE=emscripten/emsdk:6.0.11

exports=_malloc,_free,_topoc_read,_topoc_error,_topoc_error_line,_topoc_summary,_topoc_vertex_count,\
_topoc_index_count,_topoc_positions,_topoc_indices,_topoc_curvature,_topoc_defect,_topoc_boundary,\
_topoc_elevation,_topoc_height,_topoc_order,_topoc_sublevel_euler,_topoc_critical,\
_topoc_persistence,_topoc_pairs,_topoc_persistence_limit,_topoc_reeb,_topoc_reeb_graph,_topoc_reeb_limit

docker run --rm -u "$(id -u):$(id -g)" -e EM_CACHE=/tmp/em-cache \
  -v "$projects:/p:ro" -v "$out:/out" -w /tmp "$EMSDK_IMAGE" sh -c "
    emcc -O2 -std=c11 -I/p/lib-c/include -c /p/lib-c/src/mesh.c /p/lib-c/src/obj.c /p/lib-c/src/ply.c /p/lib-c/src/stl.c /p/lib-c/src/topology.c &&
    em++ -O2 -std=c++20 -fwasm-exceptions -Wall -Wextra -Werror -I/p/topologie/include -I/p/lib-c/include \
      /p/topologie/src/mesh.cpp /p/topologie/src/invariants.cpp /p/topologie/src/curvature.cpp /p/topologie/src/morse.cpp /p/topologie/src/persistence.cpp /p/topologie/src/reeb.cpp \
      /p/topologie/tools/c_api.cpp mesh.o obj.o ply.o stl.o topology.o \
      -o /out/topo.js -fwasm-exceptions \
      -sMODULARIZE=1 -sEXPORT_ES6=1 -sEXPORT_NAME=createTopo -sENVIRONMENT=web,node \
      -sFILESYSTEM=0 -sALLOW_MEMORY_GROWTH=1 -sMAXIMUM_MEMORY=1GB \
      -sEXPORTED_FUNCTIONS=$exports -sEXPORTED_RUNTIME_METHODS=UTF8ToString,HEAPU8,HEAPF32,HEAPU32,HEAP32"
echo "WebAssembly build written to $out"
