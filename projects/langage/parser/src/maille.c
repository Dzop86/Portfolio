#include "maille.h"

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "lex_state.h"
#include "parser.h"
#include "lexer.h"

void report_error(maille_result *result, int line, int column, const char *format, ...) {
    if (result->message[0]) return;
    result->line = line;
    result->column = column;
    va_list args;
    va_start(args, format);
    vsnprintf(result->message, sizeof result->message, format, args);
    va_end(args);
}

int lex_append(struct lex_state *state, char c) {
    if (state->buffer_len + 1 >= state->buffer_cap) {
        size_t cap = state->buffer_cap ? 2 * state->buffer_cap : 64;
        char *grown = realloc(state->buffer, cap);
        if (!grown) return 1;
        state->buffer = grown;
        state->buffer_cap = cap;
    }
    state->buffer[state->buffer_len++] = c;
    return 0;
}

char *lex_take_buffer(struct lex_state *state) {
    char *s = malloc(state->buffer_len + 1);
    if (!s) return NULL;
    if (state->buffer_len) memcpy(s, state->buffer, state->buffer_len);
    s[state->buffer_len] = '\0';
    return s;
}

maille_status maille_parse(const char *source, size_t len, maille_result *result) {
    memset(result, 0, sizeof *result);
    if (memchr(source, '\0', len)) {
        report_error(result, 1, 1, "the source contains a NUL byte");
        return MAILLE_SYNTAX_ERROR;
    }
    struct lex_state state = { .line = 1, .column = 1, .result = result };
    yyscan_t scanner;
    if (yylex_init_extra(&state, &scanner)) return MAILLE_OUT_OF_MEMORY;
    YY_BUFFER_STATE buffer = yy_scan_bytes(source, (int)len, scanner);
    int status = buffer ? yyparse(scanner, result) : 2;
    if (buffer) yy_delete_buffer(buffer, scanner);
    yylex_destroy(scanner);
    free(state.buffer);
    if (status == 0) return MAILLE_OK;
    node_free(result->tree);
    result->tree = NULL;
    /* yyparse returns 2 when its stack is exhausted: YYMAXDEPTH (10 000) nested parentheses or
     * operators waiting for their right operand, or a failed allocation. */
    if (status == 2) {
        result->message[0] = '\0';
        report_error(result, 1, 1, "expression nested too deeply (parser stack exhausted)");
    }
    if (!result->message[0]) report_error(result, 1, 1, "syntax error");
    return MAILLE_SYNTAX_ERROR;
}
