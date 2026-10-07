// The 3D viewer: three.js inside a React component. The scene lives in refs, built once on mount and
// fully disposed on unmount (renderer, controls, geometry, material, observer, listeners); React only
// holds what is shown around it: the mesh read, its invariants, the error, the hover tip.
import { useEffect, useMemo, useRef, useState } from 'react';
import {
  AmbientLight, BufferAttribute, BufferGeometry, Color, DirectionalLight, DoubleSide, Mesh,
  MeshStandardMaterial, PerspectiveCamera, Raycaster, Scene, Vector2, WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import type { Lang, MeshesFile } from '../api';
import { formatNumber, type Key, type T } from '../i18n';
import { nearestCorner, totalTurns, vertexColours, wasmReader, type Palette, type Reader, type Topology, type TopologyError } from '../viewer/topology';

/** What draws the scene; tests give one that throws, as a browser without WebGL does. */
export type MakeRenderer = (canvas: HTMLCanvasElement) => WebGLRenderer;

const defaultRenderer: MakeRenderer = (canvas) => {
  const r = new WebGLRenderer({ canvas, antialias: true, alpha: true });
  r.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  return r;
};

interface Props {
  meshes: MeshesFile;
  lang: Lang;
  t: T;
  siteRoot: string;
  theme: string;
  read?: Reader;
  makeRenderer?: MakeRenderer;
}

type Shown = { name: string; topology: Topology };

// lib-c's statuses 1 to 4, then the wrapper's own; anything else reads as an unrecognised format.
const REASONS: Record<string, Key> = {
  1: 'viewer.reason.1',
  2: 'viewer.reason.2',
  3: 'viewer.reason.3',
  4: 'viewer.reason.4',
  invalid: 'viewer.reason.invalid',
  'too-large': 'viewer.reason.too-large',
  load: 'viewer.reason.load',
};

interface Stage {
  renderer: WebGLRenderer;
  scene: Scene;
  camera: PerspectiveCamera;
  shape: Mesh<BufferGeometry, MeshStandardMaterial> | null;
  render: () => void;
}

export function ViewerView({ meshes, lang, t, siteRoot, theme, read, makeRenderer = defaultRenderer }: Props) {
  const reader = useMemo(() => read ?? wasmReader(new URL(`${siteRoot}${meshes.wasm}`, window.location.href).href), [read, siteRoot, meshes.wasm]);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const rootRef = useRef<HTMLElement>(null);
  const stage = useRef<Stage | null>(null);
  const [webgl, setWebgl] = useState(true);
  const [selected, setSelected] = useState<string>(meshes.samples[0]?.id ?? '');
  const [shown, setShown] = useState<Shown | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<TopologyError | null>(null);
  const [tip, setTip] = useState<{ text: string; x: number; y: number } | null>(null);
  const [ticks, setTicks] = useState<{ at: number; value: number }[]>([]);
  const n = (x: number, digits = 3) => x.toLocaleString(lang === 'fr' ? 'fr-FR' : 'en-GB', { maximumFractionDigits: digits });

  // --- the scene, once ---------------------------------------------------------------------------
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    let renderer: WebGLRenderer;
    try {
      renderer = makeRenderer(canvas);
    } catch {
      setWebgl(false);
      return;
    }
    const scene = new Scene();
    const camera = new PerspectiveCamera(40, 1, 0.01, 100);
    camera.position.set(1.6, 1.4, 2.2);
    const light = new DirectionalLight(0xffffff, 2.2);
    light.position.set(1, 1, 2);
    camera.add(light);
    scene.add(camera, new AmbientLight(0xffffff, 0.9));
    const controls = new OrbitControls(camera, canvas);
    const render = () => renderer.render(scene, camera);
    controls.addEventListener('change', render);
    // CSS sizes the canvas (aspect-ratio); this only reads it, so it never resizes what it observes.
    const resize = () => {
      const { clientWidth: w, clientHeight: h } = canvas;
      if (!w || !h) return;
      renderer.setSize(w, h, false);
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
      render();
    };
    const observer = new ResizeObserver(resize);
    observer.observe(canvas);
    stage.current = { renderer, scene, camera, shape: null, render };
    return () => {
      observer.disconnect();
      controls.removeEventListener('change', render);
      controls.dispose();
      const shape = stage.current?.shape;
      shape?.geometry.dispose();
      shape?.material.dispose();
      renderer.dispose();
      stage.current = null;
    };
  }, [makeRenderer]);

  // --- reading a mesh ------------------------------------------------------------------------------
  const load = async (name: string, bytes: Promise<ArrayBuffer>, isCurrent: () => boolean = () => true) => {
    setLoading(true);
    setError(null);
    let result: Topology | TopologyError;
    try {
      result = await reader(new Uint8Array(await bytes));
    } catch {
      // The sample did not download: the same message as a module that did not load.
      result = { ok: false, status: 'load' };
    }
    if (!isCurrent()) return;
    setLoading(false);
    if (result.ok) setShown({ name, topology: result });
    else setError(result);
  };

  useEffect(() => {
    const sample = meshes.samples.find((m) => m.id === selected);
    if (!sample) return;
    let current = true;
    void load(sample.name[lang], fetch(new URL(`${siteRoot}${sample.file}`, window.location.href)).then((r) => {
      if (!r.ok) throw new Error(String(r.status));
      return r.arrayBuffer();
    }), () => current);
    return () => {
      current = false;
    };
    // Not on `lang`: the name shown follows the language without reading the file again.
  }, [selected, reader, siteRoot]);

  // --- the shape, each time the mesh or the theme changes -------------------------------------------
  useEffect(() => {
    const s = stage.current;
    const root = rootRef.current;
    if (!shown || !root) return;
    const token = (name: string) => {
      const c = new Color(getComputedStyle(root).getPropertyValue(name).trim() || '#808080');
      return [c.r, c.g, c.b] as [number, number, number];
    };
    const palette: Palette = { negative: token('--curv-neg'), zero: token('--curv-zero'), positive: token('--curv-pos') };
    const { colours, ticks: marks } = vertexColours(shown.topology, palette);
    setTicks(marks);
    if (!s) return;
    if (s.shape) {
      s.scene.remove(s.shape);
      s.shape.geometry.dispose();
      s.shape.material.dispose();
    }
    const geometry = new BufferGeometry();
    geometry.setAttribute('position', new BufferAttribute(shown.topology.positions, 3));
    geometry.setIndex(new BufferAttribute(shown.topology.indices, 1));
    geometry.computeVertexNormals();
    geometry.setAttribute('color', new BufferAttribute(colours, 3));
    s.shape = new Mesh(geometry, new MeshStandardMaterial({ vertexColors: true, side: DoubleSide, roughness: 0.7 }));
    s.scene.add(s.shape);
    s.render();
  }, [shown, theme]);

  // --- hover: curvature of the nearest vertex --------------------------------------------------------
  const ray = useRef(new Raycaster());
  const pointer = useRef(new Vector2());
  const onPointerMove = (e: React.PointerEvent<HTMLCanvasElement>) => {
    const s = stage.current;
    if (!s?.shape || !shown) return;
    const box = e.currentTarget.getBoundingClientRect();
    pointer.current.set(((e.clientX - box.left) / box.width) * 2 - 1, -((e.clientY - box.top) / box.height) * 2 + 1);
    ray.current.setFromCamera(pointer.current, s.camera);
    const hit = ray.current.intersectObject(s.shape)[0];
    if (!hit?.face) {
      setTip(null);
      return;
    }
    const { positions, curvature, defect, boundary } = shown.topology;
    const v = nearestCorner(positions, [hit.face.a, hit.face.b, hit.face.c], hit.point);
    const d = n((defect[v]! * 180) / Math.PI, 1);
    const text = boundary[v] ? t('viewer.tipBoundary', { d }) : t('viewer.tip', { k: n(curvature[v]!, 2), d });
    setTip({ text, x: e.clientX - box.left + 12, y: e.clientY - box.top + 12 });
  };

  const onFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSelected('');
    void load(file.name, file.arrayBuffer());
  };

  const reason = (err: TopologyError) => t(REASONS[String(err.status)] ?? 'viewer.reason.other');
  const inv = shown?.topology.invariants;
  const yesNo = (b: boolean) => (b ? t('viewer.yes') : t('viewer.no'));
  const sampleName = meshes.samples.find((m) => m.id === selected)?.name[lang];
  const name = sampleName ?? shown?.name ?? '';

  return (
    <section aria-labelledby="h-viewer" ref={rootRef}>
      <h2 id="h-viewer">{t('nav.viewer')}</h2>
      <p>{t('viewer.lead')}</p>
      <div className="viewer-controls" role="group" aria-label={t('viewer.samples')}>
        {meshes.samples.map((m) => (
          <button key={m.id} type="button" className="btn" aria-pressed={selected === m.id} onClick={() => setSelected(m.id)} data-sample={m.id}>
            {m.name[lang]}
          </button>
        ))}
        <label className="btn file">
          {t('viewer.file')}
          <input type="file" accept=".obj,.ply" onChange={onFile} />
        </label>
      </div>
      <div className="viewer-stage">
        {webgl ? (
          <canvas
            ref={canvasRef}
            role="img"
            aria-label={t('viewer.canvas', { name })}
            onPointerMove={onPointerMove}
            onPointerLeave={() => setTip(null)}
            data-viewer
          />
        ) : (
          <p className="notice" data-nowebgl>
            {t('viewer.nowebgl')}
          </p>
        )}
        {tip && (
          <span className="viewer-tip" style={{ left: tip.x, top: tip.y }}>
            {tip.text}
          </span>
        )}
      </div>
      <p className="muted">{t('viewer.help')}</p>
      {loading && (
        <p role="status" className="muted">
          {t('viewer.loading')}
        </p>
      )}
      {error && (
        <p role="alert" className="notice" data-viewer-error>
          {error.line ? t('viewer.errorLine', { line: error.line, reason: reason(error) }) : t('viewer.error', { reason: reason(error) })}
        </p>
      )}
      {ticks.length === 2 && shown && (
        <figure className="legend">
          <div className="legend-bar" aria-hidden="true" />
          <div className="legend-ticks muted">
            <span>{t('viewer.saddle')}</span>
            <span>
              −{n(ticks[1]!.value, 2)} · −{n(ticks[0]!.value, 2)} · 0 · +{n(ticks[0]!.value, 2)} · +{n(ticks[1]!.value, 2)}
            </span>
            <span>{t('viewer.dome')}</span>
          </div>
          <figcaption className="muted">{t('viewer.legend')}</figcaption>
        </figure>
      )}
      {inv && shown && (
        <>
          <h3 id="h-invariants">{t('viewer.invariants', { name: shown.name })}</h3>
          <dl className="invariants" aria-labelledby="h-invariants">
            {(
              [
                ['components', formatNumber(inv.components, lang)],
                ['boundary', formatNumber(inv.boundaryLoops, lang)],
                ['euler', formatNumber(inv.euler, lang)],
                ['genus', inv.genus === null ? '—' : formatNumber(inv.genus, lang)],
                ['orientable', yesNo(inv.orientable)],
                ['manifold', yesNo(inv.manifold)],
                ['total', t('viewer.turns', { n: n(totalTurns(shown.topology.totalCurvature)) })],
              ] as const
            ).map(([key, value]) => (
              <div key={key}>
                <dt>{t(`viewer.${key}`)}</dt>
                <dd data-field={key}>{value}</dd>
              </div>
            ))}
          </dl>
        </>
      )}
    </section>
  );
}
