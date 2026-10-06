(* Hindley-Milner type inference with let-polymorphism, using levels (Rémy's algorithm): a type
   variable created deeper than the current let can be generalised when that let is done. The language
   has no mutable values, so every let can be generalised (no value restriction needed). *)

type ty =
  | TInt
  | TFloat
  | TBool
  | TString
  | TArrow of ty * ty
  | TVar of tvar ref
  | TGen of int  (** quantified variable of a type scheme *)

and tvar = Unbound of int * int  (** id, level *) | Link of ty

exception Error of Ast.loc * string

let counter = ref 0
let level = ref 1

let fresh_var () =
  incr counter;
  TVar (ref (Unbound (!counter, !level)))

let rec repr = function TVar { contents = Link t } -> repr t | t -> t

(* Type variables are named 'a, 'b... in the order they appear, separately for each printed type. *)
let to_string ?(names = Hashtbl.create 8) t =
  let name_of key =
    match Hashtbl.find_opt names key with
    | Some n -> n
    | None ->
        let i = Hashtbl.length names in
        let n = if i < 26 then Printf.sprintf "'%c" (Char.chr (97 + i)) else Printf.sprintf "'t%d" i in
        Hashtbl.add names key n;
        n
  in
  let rec go arrow_left t =
    match repr t with
    | TInt -> "int"
    | TFloat -> "float"
    | TBool -> "bool"
    | TString -> "string"
    | TVar { contents = Unbound (id, _) } -> name_of (`Var id)
    | TVar { contents = Link _ } -> assert false
    | TGen i -> name_of (`Gen i)
    | TArrow (a, b) ->
        (* Left first: OCaml evaluates the operands of ^ right to left, which would name b before a. *)
        let left = go true a in
        let s = left ^ " -> " ^ go false b in
        if arrow_left then "(" ^ s ^ ")" else s
  in
  go false t

let error loc fmt = Printf.ksprintf (fun s -> raise (Error (loc, s))) fmt

let rec occurs id lvl t =
  match repr t with
  | TVar ({ contents = Unbound (id', lvl') } as r) ->
      if id = id' then true
      else (
        (* The variable escapes no deeper than the one it is unified with. *)
        if lvl' > lvl then r := Unbound (id', lvl);
        false)
  | TArrow (a, b) -> occurs id lvl a || occurs id lvl b
  | _ -> false

exception Mismatch
exception Infinite

let rec unify a b =
  match (repr a, repr b) with
  | TInt, TInt | TFloat, TFloat | TBool, TBool | TString, TString -> ()
  | TVar r1, TVar r2 when r1 == r2 -> ()
  | TVar ({ contents = Unbound (id, lvl) } as r), t | t, TVar ({ contents = Unbound (id, lvl) } as r) ->
      if occurs id lvl t then raise Infinite;
      r := Link t
  | TArrow (a1, b1), TArrow (a2, b2) ->
      unify a1 a2;
      unify b1 b2
  | _ -> raise Mismatch

(* Unifies, or reports "this expression has type <actual> but ... <expected>" at the expression. *)
let expect loc ~actual ~expected =
  let report suffix =
    let names = Hashtbl.create 8 in
    let a = to_string ~names actual in
    let e = to_string ~names expected in
    error loc "this expression has type %s but an expression was expected of type %s%s" a e suffix
  in
  try unify actual expected with
  | Mismatch -> report ""
  | Infinite -> report " (the type would be infinite)"

let generalize t =
  let rec go t =
    match repr t with
    | TVar { contents = Unbound (id, lvl) } when lvl > !level -> TGen id
    | TArrow (a, b) -> TArrow (go a, go b)
    | t -> t
  in
  go t

let instantiate t =
  let subst = Hashtbl.create 4 in
  let rec go t =
    match repr t with
    | TGen id -> (
        match Hashtbl.find_opt subst id with
        | Some v -> v
        | None ->
            let v = fresh_var () in
            Hashtbl.add subst id v;
            v)
    | TArrow (a, b) -> TArrow (go a, go b)
    | t -> t
  in
  go t

module Env = Map.Make (String)

(* Built-in functions; their values are in Eval.builtins. *)
let builtins =
  [
    ("sqrt", TArrow (TFloat, TFloat));
    ("float_of_int", TArrow (TInt, TFloat));
    ("int_of_float", TArrow (TFloat, TInt));
    ("string_of_int", TArrow (TInt, TString));
    ("string_of_float", TArrow (TFloat, TString));
    ("string_length", TArrow (TString, TInt));
  ]

let initial_env = List.fold_left (fun env (x, t) -> Env.add x t env) Env.empty builtins

let binop_type op =
  let a = fresh_var () in
  let arr x y z = (x, y, z) in
  match op with
  | "+" | "-" | "*" | "/" | "%" -> arr TInt TInt TInt
  | "+." | "-." | "*." | "/." -> arr TFloat TFloat TFloat
  | "^" -> arr TString TString TString
  | "&&" | "||" -> arr TBool TBool TBool
  | "==" | "!=" | "<" | "<=" | ">" | ">=" -> arr a a TBool
  | _ -> invalid_arg ("binop " ^ op)

let rec infer env (e : Ast.expr) : ty =
  match e.desc with
  | Int _ -> TInt
  | Float _ -> TFloat
  | String _ -> TString
  | Bool _ -> TBool
  | Var x -> (
      match Env.find_opt x env with
      | Some t -> instantiate t
      | None -> error e.loc "unbound name %s" x)
  | Fun (x, body) ->
      let a = fresh_var () in
      let b = infer (Env.add x a env) body in
      TArrow (a, b)
  | App (f, arg) ->
      let tf = infer env f in
      (match repr tf with
       | TArrow _ | TVar _ -> ()
       | t -> error f.loc "this expression has type %s, it is not a function and cannot be applied" (to_string t));
      (* Function first (fails only on x x, by the occurs check), then the argument against its parameter. *)
      let param = fresh_var () and result = fresh_var () in
      expect f.loc ~actual:tf ~expected:(TArrow (param, result));
      expect arg.loc ~actual:(infer env arg) ~expected:param;
      result
  | Let (x, v, body) ->
      incr level;
      let tv = infer env v in
      decr level;
      infer (Env.add x (generalize tv) env) body
  | LetRec (f, v, body) ->
      incr level;
      let tf = fresh_var () in
      let tv = infer (Env.add f tf env) v in
      expect v.loc ~actual:tv ~expected:tf;
      decr level;
      infer (Env.add f (generalize tf) env) body
  | If (c, t, f) ->
      expect c.loc ~actual:(infer env c) ~expected:TBool;
      let tt = infer env t in
      expect f.loc ~actual:(infer env f) ~expected:tt;
      tt
  | Binop (op, a, b) ->
      let ta, tb, tr = binop_type op in
      expect a.loc ~actual:(infer env a) ~expected:ta;
      expect b.loc ~actual:(infer env b) ~expected:tb;
      tr
  | Unop ("-", a) ->
      expect a.loc ~actual:(infer env a) ~expected:TInt;
      TInt
  | Unop ("not", a) ->
      expect a.loc ~actual:(infer env a) ~expected:TBool;
      TBool
  | Unop (op, _) -> error e.loc "unknown operator %s" op

(* Type of a whole program, generalised and with variables named from 'a. *)
let type_of (e : Ast.expr) : ty =
  counter := 0;
  level := 1;
  incr level;
  let t = infer initial_env e in
  decr level;
  generalize t
