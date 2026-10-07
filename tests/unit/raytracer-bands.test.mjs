// How the ray tracer's image is shared between web workers (raytracer-bands.js), and the integration
// check that matters: several WebAssembly modules, each with its own bands, give the image of one.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { BAND, MAX_WORKERS, bandsFor, defaultWorkers, PassCounter } from '../../src/assets/raytracer-bands.js';
import { loadRaytracer, setScene, resize, renderRows, pixels, rows } from '../../src/assets/raytracer-api.js';

const create = (await import('../../src/assets/wasm/raytracer.js')).default;

test('the bands of all the workers cover every row exactly once, dealt in turn', () => {
  for (const height of [1, 7, 8, 9, 270, 271]) {
    for (let workers = 1; workers <= MAX_WORKERS; workers++) {
      const seen = new Uint8Array(height);
      for (let k = 0; k < workers; k++) {
        for (const [y0, y1] of bandsFor(height, workers, k)) {
          assert.ok(y0 < y1 && y1 - y0 <= BAND && y1 <= height, `${height}, ${workers}, ${k}: [${y0}, ${y1})`);
          assert.equal(Math.floor(y0 / BAND) % workers, k);
          for (let y = y0; y < y1; y++) seen[y]++;
        }
      }
      assert.ok(seen.every((n) => n === 1), `${height} rows, ${workers} workers`);
    }
  }
});

test('one core is left to the page, within 1 and MAX_WORKERS', () => {
  assert.deepEqual([undefined, 0, 1, 2, 4, 12, 64].map(defaultWorkers), [1, 1, 1, 1, 3, MAX_WORKERS, MAX_WORKERS]);
});

test('the pass counter shows the fewest passes of any band', () => {
  const c = new PassCounter(20); // three bands: 0, 8, 16
  assert.equal(c.record(0, 1), false);
  assert.equal(c.record(16, 1), false);
  assert.equal(c.record(8, 1), true);
  assert.equal(c.samples, 1);
  c.record(0, 3);
  assert.equal(c.record(8, 2), false); // band 16 is still at 1
  assert.equal(c.record(16, 2), true);
  assert.equal(c.samples, 2);
});

test('three modules rendering their own bands give the image of one, byte for byte', async () => {
  const [width, height, workers] = [48, 27, 3];
  const one = await loadRaytracer(create);
  resize(one, width, height);
  setScene(one, 'spheres');
  const image = new Uint8Array(width * height * 4);
  const libs = await Promise.all(Array.from({ length: workers }, () => loadRaytracer(create)));
  for (const lib of libs) {
    resize(lib, width, height);
    setScene(lib, 'spheres');
  }
  for (let pass = 0; pass < 3; pass++) {
    renderRows(one, 0, height);
    libs.forEach((lib, k) => {
      for (const [y0, y1] of bandsFor(height, workers, k)) {
        renderRows(lib, y0, y1);
        image.set(rows(lib, y0, y1), y0 * width * 4);
      }
    });
  }
  assert.deepEqual(image, pixels(one));
});
