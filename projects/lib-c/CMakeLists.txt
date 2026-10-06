cmake_minimum_required(VERSION 3.20)
project(meshlib VERSION 0.1.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

option(MESH_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(MESH_BUILD_TESTS "Build the Unity test suite" ON)
option(MESH_FUZZ "Build the libFuzzer target (Clang only)" OFF)

# Strict warnings, treated as errors, on every compiler of the CI matrix.
function(mesh_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE /W4 /WX)
    target_compile_definitions(${target} PRIVATE _CRT_SECURE_NO_WARNINGS)
  else()
    target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror)
  endif()
endfunction()

if(MESH_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined,float-cast-overflow -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined,float-cast-overflow)
endif()

# Fuzzing instruments every target, so it is set before they are declared.
if(MESH_FUZZ)
  add_compile_options(-fsanitize=fuzzer-no-link,address,undefined -fno-omit-frame-pointer)
  add_link_options(-fsanitize=address,undefined)
endif()

# BUILD_SHARED_LIBS=ON builds libmesh.so / .dylib / mesh.dll for the Python API (projects/fastapi).
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
add_library(mesh src/mesh.c src/obj.c src/ply.c src/stl.c src/topology.c)
target_include_directories(mesh PUBLIC include)
mesh_warnings(mesh)
if(NOT MSVC)
  target_link_libraries(mesh PRIVATE m)
endif()

add_executable(meshinfo tools/meshinfo.c)
target_link_libraries(meshinfo PRIVATE mesh)
mesh_warnings(meshinfo)

if(MESH_FUZZ)
  add_executable(fuzz_read tests/fuzz/fuzz_read.c)
  target_link_libraries(fuzz_read PRIVATE mesh)
  target_link_options(fuzz_read PRIVATE -fsanitize=fuzzer)
endif()

if(MESH_BUILD_TESTS)
  enable_testing()
  include(FetchContent)
  FetchContent_Declare(unity
    GIT_REPOSITORY https://github.com/ThrowTheSwitch/Unity.git
    GIT_TAG v2.6.1
    GIT_SHALLOW TRUE)
  FetchContent_MakeAvailable(unity)
  target_compile_definitions(unity PUBLIC UNITY_INCLUDE_DOUBLE)

  add_executable(test_obj tests/test_obj.c)
  target_link_libraries(test_obj PRIVATE mesh unity)
  target_compile_definitions(test_obj PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/tests/data")
  mesh_warnings(test_obj)
  add_test(NAME unit.obj COMMAND test_obj)

  add_executable(test_topology tests/test_topology.c)
  target_link_libraries(test_topology PRIVATE mesh unity)
  target_compile_definitions(test_topology PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/tests/data")
  mesh_warnings(test_topology)
  add_test(NAME unit.topology COMMAND test_topology)

  add_executable(test_stl tests/test_stl.c)
  target_link_libraries(test_stl PRIVATE mesh unity)
  target_compile_definitions(test_stl PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/tests/data")
  mesh_warnings(test_stl)
  add_test(NAME unit.stl COMMAND test_stl)

  add_executable(test_ply tests/test_ply.c)
  target_link_libraries(test_ply PRIVATE mesh unity)
  target_compile_definitions(test_ply PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/tests/data")
  mesh_warnings(test_ply)
  add_test(NAME unit.ply COMMAND test_ply)

  # Integration: the CLI on a real file, then its error paths.
  add_test(NAME cli.cube COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/cube.obj)
  set_tests_properties(cli.cube PROPERTIES
    PASS_REGULAR_EXPRESSION "vertices: 8[\r\n]+polygons: 6[\r\n]+triangles: 12[\r\n]+edges: 18[\r\n]+boundary edges: 0[\r\n]+euler characteristic: 2")
  add_test(NAME cli.ply COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/tetrahedron.ply)
  set_tests_properties(cli.ply PROPERTIES
    PASS_REGULAR_EXPRESSION "vertices: 4[\r\n]+polygons: 4[\r\n]+triangles: 4")
  add_test(NAME cli.torus COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/torus.obj)
  set_tests_properties(cli.torus PROPERTIES
    PASS_REGULAR_EXPRESSION "triangles: 96[\r\n]+edges: 144[\r\n]+boundary edges: 0[\r\n]+euler characteristic: 0")
  add_test(NAME cli.stl COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/cube.stl)
  set_tests_properties(cli.stl PROPERTIES
    PASS_REGULAR_EXPRESSION "vertices: 8[\r\n]+polygons: 12[\r\n]+triangles: 12[\r\n]+edges: 18")
  add_test(NAME cli.missing COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/missing.obj)
  set_tests_properties(cli.missing PROPERTIES WILL_FAIL TRUE)
  add_test(NAME cli.usage COMMAND meshinfo)
  set_tests_properties(cli.usage PROPERTIES WILL_FAIL TRUE)
endif()
