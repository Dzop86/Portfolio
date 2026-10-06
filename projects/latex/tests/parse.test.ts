// Unit tests of the parser: tree shape, positions, arguments, environments, math, and recovery from
// malformed input (the parser must never throw).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { parse } from '../src/parse.ts';
import type { Node } from '../src/ast.ts';

const errors = (src: string) => parse(src).diagnostics.filter((d) => d.severity === 'error')
  .map((d) => `${d.pos.line}:${d.pos.column} ${d.message}`);
const warnings = (src: string) => parse(src).diagnostics.filter((d) => d.severity === 'warning')
  .map((d) => `${d.pos.line}:${d.pos.column} ${d.message}`);
/** Compact shape of nodes: text as "quoted", commands as \name{...}, envs as [name ...], math as $..$. */
function shape(nodes: Node[]): string {
  return nodes.map((n) => {
    switch (n.type) {
      case 'text': return JSON.stringify(n.value);
      case 'group': return `{${shape(n.children)}}`;
      case 'command': return `\\${n.name}${n.star ? '*' : ''}${n.optional ? `[${shape(n.optional)}]` : ''}${n.args.map((a) => `{${shape(a)}}`).join('')}`;
      case 'env': return `[${n.name}${n.raw !== null ? ` raw=${JSON.stringify(n.raw)}` : ` ${shape(n.children)}`}]`;
      case 'math': return n.display ? `$$${n.tex}$$` : `$${n.tex}$`;
      case 'align': return '&';
      case 'parbreak': return '¶';
    }
  }).join(' ');
}
const body = (src: string) => shape(parse(src).body);

test('text, commands with their arguments, groups', () => {
  assert.equal(body('Hello \\emph{world}!'), '"Hello " \\emph{"world"} "!"');
  assert.equal(body('\\section*{Intro}'), '\\section*{"Intro"}');
  assert.equal(body('\\section[Short]{Long title}'), '\\section[“Short”]{"Long title"}'.replace('“Short”', '"Short"'));
  assert.equal(body('\\href{https://x.org}{site}'), '\\href{"https://x.org"}{"site"}');
  assert.equal(body('{a {b}}'), '{"a " {"b"}}');
  assert.equal(body('\\emph x y'), '\\emph{"x"} " y"');
});

test('a command made of letters swallows the following spaces', () => {
  assert.equal(body('\\LaTeX   is'), '\\LaTeX "is"');
  assert.equal(body('a\\,b'), '"a" \\, "b"');
});

test('comments vanish with their newline; blank lines break paragraphs', () => {
  assert.equal(body('a % note\n  b'), '"a b"');
  assert.equal(body('a\n\n\n  b'), '"a" ¶ "b"');
  assert.equal(body('a\nb'), '"a\\nb"');
});

test('math: inline and display, escaped dollars inside', () => {
  assert.equal(body('$x^2$ and $$y$$'), '$x^2$ " and " $$y$$');
  assert.equal(body('\\(a\\) \\[b\\]'), '$a$ " " $$b$$');
  assert.equal(body('$\\$5$'), '$\\$5$');
});

test('environments: nested, raw for math and verbatim, arguments for tabular', () => {
  assert.equal(body('\\begin{itemize}\\item a \\item[b] c\\end{itemize}'),
    '[itemize \\item "a " \\item["b"] " c"]');
  assert.equal(body('\\begin{equation}\n  E = mc^2 \\label{e}\n\\end{equation}'), '[equation raw="  E = mc^2 \\\\label{e}\\n"]');
  assert.equal(body('\\begin{verbatim}\\emph{x} % y\\end{verbatim}'), '[verbatim raw="\\\\emph{x} % y"]');
  assert.equal(body('\\begin{tabular}{l|r} a & b \\\\ \\hline\\end{tabular}'),
    '[tabular " a " & " b " \\\\ " " \\hline]');
  const table = parse('\\begin{tabular}{l|r}x\\end{tabular}').body[0] as Extract<Node, { type: 'env' }>;
  assert.equal(shape(table.args[0] ?? []), '"l|r"');
});

test('the preamble is split from the body; title, author and date are kept', () => {
  const doc = parse('\\documentclass{article}\n\\title{T}\\author{A}\n\\begin{document}\nBody\n\\end{document}\n');
  assert.equal(shape(doc.body), '"\\nBody\\n"');
  assert.equal(shape(doc.meta.title ?? []), '"T"');
  assert.equal(shape(doc.meta.author ?? []), '"A"');
  assert.deepEqual(doc.diagnostics, []);
});

test('every node knows its line and column (characters, not bytes or UTF-16 units)', () => {
  const doc = parse('ab\n  \\emph{c}\n€😀 $x$');
  const emph = doc.body.find((n) => n.type === 'command');
  assert.deepEqual([emph?.pos.line, emph?.pos.column], [2, 3]);
  const m = doc.body.find((n) => n.type === 'math');
  assert.deepEqual([m?.pos.line, m?.pos.column], [3, 4]);
});

test('malformed input becomes located errors, and the rest is still parsed', () => {
  assert.deepEqual(errors('a {b'), ['1:3 unclosed { : missing }']);
  assert.deepEqual(errors('a } b'), ['1:3 unmatched }']);
  assert.deepEqual(errors('x $y'), ['1:3 unclosed math: $ needs a matching $']);
  assert.deepEqual(errors('\\begin{itemize}\n\\item a'), ['1:1 \\begin{itemize} is never closed by \\end{itemize}']);
  assert.deepEqual(errors('\\begin{quote}\n\\end{center}\n\\end{quote}'), ['2:1 \\end{center} does not close \\begin{quote} (line 1)']);
  assert.deepEqual(errors('\\end{quote}'), ['1:1 \\end{quote} without a matching \\begin']);
  assert.deepEqual(errors('\\begin{equation} x'), ['1:1 \\begin{equation} is never closed by \\end{equation}']);
  assert.deepEqual(errors('\\emph'), ['1:6 \\emph is missing an argument']);
  // The title went into the unclosed optional argument, so the required one is missing too.
  assert.deepEqual(errors('\\section[x{Title}'), ['1:9 unclosed [ (optional argument)', '1:18 \\section is missing an argument']);
  // After an error, the rest of the document is still there.
  assert.match(body('a } \\textbf{b}'), /\\textbf\{"b"\}/);
});

test('warnings: unknown commands, characters that need math, braces-less arguments, ignored text', () => {
  assert.deepEqual(warnings('\\foo{x}'), ['1:1 unknown command \\foo']);
  assert.deepEqual(warnings('a_b'), ['1:2 _ outside math: write \\_ for the character, or put it between $ $']);
  assert.deepEqual(warnings('\\emph x'), ['1:7 \\emph takes its argument in braces']);
  assert.deepEqual(warnings('stray\n\\begin{document}a\\end{document}'), ['1:1 text before \\begin{document} is ignored']);
  assert.deepEqual(warnings('a\\_b \\% \\{ \\}'), []);
});

test('nesting is bounded: 500 nested groups give an error, not a stack overflow', () => {
  const deep = `${'{'.repeat(500)}x${'}'.repeat(500)}`;
  assert.ok(errors(deep).some((e) => /nested deeper than 200/.test(e)));
});

test('the parser never throws, whatever the input', () => {
  const pieces = ['\\', '{', '}', '$', '$$', '\\begin{', '\\end{', 'itemize}', '[', ']', '%', '\n\n', '&', '\\[', '\\]', 'a', '\\emph', '\\item'];
  let seed = 7;
  const random = () => (seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648;
  for (let k = 0; k < 2000; k++) {
    const src = Array.from({ length: 1 + Math.floor(random() * 30) }, () => pieces[Math.floor(random() * pieces.length)]).join('');
    assert.doesNotThrow(() => parse(src), src);
  }
});
