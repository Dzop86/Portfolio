// Integration test of Maille's web build: the WebAssembly parser and the js_of_ocaml interpreter
// committed under src/assets/wasm must give, on every example, the result the native chain gives
// (projects/langage/examples/*.out).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { createRequire } from 'node:module';
import vm from 'node:vm';
import { ROOT } from '../../src/lib.mjs';
import { loadParser, parse, readTree, runProgram, MAX_SOURCE } from '../../src/assets/maille-api.js';

const lib = await loadParser((await import('../../src/assets/wasm/maillec.js')).default);
// js_of_ocaml's runtime calls require when it detects Node; an ES module has none, so it is passed in.
vm.runInThisContext(`(function (require) {${readFileSync(join(ROOT, 'src/assets/wasm/maille-interp.js'), 'utf8')}\n})`)(
  createRequire(import.meta.url));
const interp = globalThis.MailleInterp;
const examples = join(ROOT, 'projects/langage/examples');

// Same spelling as the command line: "- : type = value" or "line:column: kind: message".
function asCommandLine(r) {
  if (r.stage === 'value') return `- : ${r.type} = ${r.value}`;
  return `${r.line}:${r.column}: ${r.stage === 'syntax' ? 'error' : r.stage}: ${r.message}`;
}

const files = readdirSync(examples).filter((f) => f.endsWith('.maille')).sort();

test('there are examples to compare', () => assert.ok(files.length >= 10));

for (const file of files) {
  test(`web build gives the native result on ${file}`, () => {
    const source = readFileSync(join(examples, file), 'utf8');
    const expected = readFileSync(join(examples, file.replace(/\.maille$/, '.out')), 'utf8').trim();
    assert.equal(asCommandLine(runProgram(lib, interp, source)), expected);
  });
}

test('the tree is read back from the S-expression, positions and labels included', () => {
  const tree = readTree(parse(lib, 'let x = 1 in x + "a\\"b"').sexp);
  assert.deepEqual([tree.kind, tree.line, tree.column, tree.label], ['let', 1, 1, 'x']);
  const [value, body] = tree.children;
  assert.deepEqual([value.kind, value.label], ['int', '1']);
  assert.deepEqual([body.kind, body.label, body.column], ['binop', '+', 16]);
  assert.equal(body.children[1].label, '"a\\"b"');
  assert.throws(() => readTree('(int 1:1 1'), /unclosed/);
});

test('syntax errors come back located, with no tree', () => {
  const r = runProgram(lib, interp, 'let x = 1 in\n  x +');
  assert.deepEqual([r.stage, r.line, r.column], ['syntax', 2, 6]);
  assert.match(r.message, /unexpected end of file/);
  assert.equal(r.tree, undefined);
});

test('type and runtime errors keep the tree for display', () => {
  const t = runProgram(lib, interp, '1 + true');
  assert.deepEqual([t.stage, t.column, t.tree.kind], ['type error', 5, 'binop']);
  const loop = runProgram(lib, interp, 'let rec f x = f x in f 0');
  assert.equal(loop.stage, 'runtime error');
  assert.match(loop.message, /recursion deeper than 5000 calls/);
});

test('an endless loop that does not nest stops on the step budget', () => {
  const r = runProgram(lib, interp, 'let rec count n = if n == 0 then 0 else count (n - 1) + 0 * 0 in count 3000000');
  assert.equal(r.stage, 'runtime error');
});

test('a program over the size limit is refused before WebAssembly', () => {
  const r = parse(lib, `1${' + 1'.repeat(MAX_SOURCE / 4)}`);
  assert.deepEqual([r.ok, r.tooLarge], [false, true]);
});

test('the parser frees its memory: 500 parses with errors do not grow the heap', () => {
  parse(lib, `# ${'x'.repeat(30000)}\n1`);
  const before = lib.HEAPU8.length;
  for (let k = 0; k < 500; k++) {
    parse(lib, 'let x = in 3');
    parse(lib, 'let f x = x + 1 in f 41');
  }
  assert.equal(lib.HEAPU8.length, before);
});
