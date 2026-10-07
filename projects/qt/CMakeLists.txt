cmake_minimum_required(VERSION 3.21)
project(qtviewer VERSION 0.1.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(VIEWER_BUILD_TESTS "Build the Qt Test suite" ON)

find_package(Qt6 6.5 REQUIRED COMPONENTS Widgets OpenGL OpenGLWidgets LinguistTools)
qt_standard_project_setup()

# The mesh comes from the portfolio's libraries: lib-c reads the file, topologie builds the half-edge
# structure and computes the invariants and the curvature.
set(TOPO_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../topologie topologie EXCLUDE_FROM_ALL)

function(viewer_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE /W4 /WX /permissive- /utf-8)
  else()
    target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror)
  endif()
endfunction()

# Everything but main(): the tests link the same code as the application.
qt_add_library(viewer STATIC
  src/meshmodel.h src/meshmodel.cpp
  src/camera.h src/camera.cpp
  src/picking.h src/picking.cpp
  src/renderer.h src/renderer.cpp
  src/viewerwidget.h src/viewerwidget.cpp
  src/legendwidget.h src/legendwidget.cpp
  src/mainwindow.h src/mainwindow.cpp)
target_include_directories(viewer PUBLIC src)
target_link_libraries(viewer PUBLIC Qt6::Widgets Qt6::OpenGL Qt6::OpenGLWidgets topo)
viewer_warnings(viewer)

# The topology viewer's sample surfaces, built into the application.
qt_add_resources(viewer samples
  PREFIX /samples
  BASE ../topologie/samples
  FILES ../topologie/samples/torus.obj ../topologie/samples/sphere.obj
        ../topologie/samples/mobius.obj ../topologie/samples/saddle.obj)

# English source strings, French translation (i18n/qtviewer_fr.ts), built into the binary.
qt_add_translations(viewer TS_FILES i18n/qtviewer_fr.ts RESOURCE_PREFIX /i18n LUPDATE_OPTIONS -no-obsolete -locations none)

qt_add_executable(qtviewer WIN32 MACOSX_BUNDLE src/main.cpp)
target_link_libraries(qtviewer PRIVATE viewer)
viewer_warnings(qtviewer)
set_target_properties(qtviewer PROPERTIES
  MACOSX_BUNDLE_GUI_IDENTIFIER io.github.dzop86.qtviewer
  MACOSX_BUNDLE_BUNDLE_NAME "Mesh viewer")

if(VIEWER_BUILD_TESTS)
  enable_testing()
  find_package(Qt6 REQUIRED COMPONENTS Test)
  foreach(name meshmodel camera picking render window translations)
    qt_add_executable(test_${name} tests/test_${name}.cpp)
    target_link_libraries(test_${name} PRIVATE viewer Qt6::Test)
    target_compile_definitions(test_${name} PRIVATE
      MESH_TEST_DATA="${CMAKE_CURRENT_SOURCE_DIR}/../lib-c/tests/data"
      VIEWER_SOURCE_DIR="${CMAKE_CURRENT_SOURCE_DIR}")
    viewer_warnings(test_${name})
    add_test(NAME ${name} COMMAND test_${name})
  endforeach()
endif()
