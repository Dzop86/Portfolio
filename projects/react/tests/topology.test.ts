// @vitest-environment node
// The viewer's logic on the real WebAssembly build of projects/topologie, as the site publishes it.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it, vi } from 'vitest';
import { nearestCorner, totalTurns, vertexColours, wasmReader, type Topology } from '../src/viewer/topology';

const ROOT = join(import.meta.dirname, '../../..');
const WASM = join(ROOT, 'src/assets/wasm/topo.js');
const sample = (name: string) => new Uint8Array(readFileSync(join(ROOT, 'projects/topologie/samples', name)));
const read = wasmReader(WASM, (url) => import(url));

async function topology(name: string): Promise<Topology> {
  const r = await read(sample(name));
  if (!r.ok) throw new Error(`${name}: ${String(r.status)}`);
  return r;
}

describe('reading with the WebAssembly module', () => {
  it('finds the invariants of the samples (torus, sphere, Möbius strip)', async () => {
    const torus = await topology('torus.obj');
    expect(torus.invariants).toMatchObject({ components: 1, boundaryLoops: 0, euler: 0, genus: 1, orientable: true, manifold: true });
    expect(totalTurns(torus.totalCurvature)).toBe(0);
    const sphere = await topology('sphere.obj');
    expect(sphere.invariants).toMatchObject({ euler: 2, genus: 0 });
    expect(totalTurns(sphere.totalCurvature)).toBe(2);
    const mobius = await topology('mobius.obj');
    expect(mobius.invariants).toMatchObject({ orientable: false, boundaryLoops: 1 });
  });

  it('reports an index out of range with lib-c\'s status and the line', async () => {
    const r = await read(new TextEncoder().encode('v 0 0 0\nv 1 0 0\nf 1 2 9\n'));
    expect(r).toEqual({ ok: false, status: 4, message: 'index out of range', line: 3 });
  });

  it('turns a module that fails to load into an error, and tries again on the next read', async () => {
    const load = vi.fn().mockRejectedValueOnce(new Error('offline')).mockImplementation((url: string) => import(url));
    const flaky = wasmReader(WASM, load);
    expect(await flaky(sample('torus.obj'))).toEqual({ ok: false, status: 'load' });
    expect((await flaky(sample('torus.obj'))).ok).toBe(true);
    expect(load).toHaveBeenCalledTimes(2);
  });
});

describe('colours and pointer', () => {
  const palette = { negative: [0, 0, 1] as [number, number, number], zero: [0.5, 0.5, 0.5] as [number, number, number], positive: [1, 0, 0] as [number, number, number] };

  it('colours the sphere towards the dome colour, and the torus both ways', async () => {
    const sphere = await topology('sphere.obj');
    const { colours, ticks } = vertexColours(sphere, palette);
    expect(colours).toHaveLength(sphere.positions.length);
    // K > 0 everywhere: red above grey, blue below it.
    for (let i = 0; i < colours.length; i += 3) {
      expect(colours[i]!).toBeGreaterThanOrEqual(0.5);
      expect(colours[i + 2]!).toBeLessThanOrEqual(0.5);
    }
    expect(ticks.map((x) => x.at)).toEqual([0.5, 0.9]);
    expect(ticks[0]!.value).toBeLessThanOrEqual(ticks[1]!.value);

    const torus = vertexColours(await topology('torus.obj'), palette).colours;
    let red = 0;
    let blue = 0;
    for (let i = 0; i < torus.length; i += 3) {
      if (torus[i]! > 0.5) red++;
      if (torus[i + 2]! > 0.5) blue++;
    }
    expect(red).toBeGreaterThan(0); // outside of the ring
    expect(blue).toBeGreaterThan(0); // inside, saddle-shaped
  });

  it('every colour stays between the neutral and the end colours', async () => {
    const { colours } = vertexColours(await topology('saddle.obj'), palette);
    for (const c of colours) {
      expect(c).toBeGreaterThanOrEqual(0);
      expect(c).toBeLessThanOrEqual(1);
    }
  });

  it('picks the corner nearest to the point hit', () => {
    const positions = [0, 0, 0, 1, 0, 0, 0, 1, 0];
    expect(nearestCorner(positions, [0, 1, 2], { x: 0.9, y: 0.05, z: 0 })).toBe(1);
    expect(nearestCorner(positions, [0, 1, 2], { x: 0.1, y: 0.8, z: 0 })).toBe(2);
    expect(nearestCorner(positions, [0, 1, 2], { x: 0.1, y: 0.1, z: 0 })).toBe(0);
  });
});
