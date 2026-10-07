// The viewer as a React component, in jsdom: no WebGL there, so the fallback is what is drawn, and the
// reader is a stand-in returning a tetrahedron (the WebAssembly one is tested in topology.test.ts).
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { MeshesFile } from '../src/api';
import { makeT } from '../src/i18n';
import type { Reader, Topology } from '../src/viewer/topology';
import { ViewerView, type MakeRenderer } from '../src/views/ViewerView';

const meshes: MeshesFile = {
  wasm: 'assets/wasm/topo.js',
  samples: [
    { id: 'torus', file: 'assets/samples/topologie/torus.obj', name: { fr: 'Tore', en: 'Torus' } },
    { id: 'sphere', file: 'assets/samples/topologie/sphere.obj', name: { fr: 'Sphère', en: 'Sphere' } },
  ],
};

// A tetrahedron: 4 vertices, sphere-like (χ = 2), every vertex with the same positive curvature.
const tetrahedron: Topology = {
  ok: true,
  invariants: { components: 1, boundaryLoops: 0, euler: 2, genus: 0, orientable: true, manifold: true },
  totalCurvature: 4 * Math.PI,
  positions: new Float32Array([1, 1, 1, 1, -1, -1, -1, 1, -1, -1, -1, 1]),
  indices: new Uint32Array([0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2]),
  curvature: new Float32Array([3, 3, 3, 3]),
  defect: new Float32Array([Math.PI, Math.PI, Math.PI, Math.PI]),
  boundary: new Uint8Array(4),
};

const noWebgl: MakeRenderer = () => {
  throw new Error('WebGL unavailable');
};

function serveSamples() {
  const fetch = vi.fn(async (url: URL | string) => new Response(`# ${String(url)}`));
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

afterEach(() => vi.unstubAllGlobals());

function viewer(lang: 'fr' | 'en', read: Reader) {
  return render(<ViewerView meshes={meshes} lang={lang} t={makeT(lang)} siteRoot="../" theme="dark" read={read} makeRenderer={noWebgl} />);
}

describe('the 3D viewer', () => {
  it('reads the first sample from the site, then shows its invariants', async () => {
    const fetch = serveSamples();
    const read = vi.fn<Reader>(async () => tetrahedron);
    viewer('fr', read);
    expect(await screen.findByText('Invariants de Tore')).toBeInTheDocument();
    expect(String(fetch.mock.calls[0]![0])).toMatch(/\/assets\/samples\/topologie\/torus\.obj$/);
    expect(read).toHaveBeenCalledTimes(1);
    const field = (key: string) => document.querySelector(`[data-field="${key}"]`)?.textContent;
    expect(field('euler')).toBe('2');
    expect(field('genus')).toBe('0');
    expect(field('orientable')).toBe('oui');
    expect(field('total')).toBe('2 × 2π');
    expect(screen.getByRole('button', { name: 'Tore' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('says plainly when WebGL is missing, and still computes', async () => {
    serveSamples();
    viewer('en', async () => tetrahedron);
    expect(await screen.findByText(/WebGL unavailable/)).toBeInTheDocument();
    expect(document.querySelector('canvas')).toBeNull();
    expect(await screen.findByText('Invariants of Torus')).toBeInTheDocument();
  });

  it('reads another sample on demand', async () => {
    serveSamples();
    const user = userEvent.setup();
    const read = vi.fn<Reader>(async () => tetrahedron);
    viewer('en', read);
    await screen.findByText('Invariants of Torus');
    await user.click(screen.getByRole('button', { name: 'Sphere' }));
    expect(await screen.findByText('Invariants of Sphere')).toBeInTheDocument();
    expect(read).toHaveBeenCalledTimes(2);
    expect(screen.getByRole('button', { name: 'Sphere' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Torus' })).toHaveAttribute('aria-pressed', 'false');
  });

  it('opens a local file, and explains lib-c\'s error with its line', async () => {
    serveSamples();
    const user = userEvent.setup();
    const read = vi.fn<Reader>(async () => tetrahedron);
    viewer('fr', read);
    await screen.findByText('Invariants de Tore');
    read.mockResolvedValueOnce({ ok: false, status: 4, message: 'index out of range', line: 3 });
    const file = new File(['v 0 0 0\nf 1 2 9\n'], 'casse.obj', { type: 'text/plain' });
    await user.upload(screen.getByLabelText('Ouvrir un fichier OBJ ou PLY'), file);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Ce fichier n’a pas pu être lu (ligne 3) : une face désigne un sommet absent.');
    expect(within(screen.getByRole('group')).getAllByRole('button').every((b) => b.getAttribute('aria-pressed') === 'false')).toBe(true);
  });

  it('names a module that did not load', async () => {
    serveSamples();
    viewer('en', async () => ({ ok: false, status: 'load' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('the WebAssembly module could not be loaded.');
  });
});
