/* Grammar of Maille. Precedence is written as one rule per level (lowest first), so the grammar has
 * no conflicts and needs no %prec:
 *   let / let rec / fun / if   (extend as far right as possible)
 *   ||  &&  (left)
 *   == != < <= > >=            (non-associative)
 *   ^                          (right)
 *   + - +. -.                  (left)
 *   * / % *. /.                (left)
 *   - not                      (prefix)
 *   application f x y          (left)
 * `let f x y = e` and `fun x y -> e` are desugared into nested one-argument functions. */
%require "3.6"
%define api.pure full
%define parse.error detailed
%locations
%param {yyscan_t scanner}
%parse-param {maille_result *result}

%code requires {
#include "maille.h"
typedef void *yyscan_t;

/* Parameter names of a function, in order. */
typedef struct name_list {
    char *name;
    int line, column;
    struct name_list *next;
} name_list;
}

%code {
#include <stdlib.h>
#include <string.h>

#include "lex_state.h"
#include "lexer.h"

static void yyerror(YYLTYPE *loc, yyscan_t scanner, maille_result *result, const char *message) {
    (void)scanner;
    report_error(result, loc->first_line, loc->first_column, "%s", message);
}

static void names_free(name_list *names) {
    while (names) {
        name_list *next = names->next;
        free(names->name);
        free(names);
        names = next;
    }
}

static name_list *names_cons(char *name, YYLTYPE loc, name_list *rest) {
    name_list *n = malloc(sizeof *n);
    if (!n) {
        free(name);
        names_free(rest);
        return NULL;
    }
    n->name = name;
    n->line = loc.first_line;
    n->column = loc.first_column;
    n->next = rest;
    return n;
}

/* fun x -> (fun y -> body) for the names x, y; consumes both arguments. */
static node *curry(name_list *names, node *body) {
    if (!names) return body;
    node *inner = curry(names->next, body);
    names->next = NULL;
    node *f = inner ? node_text(N_FUN, names->line, names->column, names->name) : NULL;
    names->name = NULL;
    names_free(names);
    if (!f) node_free(inner);
    return f ? node_with(f, inner, NULL, NULL) : NULL;
}

static node *binop(const char *op, YYLTYPE loc, node *left, node *right) {
    char *text = strdup(op);
    node *n = text ? node_text(N_BINOP, loc.first_line, loc.first_column, text) : NULL;
    return node_with(n, left, right, NULL);
}

/* node_with and curry return NULL when out of memory or past MAILLE_MAX_DEPTH. */
#define CHECK(value, loc)                                                              \
    do {                                                                               \
        if (!(value)) {                                                                \
            report_error(result, (loc).first_line, (loc).first_column,                 \
                         "expression nested too deeply (or out of memory)");           \
            YYABORT;                                                                   \
        }                                                                              \
    } while (0)
}

%union {
    node *node;
    char *str;
    long long ival;
    double fval;
    name_list *names;
    const char *op;   /* operator spelling, a string literal: nothing to free */
}

%token LET "let" REC "rec" IN "in" FUN "fun" IF "if" THEN "then" ELSE "else"
%token TRUE "true" FALSE "false" NOT "not"
%token ARROW "->" EQ "=" EQEQ "==" NE "!=" LT "<" LE "<=" GT ">" GE ">="
%token PLUS "+" MINUS "-" STAR "*" SLASH "/" PERCENT "%"
%token PLUSDOT "+." MINUSDOT "-." STARDOT "*." SLASHDOT "/."
%token CARET "^" AND "&&" OR "||" LPAREN "(" RPAREN ")"
%token <str> IDENT "name" STRING "string"
%token <ival> INT "integer"
%token <fval> FLOAT "float"

%type <node> expr or_expr and_expr cmp_expr concat_expr add_expr mul_expr unary app atom
%type <names> params params1
%type <op> cmp_op add_op mul_op

%destructor { node_free($$); } <node>
%destructor { free($$); } <str>
%destructor { names_free($$); } <names>

%%

program:
    expr                                   { result->tree = $1; }
  ;

expr:
    "let" IDENT params "=" expr "in" expr  {
        node *value = curry($3, $5);
        node *n = value ? node_text(N_LET, @1.first_line, @1.first_column, $2) : (free($2), NULL);
        if (!n) node_free(value);
        $$ = n ? node_with(n, value, $7, NULL) : (node_free($7), NULL);
        CHECK($$, @1);
    }
  | "let" "rec" IDENT params1 "=" expr "in" expr {
        node *value = curry($4, $6);
        node *n = value ? node_text(N_LETREC, @1.first_line, @1.first_column, $3) : (free($3), NULL);
        if (!n) node_free(value);
        $$ = n ? node_with(n, value, $8, NULL) : (node_free($8), NULL);
        CHECK($$, @1);
    }
  | "fun" params1 "->" expr                { $$ = curry($2, $4); CHECK($$, @1); }
  | "if" expr "then" expr "else" expr      {
        $$ = node_with(node_new(N_IF, @1.first_line, @1.first_column), $2, $4, $6);
        CHECK($$, @1);
    }
  | or_expr
  ;

params:
    %empty                                 { $$ = NULL; }
  | params1
  ;

params1:
    IDENT params                           { $$ = names_cons($1, @1, $2); CHECK($$, @1); }
  ;

or_expr:
    or_expr "||" and_expr                  { $$ = binop("||", @2, $1, $3); CHECK($$, @2); }
  | and_expr
  ;

and_expr:
    and_expr "&&" cmp_expr                 { $$ = binop("&&", @2, $1, $3); CHECK($$, @2); }
  | cmp_expr
  ;

cmp_expr:
    concat_expr cmp_op concat_expr         { $$ = binop($2, @2, $1, $3); CHECK($$, @2); }
  | concat_expr
  ;

cmp_op:
    "=="  { $$ = "=="; } | "!=" { $$ = "!="; }
  | "<"   { $$ = "<"; }  | "<=" { $$ = "<="; }
  | ">"   { $$ = ">"; }  | ">=" { $$ = ">="; }
  ;

concat_expr:
    add_expr "^" concat_expr               { $$ = binop("^", @2, $1, $3); CHECK($$, @2); }
  | add_expr
  ;

add_expr:
    add_expr add_op mul_expr               { $$ = binop($2, @2, $1, $3); CHECK($$, @2); }
  | mul_expr
  ;

add_op:
    "+"   { $$ = "+"; }  | "-"  { $$ = "-"; }
  | "+."  { $$ = "+."; } | "-." { $$ = "-."; }
  ;

mul_expr:
    mul_expr mul_op unary                  { $$ = binop($2, @2, $1, $3); CHECK($$, @2); }
  | unary
  ;

mul_op:
    "*"   { $$ = "*"; }  | "/"  { $$ = "/"; }  | "%" { $$ = "%"; }
  | "*."  { $$ = "*."; } | "/." { $$ = "/."; }
  ;

unary:
    "-" unary                              {
        char *op = strdup("-");
        $$ = node_with(op ? node_text(N_UNOP, @1.first_line, @1.first_column, op) : NULL, $2, NULL, NULL);
        CHECK($$, @1);
    }
  | "not" unary                            {
        char *op = strdup("not");
        $$ = node_with(op ? node_text(N_UNOP, @1.first_line, @1.first_column, op) : NULL, $2, NULL, NULL);
        CHECK($$, @1);
    }
  | app
  ;

app:
    app atom                               {
        /* An application starts where its function does. */
        $$ = node_with(node_new(N_APP, $1->line, $1->column), $1, $2, NULL);
        CHECK($$, @2);
    }
  | atom
  ;

atom:
    INT                                    {
        $$ = node_new(N_INT, @1.first_line, @1.first_column);
        CHECK($$, @1);
        $$->int_value = $1;
    }
  | FLOAT                                  {
        $$ = node_new(N_FLOAT, @1.first_line, @1.first_column);
        CHECK($$, @1);
        $$->float_value = $1;
    }
  | STRING                                 { $$ = node_text(N_STRING, @1.first_line, @1.first_column, $1); CHECK($$, @1); }
  | "true"                                 {
        $$ = node_new(N_BOOL, @1.first_line, @1.first_column);
        CHECK($$, @1);
        $$->int_value = 1;
    }
  | "false"                                { $$ = node_new(N_BOOL, @1.first_line, @1.first_column); CHECK($$, @1); }
  | IDENT                                  { $$ = node_text(N_VAR, @1.first_line, @1.first_column, $1); CHECK($$, @1); }
  | "(" expr ")"                           { $$ = $2; }
  ;

%%
