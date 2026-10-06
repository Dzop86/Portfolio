/* State shared by the lexer and the parser for one parse. */
#ifndef MAILLE_LEX_STATE_H
#define MAILLE_LEX_STATE_H

#include <stddef.h>

#include "maille.h"

struct lex_state {
    int line, column;                /* where the next token starts */
    int string_line, string_column;  /* opening quote of the string being read */
    char *buffer;                    /* contents of that string */
    size_t buffer_len, buffer_cap;
    maille_result *result;
};

/* Records the first error only (later ones are consequences); message is printf-style. */
void report_error(maille_result *result, int line, int column, const char *format, ...);
/* Appends a character to the string buffer; non-zero if out of memory. */
int lex_append(struct lex_state *state, char c);
/* Copy of the string buffer as a new C string; NULL if out of memory. */
char *lex_take_buffer(struct lex_state *state);

#endif
