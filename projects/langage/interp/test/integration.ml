(* Whole chain on the examples: maillec (C, path in MAILLEC) parses NAME.maille, the interpreter types
   and runs the tree, and the result must equal NAME.out (first line). *)

let read file = In_channel.with_open_bin file In_channel.input_all

let parse maillec file =
  let out = Filename.temp_file "maille" ".sexp" in
  let err = Filename.temp_file "maille" ".err" in
  (* quote_command also gets cmd.exe's quoting rules right on Windows. *)
  let code = Sys.command (Filename.quote_command maillec [ file ] ~stdout:out ~stderr:err) in
  let result = if code = 0 then Ok (read out) else Error (String.trim (read err)) in
  Sys.remove out;
  Sys.remove err;
  result

let () =
  let maillec =
    match Sys.getenv_opt "MAILLEC" with
    | Some p when p <> "" -> p
    | _ -> prerr_endline "MAILLEC is not set: give the path to maillec"; exit 2
  in
  let dir = Sys.argv.(1) in
  let examples =
    Sys.readdir dir |> Array.to_list |> List.filter (fun f -> Filename.check_suffix f ".maille") |> List.sort compare
  in
  if examples = [] then (prerr_endline "no examples found"; exit 2);
  let failures = ref 0 in
  List.iter
    (fun name ->
      let file = Filename.concat dir name in
      let expected = String.trim (read (Filename.remove_extension file ^ ".out")) in
      let got =
        match parse maillec file with
        | Error e ->
            (* Keep "line:column: error: message", without the file name. *)
            (match String.index_opt e ':' with Some i when Filename.check_suffix (String.sub e 0 i) ".maille" -> String.sub e (i + 1) (String.length e - i - 1) | _ -> e)
        | Ok sexp -> (match Maille.run sexp with Ok s -> s | Error e -> e)
      in
      if got = expected then Printf.printf "ok   %s\n" name
      else (
        incr failures;
        Printf.printf "FAIL %s\n  expected %s\n  got      %s\n" name expected got))
    examples;
  if !failures > 0 then exit 1
