// Syntax tree of a LaTeX document. Every node records where it starts (1-based line and column, and
// the 0-based offset in the source), so diagnostics and the editor can point at it.

export interface Pos {
  line: number;
  column: number;
  offset: number;
}

export type Node =
  | { type: 'text'; value: string; pos: Pos }
  /** A command such as \section*[short]{Title}: optional argument and required arguments, parsed. */
  | { type: 'command'; name: string; star: boolean; optional: Node[] | null; args: Node[][]; pos: Pos }
  | { type: 'group'; children: Node[]; pos: Pos }
  /** \begin{name}...\end{name}; `raw` holds the source of verbatim and math environments. */
  | { type: 'env'; name: string; args: Node[][]; children: Node[]; raw: string | null; pos: Pos }
  | { type: 'math'; display: boolean; tex: string; pos: Pos }
  /** & in a table. */
  | { type: 'align'; pos: Pos }
  /** A blank line: ends a paragraph. */
  | { type: 'parbreak'; pos: Pos };

export interface Diagnostic {
  severity: 'error' | 'warning';
  message: string;
  pos: Pos;
}

export interface Document {
  /** \title, \author, \date from the preamble (or the body), as parsed nodes. */
  meta: { title: Node[] | null; author: Node[] | null; date: Node[] | null };
  body: Node[];
  diagnostics: Diagnostic[];
}
