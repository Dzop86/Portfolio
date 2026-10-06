cmake_minimum_required(VERSION 3.20)
project(othello VERSION 0.1.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

option(OTH_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer (GCC, Clang)" OFF)
option(OTH_BUILD_TESTS "Build the Unity test suite" ON)

function(oth_warnings target)
  if(MSVC)
    target_compile_options(${target} PRIVATE /W4 /WX)
    target_compile_definitions(${target} PRIVATE _CRT_SECURE_NO_WARNINGS)
  else()
    target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Werror)
  endif()
endfunction()

if(OTH_SANITIZE AND NOT MSVC)
  add_compile_options(-fsanitize=address,undefined -fno-omit-frame-pointer -fno-sanitize-recover=all)
  add_link_options(-fsanitize=address,undefined)
endif()

add_library(othello src/board.c src/ai.c)
target_include_directories(othello PUBLIC include)
oth_warnings(othello)

add_executable(othello_cli tools/othello_cli.c)
set_target_properties(othello_cli PROPERTIES OUTPUT_NAME othello)
target_link_libraries(othello_cli PRIVATE othello)
oth_warnings(othello_cli)

if(OTH_BUILD_TESTS)
  enable_testing()
  include(FetchContent)
  FetchContent_Declare(unity
    GIT_REPOSITORY https://github.com/ThrowTheSwitch/Unity.git
    GIT_TAG v2.6.1
    GIT_SHALLOW TRUE)
  FetchContent_MakeAvailable(unity)

  foreach(name board ai)
    add_executable(test_${name} tests/test_${name}.c)
    target_link_libraries(test_${name} PRIVATE othello unity)
    oth_warnings(test_${name})
    add_test(NAME unit.${name} COMMAND test_${name})
  endforeach()

  # The terminal game, end to end: two AIs play a whole game; a scripted person makes one move.
  add_test(NAME cli.ai_vs_ai COMMAND othello_cli --black ai:2 --white ai:3)
  set_tests_properties(cli.ai_vs_ai PROPERTIES PASS_REGULAR_EXPRESSION "game over: X [0-9]+, O [0-9]+")
  add_test(NAME cli.bad_argument COMMAND othello_cli --black robot)
  set_tests_properties(cli.bad_argument PROPERTIES WILL_FAIL TRUE)
  add_test(NAME cli.scripted COMMAND ${CMAKE_COMMAND} -DCLI=$<TARGET_FILE:othello_cli>
    -P ${CMAKE_CURRENT_SOURCE_DIR}/tests/scripted.cmake)
endif()
