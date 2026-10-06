(* maille: types and evaluates the syntax tree printed by maillec.
     maillec program.maille | maille -        (or: maille tree.sexp)
   Prints "- : type = value"; on a type or runtime error, prints "line:column: ..." and exits with 1. *)

let () =
  let input =
    match Sys.argv with
    | [| _; "-" |] -> In_channel.input_all stdin
    | [| _; file |] -> In_channel.with_open_bin file In_channel.input_all
    | _ ->
        prerr_endline "usage: maille <tree.sexp | ->";
        exit 2
  in
  match Maille.run input with
  | Maille.Ok s -> print_endline s
  | Maille.Error m ->
      prerr_endline m;
      exit 1
