(* Reader for the S-expressions printed by maillec: atoms, double-quoted strings (escapes for the
   quote, the backslash, newline and tab), and parenthesised lists. *)

type t = Atom of string | Str of string | List of t list

exception Error of string

let parse (s : string) : t =
  let n = String.length s in
  let pos = ref 0 in
  let peek () = if !pos < n then Some s.[!pos] else None in
  let rec skip () =
    match peek () with
    | Some (' ' | '\n' | '\r' | '\t') -> incr pos; skip ()
    | _ -> ()
  in
  let rec value () =
    skip ();
    match peek () with
    | None -> raise (Error "unexpected end of input")
    | Some '(' ->
        incr pos;
        let rec items acc =
          skip ();
          match peek () with
          | Some ')' -> incr pos; List (List.rev acc)
          | None -> raise (Error "unclosed parenthesis")
          | Some _ -> items (value () :: acc)
        in
        items []
    | Some ')' -> raise (Error (Printf.sprintf "unexpected ) at offset %d" !pos))
    | Some '"' ->
        incr pos;
        let b = Buffer.create 16 in
        let rec chars () =
          match peek () with
          | None -> raise (Error "unterminated string")
          | Some '"' -> incr pos
          | Some '\\' ->
              incr pos;
              (match peek () with
               | Some 'n' -> Buffer.add_char b '\n'
               | Some 't' -> Buffer.add_char b '\t'
               | Some (('"' | '\\') as c) -> Buffer.add_char b c
               | _ -> raise (Error "bad escape in string"));
              incr pos;
              chars ()
          | Some c -> Buffer.add_char b c; incr pos; chars ()
        in
        chars ();
        Str (Buffer.contents b)
    | Some _ ->
        let start = !pos in
        let rec atom () =
          match peek () with
          | None | Some (' ' | '\n' | '\r' | '\t' | '(' | ')' | '"') -> ()
          | Some _ -> incr pos; atom ()
        in
        atom ();
        Atom (String.sub s start (!pos - start))
  in
  let v = value () in
  skip ();
  if !pos <> n then raise (Error (Printf.sprintf "trailing text at offset %d" !pos));
  v
