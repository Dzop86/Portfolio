# A person (black) plays d3 then an illegal move, and the input ends: the game shows the AI's answer,
# refuses the illegal move, and exits with 1 because the game is not over.
file(WRITE ${CMAKE_CURRENT_BINARY_DIR}/moves.txt "d3\na1\n")
execute_process(COMMAND ${CLI} --white ai:1 INPUT_FILE ${CMAKE_CURRENT_BINARY_DIR}/moves.txt
  OUTPUT_VARIABLE out RESULT_VARIABLE code)
foreach(expected "X plays d3" "O plays" "illegal move: a1" "input ended")
  string(FIND "${out}" "${expected}" at)
  if(at EQUAL -1)
    message(FATAL_ERROR "missing \"${expected}\" in:\n${out}")
  endif()
endforeach()
if(NOT code EQUAL 1)
  message(FATAL_ERROR "exit code ${code}, expected 1")
endif()
