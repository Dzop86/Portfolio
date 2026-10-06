(* Syntax tree of a Maille program, read from maillec's S-expression output. *)

type loc = { line : int; column : int }

type expr = { loc : loc; desc : desc }

and desc =
  | Int of int
  | Float of float
  | String of string
  | Bool of bool
  | Var of string
  | Let of string * expr * expr
  | LetRec of string * expr * expr
  | Fun of string * expr
  | App of expr * expr
  | If of expr * expr * expr
  | Binop of string * expr * expr
  | Unop of string * expr

exception Malformed of string

let malformed fmt = Printf.ksprintf (fun s -> raise (Malformed s)) fmt

let loc_of_atom a =
  match String.split_on_char ':' a with
  | [ l; c ] -> (
      match (int_of_string_opt l, int_of_string_opt c) with
      | Some line, Some column -> { line; column }
      | _ -> malformed "bad location %S" a)
  | _ -> malformed "bad location %S" a

let rec of_sexp (s : Sexp.t) : expr =
  let open Sexp in
  match s with
  | List (Atom kind :: Atom pos :: args) -> (
      let loc = loc_of_atom pos in
      let mk desc = { loc; desc } in
      match (kind, args) with
      | "int", [ Atom v ] -> (
          match int_of_string_opt v with Some i -> mk (Int i) | None -> malformed "bad integer %S" v)
      | "float", [ Atom v ] -> (
          match float_of_string_opt v with Some f -> mk (Float f) | None -> malformed "bad float %S" v)
      | "str", [ Str v ] -> mk (String v)
      | "bool", [ Atom "true" ] -> mk (Bool true)
      | "bool", [ Atom "false" ] -> mk (Bool false)
      | "var", [ Atom x ] -> mk (Var x)
      | "let", [ Atom x; v; body ] -> mk (Let (x, of_sexp v, of_sexp body))
      | "letrec", [ Atom x; v; body ] -> mk (LetRec (x, of_sexp v, of_sexp body))
      | "fun", [ Atom x; body ] -> mk (Fun (x, of_sexp body))
      | "app", [ f; a ] -> mk (App (of_sexp f, of_sexp a))
      | "if", [ c; t; e ] -> mk (If (of_sexp c, of_sexp t, of_sexp e))
      | "binop", [ Atom op; a; b ] -> mk (Binop (op, of_sexp a, of_sexp b))
      | "unop", [ Atom op; a ] -> mk (Unop (op, of_sexp a))
      | _ -> malformed "unexpected node %S at %d:%d" kind loc.line loc.column)
  | _ -> malformed "expected a node"

let parse_sexp (text : string) : expr =
  match Sexp.parse text with
  | s -> of_sexp s
  | exception Sexp.Error m -> malformed "%s" m
