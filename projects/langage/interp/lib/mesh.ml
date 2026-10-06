(* Triangle meshes for Maille: the synthetic families of the portfolio (same construction as
   projects/sql/bench/meshes.mjs, 4 k^2 triangles at resolution k) and their topological invariants. *)

type t = { positions : float array;  (** x y z per vertex *) triangles : int array  (** a b c per triangle *) }

(* Resolutions accepted by the constructors: 3 to 256, so 36 to 262 144 triangles. *)
let min_resolution = 3
let max_resolution = 256

let vertex_count m = Array.length m.positions / 3
let triangle_count m = Array.length m.triangles / 3

(* A grid of n columns that wrap around, by m rows; rows wrap too when [closed]. *)
let grid n m closed point =
  let positions = Array.make (3 * n * m) 0. in
  for i = 0 to n - 1 do
    for j = 0 to m - 1 do
      let x, y, z = point i j in
      let k = 3 * ((i * m) + j) in
      positions.(k) <- x;
      positions.(k + 1) <- y;
      positions.(k + 2) <- z
    done
  done;
  let bands = if closed then m else m - 1 in
  let idx i j = (i mod n * m) + (j mod m) in
  let triangles = Array.make (6 * n * bands) 0 in
  let t = ref 0 in
  for i = 0 to n - 1 do
    for j = 0 to bands - 1 do
      let a = idx i j and b = idx (i + 1) j and c = idx (i + 1) (j + 1) and d = idx i (j + 1) in
      Array.blit [| a; b; c; a; c; d |] 0 triangles !t 6;
      t := !t + 6
    done
  done;
  { positions; triangles }

let pi = Float.pi

let torus k =
  let n = 2 * k and m = k in
  grid n m true (fun i j ->
      let u = 2. *. pi *. float i /. float n and v = 2. *. pi *. float j /. float m in
      let r = 1. +. (0.4 *. cos v) in
      (r *. cos u, r *. sin u, 0.4 *. sin v))

let cylinder k =
  let n = 2 * k and m = k + 1 in
  grid n m false (fun i j ->
      let u = 2. *. pi *. float i /. float n in
      (cos u, sin u, (2. *. float j /. float (m - 1)) -. 1.))

(* Rings strictly between the poles, closed by a fan at each pole. *)
let sphere k =
  let n = 2 * k and rings = k in
  let band =
    grid n rings false (fun i j ->
        let u = 2. *. pi *. float i /. float n and v = pi *. float (j + 1) /. float (rings + 1) in
        (sin v *. cos u, sin v *. sin u, cos v))
  in
  let nv = n * rings in
  let positions = Array.append band.positions [| 0.; 0.; 1.; 0.; 0.; -1. |] in
  let fans =
    Array.concat
      (List.init n (fun i ->
           let i1 = (i + 1) mod n in
           [| nv; i1 * rings; i * rings; nv + 1; (i * rings) + rings - 1; (i1 * rings) + rings - 1 |]))
  in
  { positions; triangles = Array.append band.triangles fans }

(* Disjoint union: the second mesh's indices move past the first mesh's vertices. *)
let union a b =
  let shift = vertex_count a in
  { positions = Array.append a.positions b.positions; triangles = Array.append a.triangles (Array.map (( + ) shift) b.triangles) }

(* Union-find over 0..n-1, with path halving. *)
let find parent x =
  let x = ref x in
  while parent.(!x) <> !x do
    parent.(!x) <- parent.(parent.(!x));
    x := parent.(!x)
  done;
  !x

let merge parent a b =
  let ra = find parent a and rb = find parent b in
  if ra <> rb then parent.(ra) <- rb

type invariants = {
  vertices : int;
  edges : int;
  faces : int;
  boundary_edges : int;
  boundary_loops : int;
  components : int;
  euler : int;
  genus : int;  (** of an orientable surface: chi = 2c - 2g - b *)
}

let invariants m =
  let nv = vertex_count m and nt = triangle_count m in
  let uses = Hashtbl.create (3 * nt) in
  let parent = Array.init nv Fun.id in
  for t = 0 to nt - 1 do
    let corner c = m.triangles.((3 * t) + c) in
    for c = 0 to 2 do
      let a = corner c and b = corner ((c + 1) mod 3) in
      let key = (min a b, max a b) in
      Hashtbl.replace uses key (1 + Option.value ~default:0 (Hashtbl.find_opt uses key));
      merge parent a b
    done
  done;
  let edges = Hashtbl.length uses in
  (* Boundary edges (used by one triangle) link into loops; a separate union-find counts them. *)
  let loop_parent = Array.init nv Fun.id in
  let on_boundary = Array.make nv false in
  let boundary_edges = ref 0 in
  Hashtbl.iter
    (fun (a, b) n ->
      if n = 1 then (
        incr boundary_edges;
        on_boundary.(a) <- true;
        on_boundary.(b) <- true;
        merge loop_parent a b))
    uses;
  let count_roots keep par = Seq.fold_left (fun acc v -> if keep v && find par v = v then acc + 1 else acc) 0 (Seq.init nv Fun.id) in
  let used = Array.make nv false in
  Array.iter (fun v -> used.(v) <- true) m.triangles;
  let components = count_roots (fun v -> used.(v)) parent in
  let boundary_loops = count_roots (fun v -> on_boundary.(v)) loop_parent in
  let euler = nv - edges + nt in
  {
    vertices = nv;
    edges;
    faces = nt;
    boundary_edges = !boundary_edges;
    boundary_loops;
    components;
    euler;
    genus = ((2 * components) - euler - boundary_loops) / 2;
  }

let to_string m = Printf.sprintf "<mesh: %d vertices, %d triangles>" (vertex_count m) (triangle_count m)
