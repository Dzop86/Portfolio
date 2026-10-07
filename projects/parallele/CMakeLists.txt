cmake_minimum_required(VERSION 3.21)
project(parallele VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(PAR_BUILD_TESTS "Build the GoogleTest suite" ON)
option(PAR_OPENCL "Build the OpenCL version when OpenCL is found" ON)
option(PAR_CUDA "Build the CUDA version when a CUDA compiler is found" ON)
option(PAR_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)

if(PAR_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined)
endif()

# lib-c reads the files; topologie, built alongside, is the reference of the tests.
set(TOPO_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../topologie topologie EXCLUDE_FROM_ALL)

# Warnings are errors for the C++ sources; nvcc gets its own flags (see the CUDA block).
function(par_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE $<$<COMPILE_LANGUAGE:CXX>:/W4 /WX /permissive->)
  else()
    target_compile_options(${target} PRIVATE $<$<COMPILE_LANGUAGE:CXX>:-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror>)
  endif()
endfunction()

add_library(par src/mesh.cpp src/io.cpp src/curvature.cpp)
target_include_directories(par PUBLIC include PRIVATE src)
target_link_libraries(par PRIVATE mesh)
par_warnings(par)

find_package(OpenMP)
if(OpenMP_CXX_FOUND)
  target_link_libraries(par PUBLIC OpenMP::OpenMP_CXX)
else()
  message(WARNING "OpenMP not found: the OpenMP version runs on one thread.")
endif()

# Apple deprecated OpenCL in 2018, and its OpenCL has no double precision on Apple Silicon.
if(APPLE AND PAR_OPENCL)
  message(STATUS "macOS: built without the OpenCL version.")
  set(PAR_OPENCL OFF)
endif()
if(PAR_OPENCL)
  find_package(OpenCL)
  if(OpenCL_FOUND)
    target_sources(par PRIVATE src/opencl.cpp)
    target_link_libraries(par PRIVATE OpenCL::OpenCL)
    target_compile_definitions(par PUBLIC PAR_HAVE_OPENCL)
  else()
    message(STATUS "OpenCL not found: built without the OpenCL version.")
    target_sources(par PRIVATE src/opencl_stub.cpp)
  endif()
else()
  target_sources(par PRIVATE src/opencl_stub.cpp)
endif()

if(PAR_CUDA)
  include(CheckLanguage)
  check_language(CUDA)
endif()
if(PAR_CUDA AND CMAKE_CUDA_COMPILER)
  # Turing (GeForce 16 and 20 series) and later, plus PTX for the cards to come.
  if(NOT DEFINED CMAKE_CUDA_ARCHITECTURES)
    set(CMAKE_CUDA_ARCHITECTURES 75-real 86-real 89)
  endif()
  enable_language(CUDA)
  set(CMAKE_CUDA_STANDARD 20)
  target_sources(par PRIVATE src/cuda.cu)
  # par was created before CMAKE_CUDA_ARCHITECTURES was set: without this its property stays empty.
  set_target_properties(par PROPERTIES CUDA_ARCHITECTURES "${CMAKE_CUDA_ARCHITECTURES}")
  # --fmad=false: no fused multiply-add, the same roundings as the C++ code; warnings are errors.
  target_compile_options(par PRIVATE $<$<COMPILE_LANGUAGE:CUDA>:--fmad=false -Werror=all-warnings>)
  target_compile_definitions(par PUBLIC PAR_HAVE_CUDA)
else()
  target_sources(par PRIVATE src/cuda_stub.cpp)
endif()

add_executable(parcurv tools/parcurv.cpp)
target_link_libraries(parcurv PRIVATE par)
par_warnings(parcurv)

add_executable(parbench tools/parbench.cpp)
target_link_libraries(parbench PRIVATE par)
par_warnings(parbench)

if(PAR_BUILD_TESTS)
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

  add_executable(par_tests tests/test_adjacency.cpp tests/test_curvature.cpp tests/test_opencl.cpp tests/test_cuda.cpp)
  target_link_libraries(par_tests PRIVATE par topo GTest::gtest_main)
  target_compile_definitions(par_tests PRIVATE MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/../lib-c/tests/data"
                                                TOPO_SAMPLES="${CMAKE_CURRENT_SOURCE_DIR}/../topologie/samples")
  par_warnings(par_tests)
  if(MSVC)
    # std::getenv (PAR_REQUIRE_OPENCL) is standard C++; MSVC calls it unsafe (C4996).
    target_compile_definitions(par_tests PRIVATE _CRT_SECURE_NO_WARNINGS)
  endif()
  gtest_discover_tests(par_tests)
endif()
