// Parser of a LaTeX subset into a located tree. It never throws: malformed input (unbalanced braces,
// unclosed environments or math) becomes an error diagnostic and the parser carries on, so the editor
// can still show the rest of the document.
import type { Diagnostic, Document, Node, Pos } from './ast.ts';

/** Number of required arguments, and whether an optional [argument] may come first. */
interface Spec {
  args: number;
  optional?: boolean;
}

export const COMMANDS: Record<string, Spec> = {
  documentclass: { args: 1, optional: true },
  usepackage: { args: 1, optional: true },
  title: { args: 1 },
  author: { args: 1 },
  date: { args: 1 },
  maketitle: { args: 0 },
  tableofcontents: { args: 0 },
  section: { args: 1, optional: true },
  subsection: { args: 1, optional: true },
  subsubsection: { args: 1, optional: true },
  paragraph: { args: 1 },
  label: { args: 1 },
  ref: { args: 1 },
  eqref: { args: 1 },
  cite: { args: 1, optional: true },
  footnote: { args: 1 },
  emph: { args: 1 },
  textbf: { args: 1 },
  textit: { args: 1 },
  texttt: { args: 1 },
  textsc: { args: 1 },
  underline: { args: 1 },
  url: { args: 1 },
  href: { args: 2 },
  item: { args: 0, optional: true },
  bibitem: { args: 1, optional: true },
  hline: { args: 0 },
  LaTeX: { args: 0 },
  TeX: { args: 0 },
  today: { args: 0 },
  ldots: { args: 0 },
  dots: { args: 0 },
  noindent: { args: 0 },
  newline: { args: 0 },
  par: { args: 0 },
  bigskip: { args: 0 },
  medskip: { args: 0 },
  smallskip: { args: 0 },
  centering: { args: 0 },
};

/** Environments whose content is kept as source text: math (for KaTeX) and verbatim. */
export const RAW_ENVS = new Set(['equation', 'equation*', 'align', 'align*', 'gather', 'gather*', 'multline', 'multline*', 'verbatim']);

/** Environments that take arguments after \begin{name}. */
const ENV_ARGS: Record<string, number> = { tabular: 1, thebibliography: 1 };

/** Commands made of one non-letter character: \\ \% \& \$ \# \_ \{ \} \, \  and the like. */
const SYMBOLS = new Set(['\\', '%', '&', '$', '#', '_', '{', '}', ',', ' ', ';', ':', '!', '-', '~', '^', '\'', '"', '`', '.', '/', '@']);

/** Deepest nesting of groups and environments accepted (keeps the recursion bounded). */
export const MAX_DEPTH = 200;

export function parse(source: string): Document {
  let i = 0;
  let line = 1;
  let column = 1;
  let depth = 0;
  const diagnostics: Diagnostic[] = [];
  const meta: Document['meta'] = { title: null, author: null, date: null };

  const pos = (): Pos => ({ line, column, offset: i });
  const peek = (k = 0) => source[i + k] ?? '';
  const startsWith = (s: string) => source.startsWith(s, i);
  const error = (message: string, at: Pos) => diagnostics.push({ severity: 'error', message, pos: at });
  const warning = (message: string, at: Pos) => diagnostics.push({ severity: 'warning', message, pos: at });

  function advance(n = 1) {
    for (let k = 0; k < n && i < source.length; k++) {
      // Columns count characters: the second half of a surrogate pair does not move them.
      const code = source.charCodeAt(i);
      if (source[i] === '\n') {
        line++;
        column = 1;
      } else if (code < 0xdc00 || code > 0xdfff) {
        column++;
      }
      i++;
    }
  }

  const skipSpaces = () => {
    while (/[ \t]/.test(peek())) advance();
  };
  // Spaces and at most one newline, as between a command and its argument.
  function skipArgSpace() {
    skipSpaces();
    if (peek() === '\n' && !/^\n[ \t]*\n/.test(source.slice(i, i + 64))) {
      advance();
      skipSpaces();
    }
  }

  function readName(): string {
    const start = i;
    if (/[A-Za-z]/.test(peek())) {
      while (/[A-Za-z]/.test(peek())) advance();
    } else if (peek()) {
      advance();
    }
    return source.slice(start, i);
  }

  /** Text up to the closing `}` of a {name} argument (environment names, labels). */
  function readBraced(what: string): { text: string; ok: boolean } {
    skipArgSpace();
    const at = pos();
    if (peek() !== '{') {
      error(`expected {${what}}`, at);
      return { text: '', ok: false };
    }
    advance();
    const start = i;
    while (i < source.length && peek() !== '}' && peek() !== '\n') advance();
    if (peek() !== '}') {
      error(`unclosed { in {${what}}`, at);
      return { text: source.slice(start, i), ok: false };
    }
    const text = source.slice(start, i).trim();
    advance();
    return { text, ok: true };
  }

  /** Source text up to `end` (not included), which is consumed; null if the input ends first. */
  function readRawUntil(end: string): string | null {
    let k = i;
    while (k < source.length) {
      if (source.startsWith(end, k)) {
        const raw = source.slice(i, k);
        advance(k - i + end.length);
        return raw;
      }
      // In math, an escaped character (\$, \\) never ends the formula.
      k += source[k] === '\\' && end !== '\\end{verbatim}' ? 2 : 1;
    }
    advance(source.length - i);
    return null;
  }

  function math(display: boolean, open: string, close: string, at: Pos): Node {
    advance(open.length);
    const tex = readRawUntil(close);
    if (tex === null) {
      error(`unclosed math: ${open} needs a matching ${close}`, at);
      return { type: 'math', display, tex: '', pos: at };
    }
    return { type: 'math', display, tex, pos: at };
  }

  /** A required argument: a {group}, or else a single token as TeX allows (with a warning). */
  function argument(name: string): Node[] {
    skipArgSpace();
    const at = pos();
    if (peek() === '{') {
      const g = group();
      return g.type === 'group' ? g.children : [g];
    }
    if (i >= source.length || peek() === '}') {
      error(`\\${name} is missing an argument`, at);
      return [];
    }
    warning(`\\${name} takes its argument in braces`, at);
    const one = sequence(null, true);
    return one;
  }

  function optionalArgument(): Node[] | null {
    skipArgSpace();
    if (peek() !== '[') return null;
    const at = pos();
    advance();
    const children = sequence(']');
    if (peek() !== ']') error('unclosed [ (optional argument)', at);
    else advance();
    return children;
  }

  function group(): Node {
    const at = pos();
    advance();
    if (++depth > MAX_DEPTH) {
      error(`groups nested deeper than ${MAX_DEPTH}`, at);
      depth--;
      readRawUntil('}');
      return { type: 'group', children: [], pos: at };
    }
    const children = sequence('}');
    depth--;
    if (peek() === '}') advance();
    else error('unclosed { : missing }', at);
    return { type: 'group', children, pos: at };
  }

  function environment(at: Pos): Node | null {
    const { text: name, ok } = readBraced('environment name');
    if (!ok || !name) return null;
    const args: Node[][] = [];
    for (let k = 0; k < (ENV_ARGS[name] ?? 0); k++) args.push(argument(`begin{${name}}`));
    if (RAW_ENVS.has(name)) {
      if (name !== 'verbatim' && peek() === '\n') advance();
      const raw = readRawUntil(`\\end{${name}}`);
      if (raw === null) error(`\\begin{${name}} is never closed by \\end{${name}}`, at);
      return { type: 'env', name, args, children: [], raw: raw ?? '', pos: at };
    }
    if (++depth > MAX_DEPTH) {
      error(`environments nested deeper than ${MAX_DEPTH}`, at);
      depth--;
      readRawUntil(`\\end{${name}}`);
      return { type: 'env', name, args, children: [], raw: null, pos: at };
    }
    const children = sequence({ env: name, at });
    depth--;
    return { type: 'env', name, args, children, raw: null, pos: at };
  }

  /** The \end{...} at the current position, consumed, with its name; null if none. */
  function tryEnd(): { name: string; at: Pos } | null {
    if (!startsWith('\\end') || /[A-Za-z]/.test(peek(4))) return null;
    const at = pos();
    advance(4);
    const { text } = readBraced('environment name');
    return { name: text, at };
  }

  /**
   * Nodes up to a closing `}` or `]` (not consumed), the \end of an environment (consumed), or the end
   * of the input. With `single`, stops after one node (a bare command argument).
   */
  function sequence(until: '}' | ']' | { env: string; at: Pos } | null, single = false): Node[] {
    const nodes: Node[] = [];
    let text = '';
    let textPos = pos();
    const flush = () => {
      if (text) nodes.push({ type: 'text', value: text, pos: textPos });
      text = '';
    };
    const push = (n: Node) => {
      flush();
      nodes.push(n);
    };

    while (i < source.length) {
      const c = peek();
      if (until === '}' && c === '}') break;
      if (until === ']' && c === ']') break;
      if (!text) textPos = pos();

      if (c === '%') {
        // A comment runs to the end of the line, and eats that newline and the next line's indentation.
        while (i < source.length && peek() !== '\n') advance();
        if (peek() === '\n' && !/^\n[ \t]*\n/.test(source.slice(i, i + 64))) {
          advance();
          skipSpaces();
        }
        continue;
      }
      if (c === '\n' && /^\n[ \t]*\n/.test(source.slice(i, i + 64))) {
        const at = pos();
        while (/\s/.test(peek())) advance();
        push({ type: 'parbreak', pos: at });
        continue;
      }
      if (c === '{') {
        push(group());
        if (single) break;
        continue;
      }
      if (c === '}') {
        error('unmatched }', pos());
        advance();
        continue;
      }
      if (c === '$') {
        const at = pos();
        push(peek(1) === '$' ? math(true, '$$', '$$', at) : math(false, '$', '$', at));
        if (single) break;
        continue;
      }
      if (c === '&') {
        push({ type: 'align', pos: pos() });
        advance();
        continue;
      }
      if (c === '\\') {
        const at = pos();
        if (peek(1) === '[' || peek(1) === '(') {
          const display = peek(1) === '[';
          push(math(display, display ? '\\[' : '\\(', display ? '\\]' : '\\)', at));
          if (single) break;
          continue;
        }
        const end = tryEnd();
        if (end) {
          if (typeof until === 'object' && until !== null && end.name === until.env) {
            flush();
            return nodes;
          }
          if (typeof until === 'object' && until !== null) {
            error(`\\end{${end.name}} does not close \\begin{${until.env}} (line ${until.at.line})`, end.at);
          } else {
            error(`\\end{${end.name}} without a matching \\begin`, end.at);
          }
          continue;
        }
        advance();
        const name = readName();
        if (name === 'begin') {
          const env = environment(at);
          if (env) push(env);
          if (single) break;
          continue;
        }
        const star = /[A-Za-z]/.test(name) && peek() === '*';
        if (star) advance();
        const spec = COMMANDS[name];
        const optional = spec?.optional ? optionalArgument() : null;
        const args: Node[][] = [];
        for (let k = 0; k < (spec?.args ?? 0); k++) args.push(argument(name));
        const node: Node = { type: 'command', name, star, optional, args, pos: at };
        if (name === 'title' || name === 'author' || name === 'date') meta[name] = args[0] ?? [];
        else push(node);
        // A command made of letters swallows the spaces after it, as in TeX.
        if (/^[A-Za-z]+$/.test(name) && (spec?.args ?? 0) === 0 && !optional) skipSpaces();
        const letters = /^[A-Za-z]+$/.test(name);
        if ((letters && !spec) || (!letters && !SYMBOLS.has(name))) warning(`unknown command \\${name}`, at);
        if (single) break;
        continue;
      }
      if (c === '#' || c === '^' || c === '_') {
        warning(`${c} outside math: write \\${c} for the character, or put it between $ $`, pos());
      }
      if (!text) textPos = pos();
      text += c;
      advance();
      if (single && /\S/.test(c)) break;
    }
    flush();
    if (typeof until === 'object' && until !== null) {
      error(`\\begin{${until.env}} is never closed by \\end{${until.env}}`, until.at);
    }
    return nodes;
  }

  // \begin{document} splits the preamble from the body; without it, the whole source is the body.
  let body = sequence(null);
  const docIndex = body.findIndex((n) => n.type === 'env' && n.name === 'document');
  if (docIndex >= 0) {
    const doc = body[docIndex] as Extract<Node, { type: 'env' }>;
    for (const n of body.slice(0, docIndex)) {
      if (n.type === 'text' && n.value.trim()) warning('text before \\begin{document} is ignored', n.pos);
    }
    for (const n of body.slice(docIndex + 1)) {
      if ((n.type === 'text' && n.value.trim()) || n.type === 'command' || n.type === 'env' || n.type === 'math') {
        warning('content after \\end{document} is ignored', n.pos);
        break;
      }
    }
    body = doc.children;
  }
  return { meta, body, diagnostics };
}
