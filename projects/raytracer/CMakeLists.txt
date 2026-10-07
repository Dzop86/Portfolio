cmake_minimum_required(VERSION 3.20)
project(raytracer VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(RT_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(RT_BUILD_TESTS "Build the GoogleTest suite" ON)

if(RT_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined,float-cast-overflow -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined,float-cast-overflow)
endif()

# The mesh reader comes from lib-c, the C library of the same portfolio.
set(MESH_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../lib-c lib-c EXCLUDE_FROM_ALL)

function(rt_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE /W4 /WX /permissive-)
  else()
    # No fused multiply-add: the same image on every compiler (Clang contracts by default).
    target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror -ffp-contract=off)
  endif()
endfunction()

add_library(rt src/mesh.cpp src/bvh.cpp src/scene.cpp src/render.cpp src/scenes.cpp src/image.cpp)
target_include_directories(rt PUBLIC include)
target_link_libraries(rt PRIVATE mesh)
rt_warnings(rt)

add_executable(rt_render tools/rt_render.cpp)
target_link_libraries(rt_render PRIVATE rt)
rt_warnings(rt_render)

if(RT_BUILD_TESTS)
  enable_testing()
  include(FetchContent)
  set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
  set(INSTALL_GTEST OFF CACHE BOOL "" FORCE)
  FetchContent_Declare(googletest
    GIT_REPOSITORY https://github.com/google/googletest.git
    GIT_TAG v1.18.0
    GIT_SHALLOW TRUE)
  FetchContent_MakeAvailable(googletest)
  include(GoogleTest)

  add_executable(rt_tests tests/test_math.cpp tests/test_geometry.cpp tests/test_mesh_bvh.cpp tests/test_render.cpp tests/test_scenes.cpp)
  target_link_libraries(rt_tests PRIVATE rt GTest::gtest_main)
  target_compile_definitions(rt_tests PRIVATE RT_SAMPLES="${CMAKE_CURRENT_SOURCE_DIR}/../topologie/samples"
                                              RT_REFERENCE="${CMAKE_CURRENT_SOURCE_DIR}/tests/reference")
  rt_warnings(rt_tests)
  # RT_UPDATE_REFERENCE is read with std::getenv, which MSVC deprecates in favour of _dupenv_s.
  if(MSVC)
    target_compile_definitions(rt_tests PRIVATE _CRT_SECURE_NO_WARNINGS)
  endif()
  gtest_discover_tests(rt_tests)
endif()
