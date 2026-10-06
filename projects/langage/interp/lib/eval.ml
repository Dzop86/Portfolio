(* Evaluator: call by value, closures over an immutable environment, a mutable cell only to tie the
   knot of let rec. Two budgets keep a program from running away (the site's demo runs it in the
   visitor's browser): a number of evaluation steps and a depth of nested calls. *)

type value =
  | VInt of int
  | VFloat of float
  | VBool of bool
  | VString of string
  | VMesh of Mesh.t * Mesh.invariants Lazy.t  (** invariants computed once, on first use *)
  | VClosure of string * Ast.expr * env ref
  | VBuiltin of string * (value -> value)

and env = value Types.Env.t

exception Error of Ast.loc * string

(* Raised by a built-in function; the call site turns it into an Error at its location. *)
exception Builtin_error of string

type limits = { max_steps : int; max_depth : int }

let default_limits = { max_steps = 10_000_000; max_depth = 10_000 }

let error loc fmt = Printf.ksprintf (fun s -> raise (Error (loc, s))) fmt

(* Shortest of %.15g, %.16g, %.17g that reads back as the same float, with ".0" for whole numbers:
   the same spelling as maillec's. *)
let float_to_string f =
  if Float.is_nan f then "nan"
  else if Float.is_integer f && Float.abs f < 1e16 then Printf.sprintf "%.1f" f
  else if Float.abs f = Float.infinity then if f > 0. then "infinity" else "-infinity"
  else
    let rec go d =
      let s = Printf.sprintf "%.*g" d f in
      if d >= 17 || float_of_string s = f then s else go (d + 1)
    in
    let s = go 15 in
    if String.exists (fun c -> c = '.' || c = 'e') s then s else s ^ ".0"

let mesh m = VMesh (m, lazy (Mesh.invariants m))

let make name build = function
  | VInt k when k < Mesh.min_resolution || k > Mesh.max_resolution ->
      raise (Builtin_error (Printf.sprintf "%s: resolution must be between %d and %d, got %d" name Mesh.min_resolution Mesh.max_resolution k))
  | VInt k -> mesh (build k)
  | _ -> assert false

let invariant f = function VMesh (_, inv) -> VInt (f (Lazy.force inv)) | _ -> assert false

let builtins =
  let num f = function VFloat x -> f x | _ -> assert false in
  [
    ("torus", make "torus" Mesh.torus);
    ("sphere", make "sphere" Mesh.sphere);
    ("cylinder", make "cylinder" Mesh.cylinder);
    ( "union",
      function
      | VMesh (a, _) ->
          VBuiltin
            ( "union",
              function
              | VMesh (b, _) when Mesh.vertex_count a + Mesh.vertex_count b > 4 * Mesh.max_resolution * Mesh.max_resolution ->
                  raise (Builtin_error "union: the result would be too large")
              | VMesh (b, _) -> mesh (Mesh.union a b)
              | _ -> assert false )
      | _ -> assert false );
    ("vertices", invariant (fun i -> i.Mesh.vertices));
    ("edges", invariant (fun i -> i.Mesh.edges));
    ("faces", invariant (fun i -> i.Mesh.faces));
    ("euler", invariant (fun i -> i.Mesh.euler));
    ("boundary_loops", invariant (fun i -> i.Mesh.boundary_loops));
    ("components", invariant (fun i -> i.Mesh.components));
    ("genus", invariant (fun i -> i.Mesh.genus));
    ("sqrt", num (fun x -> VFloat (Float.sqrt x)));
    ("float_of_int", function VInt i -> VFloat (float_of_int i) | _ -> assert false);
    ("int_of_float", num (fun x -> VInt (int_of_float x)));
    ("string_of_int", function VInt i -> VString (string_of_int i) | _ -> assert false);
    ("string_of_float", num (fun x -> VString (float_to_string x)));
    ("string_length", function VString s -> VInt (String.length s) | _ -> assert false);
  ]

let initial_env =
  List.fold_left (fun env (x, f) -> Types.Env.add x (VBuiltin (x, f)) env) Types.Env.empty builtins

(* Structural comparison of two values of the same type (the type checker guarantees it). *)
let rec compare_values loc a b =
  match (a, b) with
  | VInt x, VInt y -> compare x y
  | VFloat x, VFloat y -> compare x y
  | VBool x, VBool y -> compare x y
  | VString x, VString y -> compare x y
  | VMesh _, _ -> error loc "meshes cannot be compared (compare their invariants)"
  | (VClosure _ | VBuiltin _), _ -> error loc "functions cannot be compared"
  | _ -> ignore (compare_values loc b a); assert false

let run ?(limits = default_limits) (program : Ast.expr) : value =
  let steps = ref 0 in
  let rec eval env depth (e : Ast.expr) =
    incr steps;
    if !steps > limits.max_steps then error e.loc "evaluation stopped after %d steps" limits.max_steps;
    match e.desc with
    | Int i -> VInt i
    | Float f -> VFloat f
    | String s -> VString s
    | Bool b -> VBool b
    | Var x -> (
        match Types.Env.find_opt x env with Some v -> v | None -> error e.loc "unbound name %s" x)
    | Fun (x, body) -> VClosure (x, body, ref env)
    | Let (x, v, body) -> eval (Types.Env.add x (eval env depth v) env) depth body
    | LetRec (f, v, body) ->
        let value =
          match eval env depth v with
          | VClosure (x, b, cell) ->
              let clo = VClosure (x, b, cell) in
              cell := Types.Env.add f clo !cell;
              clo
          | other -> other
        in
        eval (Types.Env.add f value env) depth body
    | App (f, arg) -> (
        let fv = eval env depth f in
        let av = eval env depth arg in
        if depth >= limits.max_depth then error e.loc "recursion deeper than %d calls" limits.max_depth;
        match fv with
        | VClosure (x, body, cell) -> eval (Types.Env.add x av !cell) (depth + 1) body
        | VBuiltin (_, prim) -> ( try prim av with Builtin_error m -> error e.loc "%s" m)
        | _ -> error e.loc "not a function")
    | If (c, t, f) -> (
        match eval env depth c with
        | VBool true -> eval env depth t
        | VBool false -> eval env depth f
        | _ -> error c.loc "not a boolean")
    | Binop ("&&", a, b) -> (
        match eval env depth a with VBool false -> VBool false | _ -> eval env depth b)
    | Binop ("||", a, b) -> (
        match eval env depth a with VBool true -> VBool true | _ -> eval env depth b)
    | Binop (op, a, b) -> binop e.loc op (eval env depth a) (eval env depth b)
    | Unop ("-", a) -> (match eval env depth a with VInt i -> VInt (-i) | _ -> error e.loc "not an integer")
    | Unop ("not", a) -> (match eval env depth a with VBool x -> VBool (not x) | _ -> error e.loc "not a boolean")
    | Unop (op, _) -> error e.loc "unknown operator %s" op
  and binop loc op a b =
    match (op, a, b) with
    | "+", VInt x, VInt y -> VInt (x + y)
    | "-", VInt x, VInt y -> VInt (x - y)
    | "*", VInt x, VInt y -> VInt (x * y)
    | ("/" | "%"), VInt _, VInt 0 -> error loc "division by zero"
    | "/", VInt x, VInt y -> VInt (x / y)
    | "%", VInt x, VInt y -> VInt (x mod y)
    | "+.", VFloat x, VFloat y -> VFloat (x +. y)
    | "-.", VFloat x, VFloat y -> VFloat (x -. y)
    | "*.", VFloat x, VFloat y -> VFloat (x *. y)
    | "/.", VFloat x, VFloat y -> VFloat (x /. y)
    | "^", VString x, VString y -> VString (x ^ y)
    | "==", _, _ -> VBool (compare_values loc a b = 0)
    | "!=", _, _ -> VBool (compare_values loc a b <> 0)
    | "<", _, _ -> VBool (compare_values loc a b < 0)
    | "<=", _, _ -> VBool (compare_values loc a b <= 0)
    | ">", _, _ -> VBool (compare_values loc a b > 0)
    | ">=", _, _ -> VBool (compare_values loc a b >= 0)
    | _ -> error loc "bad operands for %s" op
  in
  eval initial_env 0 program

let to_string = function
  | VInt i -> string_of_int i
  | VFloat f -> float_to_string f
  | VBool b -> string_of_bool b
  | VString s -> Printf.sprintf "%S" s
  | VMesh (m, _) -> Mesh.to_string m
  | VClosure _ | VBuiltin _ -> "<fun>"
