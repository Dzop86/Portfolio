(* Unit tests of the interpreter: S-expression reader, type inference, evaluation, limits. Trees are
   built directly here, so these tests do not depend on the C parser. *)

open Maille

let failures = ref 0

let check name cond =
  if not cond then (
    incr failures;
    Printf.eprintf "FAIL %s\n%!" name)

let check_eq name expected got =
  if expected <> got then (
    incr failures;
    Printf.eprintf "FAIL %s\n  expected %s\n  got      %s\n%!" name expected got)

(* Tree builders. Every node sits at 1:1: locations are checked through Maille.run on S-expressions
   written by hand (see pipeline), since OCaml evaluates constructor arguments in no fixed order. *)
let mk desc = { Ast.loc = { line = 1; column = 1 }; desc }
let i n = mk (Ast.Int n)
let f x = mk (Ast.Float x)
let s x = mk (Ast.String x)
let b x = mk (Ast.Bool x)
let v x = mk (Ast.Var x)
let ( @@@ ) fn arg = mk (Ast.App (fn, arg))
let fn x body = mk (Ast.Fun (x, body))
let let_ x e body = mk (Ast.Let (x, e, body))
let letrec x e body = mk (Ast.LetRec (x, e, body))
let if_ c t e = mk (Ast.If (c, t, e))
let op o a c = mk (Ast.Binop (o, a, c))

let type_of e =
  match Types.type_of e with t -> Types.to_string t | exception Types.Error (_, m) -> "type error: " ^ m

let type_error e =
  match Types.type_of e with
  | _ -> "no error"
  | exception Types.Error (_, m) -> m

let value ?limits e = Eval.to_string (Eval.run ?limits e)

let runtime_error ?limits e =
  match Eval.run ?limits e with
  | _ -> "no error"
  | exception Eval.Error (_, m) -> m

let sexp () =
  let open Sexp in
  check "atoms and lists" (parse "(a (b c) d)" = List [ Atom "a"; List [ Atom "b"; Atom "c" ]; Atom "d" ]);
  check "string escapes" (parse {|"a\"b\\c\nd"|} = Str "a\"b\\c\nd");
  check "spaces around" (parse "  ( x )\n" = List [ Atom "x" ]);
  List.iter
    (fun bad -> check ("malformed: " ^ bad) (match parse bad with _ -> false | exception Error _ -> true))
    [ "(a"; "a)"; "\"open"; ""; "(a) b"; {|"\q"|} ]

let tree_of_sexp () =
  let e = Ast.parse_sexp {|(let 1:1 x (int 1:9 1) (binop 1:16 + (var 1:14 x) (float 1:18 2.5)))|} in
  check "let node" (match e.desc with Ast.Let ("x", _, { desc = Binop ("+", _, _); loc = { column = 16; _ } }) -> true | _ -> false);
  List.iter
    (fun bad -> check ("malformed tree: " ^ bad) (match Ast.parse_sexp bad with _ -> false | exception Ast.Malformed _ -> true))
    [ "(int 1:1)"; "(int x 1)"; "(int 1:1 1.5)"; "(frob 1:1 1)"; "(bool 1:1 maybe)"; "42" ]

let inference () =
  check_eq "literal" "int" (type_of (i 1));
  check_eq "identity" "'a -> 'a" (type_of (fn "x" (v "x")));
  check_eq "compose" "('a -> 'b) -> ('c -> 'a) -> 'c -> 'b"
    (type_of (fn "f" (fn "g" (fn "x" (v "f" @@@ (v "g" @@@ v "x"))))));
  check_eq "let-polymorphism: id at two types" "int"
    (type_of (let_ "id" (fn "x" (v "x")) (if_ (v "id" @@@ b true) (v "id" @@@ i 1) (i 2))));
  check_eq "recursive factorial" "int -> int"
    (type_of (letrec "fact" (fn "n" (if_ (op "<=" (v "n") (i 1)) (i 1) (op "*" (v "n") (v "fact" @@@ op "-" (v "n") (i 1))))) (v "fact")));
  check_eq "comparison is polymorphic" "bool" (type_of (op "<" (s "a") (s "b")));
  check_eq "builtins" "float" (type_of (v "sqrt" @@@ (v "float_of_int" @@@ i 2)));
  check_eq "a lambda parameter is monomorphic"
    "this expression has type bool but an expression was expected of type int"
    (type_error (fn "g" (if_ (v "g" @@@ i 1) (v "g" @@@ b true) (b false))));
  check_eq "int + bool, located on the bool"
    "this expression has type bool but an expression was expected of type int" (type_error (op "+" (i 1) (b true)));
  check_eq "branches must agree"
    "this expression has type string but an expression was expected of type int"
    (type_error (if_ (b true) (i 1) (s "x")));
  check_eq "self-application"
    "this expression has type 'a -> 'b but an expression was expected of type 'a (the type would be infinite)"
    (type_error (fn "x" (v "x" @@@ v "x")));
  check_eq "int and float do not mix" "this expression has type float but an expression was expected of type int"
    (type_error (op "==" (i 1) (f 1.0)));
  check_eq "unbound name" "unbound name y" (type_error (v "y"));
  check_eq "applying an integer" "this expression has type int, it is not a function and cannot be applied"
    (type_error (i 3 @@@ i 4))

let fib = letrec "fib" (fn "n" (if_ (op "<" (v "n") (i 2)) (v "n") (op "+" (v "fib" @@@ op "-" (v "n") (i 1)) (v "fib" @@@ op "-" (v "n") (i 2)))))

let evaluation () =
  check_eq "arithmetic" "7" (value (op "+" (i 1) (op "*" (i 2) (i 3))));
  check_eq "integer division rounds toward zero" "-2" (value (op "/" (mk (Ast.Unop ("-", i 7))) (i 3)));
  check_eq "fib 20" "6765" (value (fib (v "fib" @@@ i 20)));
  check_eq "closures capture their definition" "2"
    (value (let_ "x" (i 1) (let_ "f" (fn "y" (op "+" (v "x") (v "y"))) (let_ "x" (i 10) (v "f" @@@ i 1)))));
  check_eq "&& short-circuits" "false" (value (op "&&" (b false) (op "==" (op "/" (i 1) (i 0)) (i 0))));
  check_eq "|| short-circuits" "true" (value (op "||" (b true) (op "==" (op "/" (i 1) (i 0)) (i 0))));
  check_eq "strings" "\"maille!\"" (value (op "^" (s "maille") (s "!")));
  check_eq "functions print as <fun>" "<fun>" (value (fn "x" (v "x")));
  check_eq "division by zero" "division by zero" (runtime_error (op "%" (i 1) (i 0)));
  check_eq "functions are not comparable" "functions cannot be compared"
    (runtime_error (op "==" (fn "x" (v "x")) (fn "x" (v "x"))));
  let small = { Eval.max_steps = 1_000; max_depth = 50 } in
  check_eq "step budget" "evaluation stopped after 1000 steps" (runtime_error ~limits:small (fib (v "fib" @@@ i 20)));
  check_eq "depth budget" "recursion deeper than 50 calls"
    (runtime_error ~limits:small (letrec "loop" (fn "n" (v "loop" @@@ v "n")) (v "loop" @@@ i 0)));
  check_eq "fib 10 under a small budget" "55" (value ~limits:{ small with max_steps = 100_000 } (fib (v "fib" @@@ i 10)))

let floats () =
  List.iter
    (fun (x, expected) -> check_eq (Printf.sprintf "float %h" x) expected (Eval.float_to_string x))
    [ (0.1, "0.1"); (2., "2.0"); (-3., "-3.0"); (1e20, "1e+20"); (2.5, "2.5"); (1. /. 3., "0.3333333333333333");
      (Float.sqrt 2., "1.4142135623730951"); (Float.infinity, "infinity") ]

let pipeline () =
  check_eq "whole run" "- : int = 3"
    (match Maille.run "(binop 1:3 + (int 1:1 1) (int 1:5 2))" with Ok s -> s | Error e -> e);
  check_eq "type error message" "1:5: type error: this expression has type bool but an expression was expected of type int"
    (match Maille.run "(binop 1:3 + (int 1:1 1) (bool 1:5 true))" with Ok s -> s | Error e -> e);
  check_eq "malformed tree" "malformed syntax tree: unclosed parenthesis"
    (match Maille.run "(int 1:1 1" with Ok s -> s | Error e -> e);
  check_eq "runtime error location" "2:7: runtime error: division by zero"
    (match Maille.run "(binop 2:7 / (int 2:5 1) (int 2:9 0))" with Ok s -> s | Error e -> e);
  check_eq "type error on the argument" "3:4: type error: this expression has type string but an expression was expected of type float"
    (match Maille.run {|(app 3:1 (var 3:1 sqrt) (str 3:4 "x"))|} with Ok s -> s | Error e -> e)

let () =
  sexp ();
  tree_of_sexp ();
  inference ();
  evaluation ();
  floats ();
  pipeline ();
  if !failures > 0 then (
    Printf.eprintf "%d failure(s)\n" !failures;
    exit 1)
  else print_endline "all interpreter tests passed"
