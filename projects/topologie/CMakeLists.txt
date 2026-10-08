cmake_minimum_required(VERSION 3.20)
project(topologie VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(TOPO_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(TOPO_BUILD_TESTS "Build the GoogleTest suite" ON)
option(TOPO_C_API "Build the C API (tools/c_api.cpp) as a shared library, for the Python API" OFF)

if(TOPO_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined,float-cast-overflow -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined,float-cast-overflow)
endif()

# The mesh reader comes from lib-c, the C library of the same portfolio.
set(MESH_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../lib-c lib-c EXCLUDE_FROM_ALL)

function(topo_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE /W4 /WX /permissive-)
  else()
    target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror)
  endif()
endfunction()

add_library(topo src/mesh.cpp src/invariants.cpp src/curvature.cpp src/morse.cpp src/persistence.cpp src/reeb.cpp src/extended.cpp)
target_include_directories(topo PUBLIC include)
target_link_libraries(topo PRIVATE mesh)
topo_warnings(topo)

# Shared library with the C entry points (libtopoc.so / .dylib / topoc.dll), lib-c linked in statically.
if(TOPO_C_API)
  set_target_properties(mesh topo PROPERTIES POSITION_INDEPENDENT_CODE ON)
  add_library(topoc SHARED tools/c_api.cpp)
  target_link_libraries(topoc PRIVATE topo mesh)
  topo_warnings(topoc)
  set_target_properties(topoc PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

# Computing times for the project page (sprint 40), read by scripts/bench.mjs; measure in Release.
add_executable(topo_bench tools/topo_bench.cpp)
target_link_libraries(topo_bench PRIVATE topo)
topo_warnings(topo_bench)

if(TOPO_BUILD_TESTS)
  enable_testing()
  include(FetchContent)
  set(gtest_force_shared_crt ON CACHE BOOL "" FORCE) # MSVC: same runtime as the tests
  set(INSTALL_GTEST OFF CACHE BOOL "" FORCE)
  FetchContent_Declare(googletest
    GIT_REPOSITORY https://github.com/google/googletest.git
    GIT_TAG v1.18.0
    GIT_SHALLOW TRUE)
  FetchContent_MakeAvailable(googletest)
  include(GoogleTest)

  add_executable(topo_tests tests/test_halfedge.cpp tests/test_invariants.cpp tests/test_curvature.cpp tests/test_samples.cpp tests/test_morse.cpp tests/test_persistence.cpp tests/test_reeb.cpp tests/test_bench.cpp tests/test_extended.cpp)
  target_link_libraries(topo_tests PRIVATE topo mesh GTest::gtest_main)
  target_compile_definitions(topo_tests PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/../lib-c/tests/data"
                                                TOPO_SAMPLES="${CMAKE_CURRENT_SOURCE_DIR}/samples")
  # Listing the tests is quick, but a fresh Debug executable can take longer than the default 5 s to start on
  # the Windows runner (seen in CI, sprint 38).
  gtest_discover_tests(topo_tests DISCOVERY_TIMEOUT 60)
endif()
