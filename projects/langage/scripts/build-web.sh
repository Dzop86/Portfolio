#!/bin/sh
# Builds Maille for the portfolio's demo, in pinned Docker images:
#   - the C parser: Flex and Bison generate it (docker/Dockerfile), Emscripten 6.0.11 compiles it to
#     WebAssembly -> maillec.js, maillec.wasm;
#   - the OCaml interpreter: js_of_ocaml -> maille-interp.js.
# Usage: scripts/build-web.sh [OUT_DIR]   (default: the site assets, src/assets/wasm)
# The outputs are committed; CI rebuilds them and fails if they differ (as for lib-c, D15).
set -eu
here=$(cd "$(dirname "$0")/.." && pwd)
out=${1:-$here/../../src/assets/wasm}
mkdir -p "$out"
out=$(cd "$out" && pwd)
gen=$(mktemp -d)
trap 'rm -rf "$gen"' EXIT
BUILD_IMAGE=maille-build
EMSDK_IMAGE=emscripten/emsdk:6.0.11

docker build -q -t "$BUILD_IMAGE" "$here/docker" > /dev/null

# Generated C (gnu11 below, like CMake's default: open_memstream and strdup are POSIX), with paths relative to /src so the #line directives do not depend on the machine.
docker run --rm -u "$(id -u):$(id -g)" -v "$here:/src:ro" -v "$gen:/gen" -w /src "$BUILD_IMAGE" sh -c '
  bison -Wall -Werror -o /gen/parser.c --header=/gen/parser.h parser/src/parser.y &&
  flex --header-file=/gen/lexer.h -o /gen/lexer.c parser/src/lexer.l &&
  mkdir -p /gen/ocaml-src &&
  cp -r /src/dune /src/dune-project /src/interp /gen/ocaml-src/ &&
  cd /gen/ocaml-src && opam exec -- dune build --root . --profile release ./interp/js/maille_js.bc.js &&
  cp _build/default/interp/js/maille_js.bc.js /gen/maille-interp.js'

exports=_malloc,_free,_maillejs_parse,_maillejs_tree,_maillejs_line,_maillejs_column,_maillejs_message
docker run --rm -u "$(id -u):$(id -g)" -e EM_CACHE=/tmp/em-cache \
  -v "$here:/src:ro" -v "$gen:/gen:ro" -v "$out:/out" -w /src "$EMSDK_IMAGE" \
  emcc -O2 -std=gnu11 -Iparser/src -I/gen \
    parser/src/ast.c parser/src/maille.c parser/tools/wasm_api.c /gen/parser.c /gen/lexer.c \
    -o /out/maillec.js \
    -sMODULARIZE=1 -sEXPORT_ES6=1 -sEXPORT_NAME=createMaillec -sENVIRONMENT=web,node \
    -sFILESYSTEM=0 -sALLOW_MEMORY_GROWTH=1 -sMAXIMUM_MEMORY=256MB \
    -sEXPORTED_FUNCTIONS="$exports" -sEXPORTED_RUNTIME_METHODS=UTF8ToString,HEAPU8
cp "$gen/maille-interp.js" "$out/maille-interp.js"
chmod 644 "$out/maille-interp.js"  # dune leaves its outputs read-only
echo "Web build written to $out"
