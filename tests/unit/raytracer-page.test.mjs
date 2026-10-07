// The ray tracer's project page (D45): its controls, labels and files, in each language.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';

const out = build(mkdtempSync(join(tmpdir(), 'raytracer-')));

test('the ray tracer page has its scenes, finishes, sliders and every label, in each language', () => {
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-raytracer.html'), 'utf8');
    assert.ok(html.includes('data-raytracer'), lang);
    assert.match(html, /<canvas width="480" height="270" role="img" aria-label="[^"]+">/);
    // Five scenes: the spheres, then the four meshes of the topology project, which the site publishes.
    const meshes = [...html.matchAll(/data-mesh="\.\.\/(assets\/samples\/topologie\/\w+\.obj)"/g)].map((m) => m[1]);
    assert.equal(meshes.length, 4);
    for (const m of meshes) assert.ok(existsSync(join(out, m)), m);
    assert.equal((html.match(/<option value="(diffuse|metal|glass)">/g) || []).length, 3);
    // Every slider has a label tied to it and a value shown next to it.
    for (const key of ['yaw', 'pitch', 'distance']) {
      assert.ok(html.includes(`<label for="rt-${key}">`), key);
      assert.ok(html.includes(`<input id="rt-${key}" type="range"`), key);
      assert.ok(html.includes(`data-view-value="${key}"`), key);
    }
    const labels = JSON.parse(html.match(/data-labels="([^"]+)" ?>/)[1].replaceAll('&quot;', '"').replaceAll('&amp;', '&'));
    for (const [key, text] of Object.entries(labels)) assert.ok(text && !text.startsWith('rt.'), `${lang}: ${key}`);
    assert.match(labels.progress, /\{n\}.*\{max\}.*\{ms\}/);
    assert.ok(html.includes('<script type="module" src="../assets/raytracerplay.js'));
  }
  for (const file of ['raytracerplay.js', 'raytracer-worker.js', 'raytracer-api.js', 'wasm/raytracer.js', 'wasm/raytracer.wasm']) {
    assert.ok(existsSync(join(out, 'assets', file)), file);
  }
});
