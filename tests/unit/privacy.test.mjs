// Fails the build if private data reaches the published site.
// Generic patterns only, so this file never contains the data it protects.
// Extra terms can be supplied through the PRIVATE_TERMS secret (comma-separated).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readdirSync, readFileSync, statSync } from 'node:fs';
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

const extraTerms = (process.env.PRIVATE_TERMS || '').split(',').map((s) => s.trim().toLowerCase()).filter(Boolean);

function files(dir) {
  return readdirSync(dir).flatMap((f) => {
    const p = join(dir, f);
    return statSync(p).isDirectory() ? files(p) : [p];
  });
}

const dist = build(mkdtempSync(join(tmpdir(), 'portfolio-')));
const published = files(dist).filter((f) => /\.(html|js|css|json|webmanifest|svg)$/.test(f));
// Technical projects are scanned too: sources, tests, data and docs, but not their build output.
const projectFiles = existsSync(join(ROOT, 'projects'))
  ? files(join(ROOT, 'projects')).filter((f) => !/[\\/](build|_deps|node_modules)[\\/]/.test(f))
  : [];
const sources = [join(ROOT, 'data/cv.json'), join(ROOT, 'data/projects.json'), ...projectFiles];

test('the scan covers the technical projects', () => {
  assert.ok(sources.some((f) => f.endsWith(join('lib-c', 'src', 'obj.c'))), 'projects/lib-c sources');
  assert.ok(!sources.some((f) => /[\\/]build[\\/]/.test(f)), 'build output excluded');
});

for (const file of [...published, ...sources]) {
  test(`no private data in ${file.replace(ROOT, '').replace(dist, 'dist')}`, () => {
    const text = readFileSync(file, 'utf8');
    for (const { name, re } of PATTERNS) {
      const m = text.match(re);
      assert.equal(m, null, `${name} found: "${m?.[0]}"`);
    }
    const lower = text.toLowerCase();
    for (const term of extraTerms) {
      assert.ok(!lower.includes(term), 'a PRIVATE_TERMS entry was found');
    }
  });
}

test('the patterns do catch private data', () => {
  const sample = 'Tél. 01.23.45.67.89, mail jean@exemple.fr, né le 01/02/1990, 12 rue des Lilas, app. 4';
  for (const { name, re } of PATTERNS.filter((p) => p.name !== 'international phone number')) {
    assert.match(sample, re, name);
  }
});
