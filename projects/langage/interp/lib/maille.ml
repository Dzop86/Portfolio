(* Whole pipeline from maillec's S-expression: type, then value, printed as OCaml's toplevel does:
   "- : int = 42". Errors come back as "line:column: kind: message". *)

module Sexp = Sexp
module Ast = Ast
module Types = Types
module Eval = Eval
module Mesh = Mesh

type report =
  | Value of { ty : string; value : string }
  | Failure of { kind : string;  (** "type error", "runtime error" or "malformed syntax tree" *) line : int; column : int; message : string }

let evaluate ?limits (sexp : string) : report =
  match Ast.parse_sexp sexp with
  | exception Ast.Malformed m -> Failure { kind = "malformed syntax tree"; line = 0; column = 0; message = m }
  | program -> (
      match Types.type_of program with
      | exception Types.Error (loc, m) -> Failure { kind = "type error"; line = loc.line; column = loc.column; message = m }
      | ty -> (
          match Eval.run ?limits program with
          | exception Eval.Error (loc, m) -> Failure { kind = "runtime error"; line = loc.line; column = loc.column; message = m }
          | v -> Value { ty = Types.to_string ty; value = Eval.to_string v }))

type outcome = Ok of string | Error of string

let run ?limits (sexp : string) : outcome =
  match evaluate ?limits sexp with
  | Value { ty; value } -> Ok (Printf.sprintf "- : %s = %s" ty value)
  | Failure { kind = "malformed syntax tree"; message; _ } -> Error ("malformed syntax tree: " ^ message)
  | Failure { kind; line; column; message } -> Error (Printf.sprintf "%d:%d: %s: %s" line column kind message)
