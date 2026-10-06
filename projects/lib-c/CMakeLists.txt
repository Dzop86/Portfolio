cmake_minimum_required(VERSION 3.20)
project(meshlib VERSION 0.1.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

option(MESH_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(MESH_BUILD_TESTS "Build the Unity test suite" ON)

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
  add_compile_options(-fsanitize=address,undefined -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined)
endif()

add_library(mesh src/mesh.c src/obj.c)
target_include_directories(mesh PUBLIC include)
mesh_warnings(mesh)
if(NOT MSVC)
  target_link_libraries(mesh PRIVATE m)
endif()

add_executable(meshinfo tools/meshinfo.c)
target_link_libraries(meshinfo PRIVATE mesh)
mesh_warnings(meshinfo)

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

  # Integration: the CLI on a real file, then its error paths.
  add_test(NAME cli.cube COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/cube.obj)
  set_tests_properties(cli.cube PROPERTIES
    PASS_REGULAR_EXPRESSION "vertices: 8[\r\n]+polygons: 6[\r\n]+triangles: 12")
  add_test(NAME cli.missing COMMAND meshinfo ${CMAKE_CURRENT_SOURCE_DIR}/tests/data/missing.obj)
  set_tests_properties(cli.missing PROPERTIES WILL_FAIL TRUE)
  add_test(NAME cli.usage COMMAND meshinfo)
  set_tests_properties(cli.usage PROPERTIES WILL_FAIL TRUE)
endif()
