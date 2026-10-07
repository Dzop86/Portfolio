#!/bin/sh
# Compiles the ray tracer (and lib-c's reader) to WebAssembly for the project page, with a pinned
# Emscripten in Docker. Usage: scripts/build-wasm.sh [OUT_DIR]   (default: src/assets/wasm)
# The output is committed; CI rebuilds it and fails if it differs (as for topologie, D15 and D16).
set -eu
projects=$(cd "$(dirname "$0")/../.." && pwd)
out=${1:-$projects/../src/assets/wasm}
mkdir -p "$out"
out=$(cd "$out" && pwd)
EMSDK_IMAGE=emscripten/emsdk:6.0.11

exports=_malloc,_free,_rtc_load_mesh,_rtc_error,_rtc_error_line,_rtc_set_scene,_rtc_default_yaw,\
_rtc_default_pitch,_rtc_default_distance,_rtc_set_view,_rtc_resize,_rtc_render,_rtc_pixels,_rtc_samples,\
_rtc_width,_rtc_height,_rtc_triangles,_rtc_set_settings,_rtc_default_setting

docker run --rm -u "$(id -u):$(id -g)" -e EM_CACHE=/tmp/em-cache \
  -v "$projects:/p:ro" -v "$out:/out" -w /tmp "$EMSDK_IMAGE" sh -c "
    emcc -O3 -std=c11 -I/p/lib-c/include -c /p/lib-c/src/mesh.c /p/lib-c/src/obj.c /p/lib-c/src/ply.c /p/lib-c/src/stl.c /p/lib-c/src/topology.c &&
    em++ -O3 -std=c++20 -fwasm-exceptions -ffp-contract=off -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror \
      -I/p/raytracer/include -I/p/lib-c/include \
      /p/raytracer/src/mesh.cpp /p/raytracer/src/bvh.cpp /p/raytracer/src/scene.cpp /p/raytracer/src/render.cpp \
      /p/raytracer/src/scenes.cpp /p/raytracer/tools/c_api.cpp mesh.o obj.o ply.o stl.o topology.o \
      -o /out/raytracer.js -fwasm-exceptions \
      -sMODULARIZE=1 -sEXPORT_ES6=1 -sEXPORT_NAME=createRaytracer -sENVIRONMENT=web,worker,node \
      -sFILESYSTEM=0 -sALLOW_MEMORY_GROWTH=1 -sMAXIMUM_MEMORY=1GB \
      -sEXPORTED_FUNCTIONS=$exports -sEXPORTED_RUNTIME_METHODS=UTF8ToString,HEAPU8"
echo "WebAssembly build written to $out"
