// The ray tracer's project page (D45): its controls, labels and files, in each language.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';
import { readRaytracerBench } from '../../src/templates.mjs';

const out = build(mkdtempSync(join(tmpdir(), 'raytracer-')));

test('the ray tracer page has its scenes, finishes, sliders and every label, in each language', () => {
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-raytracer.html'), 'utf8').replace(/(\.(?:png|jpe?g|webp|avif|gif|svg|mp4|webm))\?v=[0-9a-f]{10}"/g, '$1"');
    assert.ok(html.includes('data-raytracer'), lang);
    assert.match(html, /<canvas width="480" height="270" role="img" aria-label="[^"]+">/);
    // Five scenes: the spheres, then the four meshes of the topology project, which the site publishes.
    const meshes = [...html.matchAll(/data-mesh="\.\.\/(assets\/samples\/topologie\/\w+\.obj)"/g)].map((m) => m[1]);
    assert.equal(meshes.length, 4);
    for (const m of meshes) assert.ok(existsSync(join(out, m)), m);
    assert.equal((html.match(/<option value="(diffuse|metal|glass)">/g) || []).length, 3);
    // The number of workers: its options depend on the device, the script fills them.
    assert.ok(html.includes('<label for="rt-workers">') && html.includes('<select id="rt-workers" data-workers>'));
    // Every slider has a label tied to it and a value shown next to it.
    for (const key of ['yaw', 'pitch', 'distance']) {
      assert.ok(html.includes(`<label for="rt-${key}">`), key);
      assert.ok(html.includes(`<input id="rt-${key}" type="range"`), key);
      assert.ok(html.includes(`data-view-value="${key}"`), key);
    }
    // Light and materials: five sliders, a button back to the defaults; the image can be saved.
    for (const key of ['azimuth', 'elevation', 'kelvin', 'fuzz', 'ior']) {
      assert.ok(html.includes(`<label for="rt-${key}">`) && html.includes(`data-setting="${key}"`) && html.includes(`data-setting-value="${key}"`), key);
    }
    assert.ok(html.includes('data-reset-settings') && html.includes('data-save'), lang);
    // The high-definition captures, each with its text.
    const shots = [...html.matchAll(/<img src="\.\.\/(assets\/images\/raytracer-[\w-]+\.png)" width="640" height="360" loading="lazy" alt="([^"]+)">/g)];
    assert.equal(shots.length, 4, lang);
    for (const [, src] of shots) assert.ok(existsSync(join(out, src)), src);
    const labels = JSON.parse(html.match(/data-labels="([^"]+)" ?>/)[1].replaceAll('&quot;', '"').replaceAll('&amp;', '&'));
    for (const [key, text] of Object.entries(labels)) assert.ok(text && !text.startsWith('rt.'), `${lang}: ${key}`);
    // The visitor's file: chosen or dropped, refused with lib-c's reason and line.
    assert.ok(html.includes('<input type="file" accept=".obj,.ply,.stl" class="visually-hidden" data-file>'), lang);
    assert.ok(html.includes('data-error role="alert" hidden'), lang);
    for (const k of ['own', 'mesh.atline', 'mesh.error', 'mesh.error.1', 'mesh.error.2', 'mesh.error.3', 'mesh.error.4', 'mesh.error.too-large', 'mesh.error.invalid']) {
      assert.ok(labels[k], `${lang}: ${k}`);
    }
    assert.match(labels.own, /\{name\}/);
    assert.match(labels['mesh.atline'], /\{line\}/);
    assert.match(labels.progress, /\{n\}.*\{max\}.*\{ms\}.*\{workers\}/);
    assert.ok(html.includes('<script type="module" src="../assets/raytracerplay.js'));
  }
  // The measured gain of several workers: one row per count, the first the reference.
  const bench = readRaytracerBench();
  assert.equal(bench.rows[0].workers, 1);
  assert.equal(bench.rows[0].speedup, 1);
  for (const r of bench.rows) assert.ok(r.msPerPass > 0 && Math.abs(r.speedup - bench.rows[0].msPerPass / r.msPerPass) < 0.01, `${r.workers}`);
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-raytracer.html'), 'utf8').replace(/(\.(?:png|jpe?g|webp|avif|gif|svg|mp4|webm))\?v=[0-9a-f]{10}"/g, '$1"');
    assert.equal((html.match(/<tr><th scope="row">\d+<\/th>/g) || []).length, bench.rows.length, lang);
    assert.ok(html.includes(bench.machine.cpu), lang);
  }
  for (const file of ['raytracerplay.js', 'raytracer-worker.js', 'raytracer-api.js', 'raytracer-bands.js', 'wasm/raytracer.js', 'wasm/raytracer.wasm']) {
    assert.ok(existsSync(join(out, 'assets', file)), file);
  }
});
