// The site's Docker image builds from the files its Dockerfile copies, nothing else. This test copies
// exactly those files into an empty folder and runs the build there, so a page that starts reading a new
// file from projects/ fails here, not in the CI's Docker job (it happened twice).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, symlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, relative, sep } from 'node:path';
import { ROOT } from '../../src/lib.mjs';

/** COPY instructions of the build stage: [[sources...], destination]. */
function copies() {
  const dockerfile = readFileSync(join(ROOT, 'Dockerfile'), 'utf8');
  // The stage named build (the dashboard has its own stage before it, D43).
  const buildStage = dockerfile.split(/^FROM /m).find((stage) => /\bAS build\b/.test(stage.split('\n')[0]));
  return [...buildStage.matchAll(/^COPY\s+(?!--from)(.+)$/gm)].map((m) => {
    const parts = m[1].trim().split(/\s+/);
    return [parts.slice(0, -1), parts.at(-1)];
  });
}

test('the Dockerfile copies something', () => {
  assert.ok(copies().length > 5);
});

test('the site builds from exactly what the Dockerfile copies', () => {
  const app = mkdtempSync(join(tmpdir(), 'docker-context-'));
  for (const [sources, dest] of copies()) {
    for (const src of sources) {
      const target = dest.endsWith('/') ? join(app, dest, src.split('/').at(-1)) : join(app, dest);
      mkdirSync(join(target, '..'), { recursive: true });
      cpSync(join(ROOT, src), target, { recursive: true });
    }
  }
  // npm ci in the image: here the repository's own node_modules stand in for it.
  symlinkSync(join(ROOT, 'node_modules'), join(app, 'node_modules'), 'junction');
  try {
    execFileSync(process.execPath, ['-e', "import('./src/build.mjs').then((m) => m.build())"], { cwd: app, stdio: 'pipe' });
  } catch (e) {
    const reason = String(e.stderr).split('\n').find((l) => /ENOENT|Error/.test(l)) ?? String(e.stderr).slice(0, 300);
    assert.fail(`the build needs a file the Dockerfile does not copy: ${reason}`);
  }
});

test('the dashboard stage copies every file of the site that the dashboard imports', () => {
  const dockerfile = readFileSync(join(ROOT, 'Dockerfile'), 'utf8');
  const stage = dockerfile.split(/^FROM /m).find((s) => /\bAS dashboard\b/.test(s.split('\n')[0]));
  assert.ok(stage, 'a dashboard stage');
  const copied = [...stage.matchAll(/^COPY\s+(?!--from)(.+)$/gm)].flatMap((m) => m[1].trim().split(/\s+/).slice(0, -1));
  // Imports that leave projects/react for the site's src/ (tokens.css, topo-api.js...), and theirs in turn.
  const wanted = new Set();
  const visit = (file) => {
    for (const [, spec] of readFileSync(file, 'utf8').matchAll(/(?:import|from)\s*['"]([^'"]+)['"]/g)) {
      if (!spec.startsWith('.')) continue;
      const target = join(dirname(file), spec);
      const rel = relative(ROOT, target).split(sep).join('/');
      if (rel.startsWith('src/') && !wanted.has(rel)) {
        wanted.add(rel);
        if (rel.endsWith('.js')) visit(target);
      }
    }
  };
  const walk = (dir) => readdirSync(dir, { withFileTypes: true }).forEach((e) =>
    e.isDirectory() ? walk(join(dir, e.name)) : /\.(ts|tsx)$/.test(e.name) && visit(join(dir, e.name)));
  walk(join(ROOT, 'projects/react/src'));
  assert.ok(wanted.has('src/assets/tokens.css'));
  for (const file of wanted) assert.ok(copied.includes(file), `the dashboard stage does not copy ${file}`);
});
