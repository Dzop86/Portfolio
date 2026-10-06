(* Whole pipeline from maillec's S-expression: type, then value, printed as OCaml's toplevel does:
   "- : int = 42". Errors come back as "line:column: kind: message". *)

module Sexp = Sexp
module Ast = Ast
module Types = Types
module Eval = Eval
module Mesh = Mesh

type outcome = Ok of string | Error of string

let run ?limits (sexp : string) : outcome =
  match Ast.parse_sexp sexp with
  | exception Ast.Malformed m -> Error ("malformed syntax tree: " ^ m)
  | program -> (
      match Types.type_of program with
      | exception Types.Error (loc, m) -> Error (Printf.sprintf "%d:%d: type error: %s" loc.line loc.column m)
      | ty -> (
          match Eval.run ?limits program with
          | exception Eval.Error (loc, m) -> Error (Printf.sprintf "%d:%d: runtime error: %s" loc.line loc.column m)
          | v -> Ok (Printf.sprintf "- : %s = %s" (Types.to_string ty) (Eval.to_string v))))
