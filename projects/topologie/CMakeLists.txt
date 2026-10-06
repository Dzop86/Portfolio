cmake_minimum_required(VERSION 3.20)
project(topologie VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(TOPO_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(TOPO_BUILD_TESTS "Build the GoogleTest suite" ON)

if(TOPO_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined,float-cast-overflow -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined,float-cast-overflow)
endif()

# The mesh reader comes from lib-c, the C library of the same portfolio.
set(MESH_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../lib-c lib-c EXCLUDE_FROM_ALL)

add_library(topo src/mesh.cpp)
target_include_directories(topo PUBLIC include)
target_link_libraries(topo PRIVATE mesh)
if(MSVC)
  target_compile_options(topo PRIVATE /W4 /WX /permissive-)
else()
  target_compile_options(topo PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror)
endif()

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

  add_executable(topo_tests tests/test_halfedge.cpp)
  target_link_libraries(topo_tests PRIVATE topo mesh GTest::gtest_main)
  target_compile_definitions(topo_tests PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/../lib-c/tests/data")
  gtest_discover_tests(topo_tests)
endif()
