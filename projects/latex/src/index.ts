// LaTeX subset to HTML: parse (located tree and diagnostics), then render (HTML, outline, diagnostics).
import { parse } from './parse.ts';
import { render, type Options, type Rendered } from './render.ts';

export { parse } from './parse.ts';
export { render, escapeHtml } from './render.ts';
export type { Diagnostic, Document, Node, Pos } from './ast.ts';
export type { OutlineEntry, Options, Rendered } from './render.ts';

export function renderLatex(source: string, options?: Options): Rendered {
  return render(parse(source), options);
}
