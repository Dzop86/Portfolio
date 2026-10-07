// Fails the build if private data reaches the published site.
// Generic patterns only, so this file never contains the data it protects.
// Extra terms can be supplied through the PRIVATE_TERMS secret (comma-separated).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';
import { ROOT } from '../../src/lib.mjs';

const PATTERNS = [
  { name: 'French phone number', re: /(?<![\d.])0[1-9](?:[ .-]?\d{2}){4}(?![\d.])/ },
  { name: 'international phone number', re: /\+33[ .]?[1-9](?:[ .-]?\d{2}){4}/ },
  { name: 'e-mail address', re: /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/ },
  { name: 'date of birth', re: /(?:^|[\s>])(?:née?|born)\s[^<]{0,20}\d{1,2}[/ .-]/i },
  { name: 'street address', re: /\b\d{1,4},?\s+(?:rue|route|avenue|boulevard|chemin|allée)\b/i },
  { name: 'flat number', re: /\b(?:appartement|app\.)\s*\d+/i },
];

// Exact matches that are not private data: the digit string of number formatting code (js_of_ocaml's
// runtime has "0123456789abcdef"), which looks like a phone number; the public address of a package
// maintainer that npm copies into package-lock.json with a deprecation notice (glob 10, pulled by the
// Angular tools). Anything else still fails.
const ALLOWED = ['0123456789', 'i@izs.me'];

const extraTerms = (process.env.PRIVATE_TERMS || '').split(',').map((s) => s.trim().toLowerCase()).filter(Boolean);

function files(dir) {
  return readdirSync(dir).flatMap((f) => {
    const p = join(dir, f);
    return statSync(p).isDirectory() ? files(p) : [p];
  });
}

const dist = build(mkdtempSync(join(tmpdir(), 'portfolio-')));
const published = files(dist).filter((f) => /\.(html|js|css|json|webmanifest|svg)$/.test(f));
// Technical projects are scanned too: every file git tracks under projects/ (sources, tests, data, docs).
// Local build output (build*/, bin/, obj/, alire/...) is ignored by git, so it is left out by construction.
const projectFiles = execFileSync('git', ['ls-files', '-z', 'projects'], { cwd: ROOT, encoding: 'utf8' })
  .split('\0').filter(Boolean).map((f) => join(ROOT, f));
const sources = [join(ROOT, 'data/cv.json'), join(ROOT, 'data/projects.json'), ...projectFiles];

test('the scan covers the technical projects', () => {
  assert.ok(sources.some((f) => f.endsWith(join('lib-c', 'src', 'obj.c'))), 'projects/lib-c sources');
  // Build output: CMake's build*/, dune's _build/, Alire's bin/ and obj/ for Ada. A dune bin/ holds sources.
  assert.ok(!sources.some((f) => /[\\/](build[^\\/]*|_build)[\\/]|[\\/]ada[\\/](bin|obj)[\\/]/.test(f)), 'build output excluded');
  assert.ok(sources.some((f) => f.endsWith(join('langage', 'interp', 'bin', 'main.ml'))), 'projects/langage sources');
  assert.ok(sources.some((f) => f.endsWith(join('ada', 'src', 'traffic.adb'))), 'projects/ada sources');
});

for (const file of [...published, ...sources]) {
  test(`no private data in ${file.replace(ROOT, '').replace(dist, 'dist')}`, () => {
    const text = readFileSync(file, 'utf8');
    for (const { name, re } of PATTERNS) {
      const m = [...text.matchAll(new RegExp(re.source, `${re.flags}g`))].find((x) => !ALLOWED.includes(x[0]));
      assert.equal(m, undefined, `${name} found: "${m?.[0]}"`);
    }
    const lower = text.toLowerCase();
    for (const term of extraTerms) {
      assert.ok(!lower.includes(term), 'a PRIVATE_TERMS entry was found');
    }
  });
}

test('the allow list is exact: a phone number next to it is still caught', () => {
  const phone = PATTERNS[0].re;
  const hits = [...'h="0123456789abcdef"; tel 01 23 45 67 89'.matchAll(new RegExp(phone.source, 'g'))]
    .map((x) => x[0]).filter((x) => !ALLOWED.includes(x));
  assert.deepEqual(hits, ['01 23 45 67 89']);
});

test('the patterns do catch private data', () => {
  const sample = 'Tél. 01.23.45.67.89, mail jean@exemple.fr, né le 01/02/1990, 12 rue des Lilas, app. 4';
  for (const { name, re } of PATTERNS.filter((p) => p.name !== 'international phone number')) {
    assert.match(sample, re, name);
  }
});
