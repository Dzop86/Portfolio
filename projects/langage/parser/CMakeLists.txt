cmake_minimum_required(VERSION 3.20)
project(maille_parser C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

option(MAILLE_SANITIZE "Build with ASan and UBSan (GCC or Clang)" OFF)
option(MAILLE_BUILD_TESTS "Build the tests" ON)

if(MAILLE_SANITIZE)
  add_compile_options(-fsanitize=address,undefined -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined)
endif()

# Bison 3.6+ for "parse.error detailed"; on Windows, winflexbison provides win_bison and win_flex.
find_package(BISON 3.6 REQUIRED)
find_package(FLEX REQUIRED)

BISON_TARGET(maille_grammar src/parser.y ${CMAKE_CURRENT_BINARY_DIR}/parser.c
  DEFINES_FILE ${CMAKE_CURRENT_BINARY_DIR}/parser.h COMPILE_FLAGS "-Wall -Werror")
FLEX_TARGET(maille_lexer src/lexer.l ${CMAKE_CURRENT_BINARY_DIR}/lexer.c
  DEFINES_FILE ${CMAKE_CURRENT_BINARY_DIR}/lexer.h)
ADD_FLEX_BISON_DEPENDENCY(maille_lexer maille_grammar)

add_library(maille STATIC src/ast.c src/maille.c ${BISON_maille_grammar_OUTPUTS} ${FLEX_maille_lexer_OUTPUTS})
target_include_directories(maille PUBLIC src ${CMAKE_CURRENT_BINARY_DIR})
# Generated code is not ours to warn about; our own files are compiled with warnings as errors.
set_source_files_properties(src/ast.c src/maille.c src/main.c PROPERTIES COMPILE_OPTIONS
  "$<IF:$<C_COMPILER_ID:MSVC>,/W4;/WX,-Wall;-Wextra;-Werror>")
if(MSVC)
  # strdup, fopen... are fine here; flex output uses isatty only when interactive (disabled).
  target_compile_definitions(maille PUBLIC _CRT_SECURE_NO_WARNINGS _CRT_NONSTDC_NO_DEPRECATE YY_NO_UNISTD_H)
endif()

add_executable(maillec src/main.c)
target_link_libraries(maillec PRIVATE maille)

if(MAILLE_BUILD_TESTS)
  enable_testing()
  add_executable(test_parser tests/test_parser.c)
  target_link_libraries(test_parser PRIVATE maille)
  set_source_files_properties(tests/test_parser.c PROPERTIES COMPILE_OPTIONS
    "$<IF:$<C_COMPILER_ID:MSVC>,/W4;/WX,-Wall;-Wextra;-Werror>")
  add_test(NAME parser_unit COMMAND test_parser)

  # Reference cases: tests/cases/NAME.maille gives NAME.ast (the tree) or NAME.err (the error line).
  file(GLOB cases RELATIVE ${CMAKE_CURRENT_SOURCE_DIR}/tests/cases ${CMAKE_CURRENT_SOURCE_DIR}/tests/cases/*.maille)
  foreach(case ${cases})
    get_filename_component(name ${case} NAME_WE)
    add_test(NAME case_${name} COMMAND ${CMAKE_COMMAND}
      -DMAILLEC=$<TARGET_FILE:maillec> -DCASE=${CMAKE_CURRENT_SOURCE_DIR}/tests/cases/${name}
      -P ${CMAKE_CURRENT_SOURCE_DIR}/tests/run_case.cmake)
  endforeach()
endif()
