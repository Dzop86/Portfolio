// Maille in the browser: the C parser compiled to WebAssembly (wasm/maillec.js) gives the syntax tree
// as an S-expression, the OCaml interpreter compiled by js_of_ocaml (wasm/maille-interp.js, which sets
// globalThis.MailleInterp) types and runs it. Shared by the demo and the Node tests.

/** Programs above this size are refused before reaching WebAssembly. */
export const MAX_SOURCE = 64 * 1024;

export function loadParser(createMaillec, options = {}) {
  return createMaillec(options);
}

/** Parses source text: { ok: true, sexp } or { ok: false, line, column, message }. */
export function parse(lib, source) {
  const bytes = new TextEncoder().encode(source);
  if (bytes.byteLength > MAX_SOURCE) return { ok: false, line: 1, column: 1, message: 'program too large', tooLarge: true };
  const ptr = lib._malloc(Math.max(bytes.byteLength, 1));
  if (!ptr) return { ok: false, line: 1, column: 1, message: 'out of memory' };
  try {
    lib.HEAPU8.set(bytes, ptr);
    const status = lib._maillejs_parse(ptr, bytes.byteLength);
    if (status !== 0) {
      return { ok: false, line: lib._maillejs_line(), column: lib._maillejs_column(), message: lib.UTF8ToString(lib._maillejs_message()) };
    }
    return { ok: true, sexp: lib.UTF8ToString(lib._maillejs_tree()) };
  } finally {
    lib._free(ptr);
  }
}

/**
 * Reads maillec's S-expression into nodes { kind, line, column, label, children } for display.
 * Atoms after the position are the node's label (operator, name, literal); lists are children.
 */
export function readTree(sexp) {
  let i = 0;
  const skip = () => { while (i < sexp.length && /\s/.test(sexp[i])) i++; };
  function atom() {
    if (sexp[i] === '"') {
      let s = '"';
      i++;
      while (i < sexp.length && sexp[i] !== '"') {
        if (sexp[i] === '\\') s += sexp[i++];
        s += sexp[i++];
      }
      i++;
      return `${s}"`;
    }
    const start = i;
    while (i < sexp.length && !/[\s()]/.test(sexp[i])) i++;
    return sexp.slice(start, i);
  }
  function node() {
    skip();
    if (sexp[i] !== '(') throw new Error(`expected ( at ${i}`);
    i++;
    const kind = atom();
    skip();
    const [line, column] = atom().split(':').map(Number);
    const label = [];
    const children = [];
    for (skip(); sexp[i] !== ')'; skip()) {
      if (i >= sexp.length) throw new Error('unclosed (');
      if (sexp[i] === '(') children.push(node());
      else label.push(atom());
    }
    i++;
    return { kind, line, column, label: label.join(' '), children };
  }
  const tree = node();
  skip();
  if (i !== sexp.length) throw new Error('trailing text');
  return tree;
}

/** Parses then runs: { stage: 'syntax' | 'type error' | 'runtime error' | 'value', ... }. */
export function runProgram(lib, interp, source) {
  const parsed = parse(lib, source);
  if (!parsed.ok) return { stage: 'syntax', line: parsed.line, column: parsed.column, message: parsed.message };
  let r;
  try {
    r = interp.run(parsed.sexp);
  } catch (e) {
    // The budgets should stop a program first; a JavaScript stack overflow is reported the same way.
    if (!(e instanceof RangeError)) throw e;
    r = { ok: false, kind: 'runtime error', line: 1, column: 1, message: 'recursion too deep for the browser' };
  }
  const tree = readTree(parsed.sexp);
  if (r.ok) return { stage: 'value', type: r.type, value: r.value, tree, sexp: parsed.sexp };
  return { stage: r.kind, line: r.line, column: r.column, message: r.message, tree, sexp: parsed.sexp };
}
