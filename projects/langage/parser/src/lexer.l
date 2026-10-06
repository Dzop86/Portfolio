/* Lexer of Maille: reentrant, reads from a memory buffer, tracks line and column (1-based) of each
 * token. A lexical error is reported through report_error and returned as YYerror, so the parser
 * stops without adding its own "syntax error". */
%option reentrant bison-bridge bison-locations
%option noyywrap nounput noinput never-interactive nounistd
%option extra-type="struct lex_state *"

%{
#include <errno.h>
#include <stdlib.h>
#include <string.h>

#include "parser.h"
#include "lex_state.h"

/* Every token starts where the previous one ended; newlines move to the next line. Columns count
 * characters, not bytes: UTF-8 continuation bytes (10xxxxxx) do not advance them. */
#define YY_USER_ACTION                                         \
    yylloc->first_line = yyextra->line;                        \
    yylloc->first_column = yyextra->column;                    \
    for (int i_ = 0; i_ < yyleng; i_++) {                      \
        if (yytext[i_] == '\n') { yyextra->line++; yyextra->column = 1; } \
        else if ((yytext[i_] & 0xC0) != 0x80) yyextra->column++; \
    }                                                          \
    yylloc->last_line = yyextra->line;                         \
    yylloc->last_column = yyextra->column;

#define LEX_ERROR(...)                                         \
    do {                                                       \
        report_error(yyextra->result, yylloc->first_line, yylloc->first_column, __VA_ARGS__); \
        return YYerror;                                        \
    } while (0)
%}

%x STR

DIGIT   [0-9]
IDENT   [a-z_][A-Za-z0-9_']*

%%

[ \t\r\n]+            ;
"#"[^\n]*             ;

"let"                 return LET;
"rec"                 return REC;
"in"                  return IN;
"fun"                 return FUN;
"if"                  return IF;
"then"                return THEN;
"else"                return ELSE;
"true"                return TRUE;
"false"               return FALSE;
"not"                 return NOT;

"->"                  return ARROW;
"=="                  return EQEQ;
"!="                  return NE;
"<="                  return LE;
">="                  return GE;
"&&"                  return AND;
"||"                  return OR;
"+."                  return PLUSDOT;
"-."                  return MINUSDOT;
"*."                  return STARDOT;
"/."                  return SLASHDOT;
"="                   return EQ;
"<"                   return LT;
">"                   return GT;
"+"                   return PLUS;
"-"                   return MINUS;
"*"                   return STAR;
"/"                   return SLASH;
"%"                   return PERCENT;
"^"                   return CARET;
"("                   return LPAREN;
")"                   return RPAREN;

{DIGIT}+"."{DIGIT}*([eE][+-]?{DIGIT}+)?|{DIGIT}+[eE][+-]?{DIGIT}+ {
    errno = 0;
    yylval->fval = strtod(yytext, NULL);
    if (errno == ERANGE && (yylval->fval > 1 || yylval->fval < -1)) LEX_ERROR("float literal out of range: %s", yytext);
    return FLOAT;
}

{DIGIT}+ {
    errno = 0;
    long long v = strtoll(yytext, NULL, 10);
    /* 2^62 - 1 at most: OCaml's native int has 63 bits. */
    if (errno == ERANGE || v > 4611686018427387903LL) LEX_ERROR("integer literal too large: %s", yytext);
    yylval->ival = v;
    return INT;
}

{IDENT} {
    yylval->str = strdup(yytext);
    if (!yylval->str) LEX_ERROR("out of memory");
    return IDENT;
}

\" {
    yyextra->string_line = yylloc->first_line;
    yyextra->string_column = yylloc->first_column;
    yyextra->buffer_len = 0;
    BEGIN(STR);
}

<STR>{
\" {
    BEGIN(INITIAL);
    yylloc->first_line = yyextra->string_line;
    yylloc->first_column = yyextra->string_column;
    yylval->str = lex_take_buffer(yyextra);
    if (!yylval->str) LEX_ERROR("out of memory");
    return STRING;
}
\\n                   if (lex_append(yyextra, '\n')) LEX_ERROR("out of memory");
\\t                   if (lex_append(yyextra, '\t')) LEX_ERROR("out of memory");
\\\\                  if (lex_append(yyextra, '\\')) LEX_ERROR("out of memory");
\\\"                  if (lex_append(yyextra, '"')) LEX_ERROR("out of memory");
\\(.|\n)              { BEGIN(INITIAL); LEX_ERROR("unknown escape \\%c in string", yytext[1] == '\n' ? 'n' : yytext[1]); }
\n                    {
    BEGIN(INITIAL);
    report_error(yyextra->result, yyextra->string_line, yyextra->string_column, "unterminated string");
    return YYerror;
}
<<EOF>>               {
    BEGIN(INITIAL);
    report_error(yyextra->result, yyextra->string_line, yyextra->string_column, "unterminated string");
    return YYerror;
}
[^\\"\n]+             {
    for (int i = 0; i < yyleng; i++) {
        if (lex_append(yyextra, yytext[i])) LEX_ERROR("out of memory");
    }
}
}

<<EOF>>               {
    /* No YY_USER_ACTION at end of input: an error there points just past the last character. */
    yylloc->first_line = yylloc->last_line = yyextra->line;
    yylloc->first_column = yylloc->last_column = yyextra->column;
    return YYEOF;
}

[A-Z][A-Za-z0-9_']*   LEX_ERROR("names start with a lower-case letter: %s", yytext);
.                     LEX_ERROR("unexpected character '%s'", yytext);

%%
