// The site's Docker image builds from the files its Dockerfile copies, nothing else. This test copies
// exactly those files into an empty folder and runs the build there, so a page that starts reading a new
// file from projects/ fails here, not in the CI's Docker job (it happened twice).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, symlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';

/** COPY instructions of the build stage: [[sources...], destination]. */
function copies() {
  const dockerfile = readFileSync(join(ROOT, 'Dockerfile'), 'utf8');
  const buildStage = dockerfile.split(/^FROM /m)[1];
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
