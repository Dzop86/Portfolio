(* Browser entry point: sets globalThis.MailleInterp.run(sexp), which returns
   { ok: true, type, value } or { ok: false, kind, line, column, message }. The budgets are smaller
   than on the command line: the program runs in the visitor's page, and each Maille call takes several
   JavaScript frames (5 000 nested calls overflowed the stack of Node 22 on macOS). *)

open Js_of_ocaml

let limits = { Maille.Eval.max_steps = 2_000_000; max_depth = 1_000 }

let obj fields = Js.Unsafe.obj (Array.of_list fields)
let str s = Js.Unsafe.inject (Js.string s)
let int i = Js.Unsafe.inject i
let bool b = Js.Unsafe.inject (Js.bool b)

let run sexp =
  match Maille.evaluate ~limits (Js.to_string sexp) with
  | Maille.Value { ty; value } -> obj [ ("ok", bool true); ("type", str ty); ("value", str value) ]
  | Maille.Failure { kind; line; column; message } ->
      obj [ ("ok", bool false); ("kind", str kind); ("line", int line); ("column", int column); ("message", str message) ]

let () =
  Js.Unsafe.set Js.Unsafe.global (Js.string "MailleInterp")
    (obj [ ("run", Js.Unsafe.inject (Js.wrap_callback run)) ])
