import { mkdirSync, rmSync, writeFileSync, cpSync } from 'node:fs';
import { join } from 'node:path';
import { buildSync } from 'esbuild';
import { ROOT, LANGS, PAGES, BASE_PATH, loadData, makeT, normalizeBase, esc, projectPage } from './lib.mjs';
import { renderPage, renderProjectPage } from './templates.mjs';

export function build(outDir = join(ROOT, 'dist'), { basePath = BASE_PATH } = {}) {
  const data = loadData();
  rmSync(outDir, { recursive: true, force: true });
  mkdirSync(outDir, { recursive: true });
  cpSync(join(ROOT, 'src/assets'), join(outDir, 'assets'), { recursive: true });
  // Sample meshes of the lib-c demo come straight from the library's test data.
  mkdirSync(join(outDir, 'assets/samples'), { recursive: true });
  for (const file of ['cube.obj', 'tetrahedron.ply', 'torus.obj']) {
    cpSync(join(ROOT, 'projects/lib-c/tests/data', file), join(outDir, 'assets/samples', file));
  }
  // The topology viewer shows the synthetic meshes of projects/topologie, in their own folder.
  cpSync(join(ROOT, 'projects/topologie/samples'), join(outDir, 'assets/samples/topologie'), { recursive: true });
  // SQL playground (D20): schema and campaign of projects/sql, SQLite compiled by sql.js, and the worker
  // that owns the database, bundled with sql.js into one module.
  mkdirSync(join(outDir, 'assets/samples/sql'), { recursive: true });
  cpSync(join(ROOT, 'projects/sql/sqlite/schema.sql'), join(outDir, 'assets/samples/sql/schema.sql'));
  cpSync(join(ROOT, 'projects/sql/data/measurements.csv'), join(outDir, 'assets/samples/sql/measurements.csv'));
  cpSync(join(ROOT, 'node_modules/sql.js/dist/sql-wasm-browser.wasm'), join(outDir, 'assets/wasm/sql-wasm.wasm'));
  buildSync({
    entryPoints: [join(ROOT, 'src/sqlplay/worker.js')],
    outfile: join(outDir, 'assets/sqlworker.js'),
    bundle: true,
    minify: true,
    format: 'esm',
    target: 'es2022',
    legalComments: 'inline',
    logLevel: 'error',
  });
  // three.js and the viewer, bundled and minified into one module (D17); WebAssembly stays a separate file.
  buildSync({
    entryPoints: [join(ROOT, 'src/viewer/topoviewer.js')],
    outfile: join(outDir, 'assets/topoviewer.js'),
    bundle: true,
    minify: true,
    format: 'esm',
    target: 'es2022',
    legalComments: 'inline',
    logLevel: 'error',
  });

  for (const lang of LANGS) {
    const t = makeT(data.i18n[lang], lang);
    mkdirSync(join(outDir, lang), { recursive: true });
    for (const page of PAGES) {
      writeFileSync(join(outDir, lang, `${page}.html`), renderPage(page, { lang, t, data }));
    }
    for (const project of data.projects) {
      writeFileSync(join(outDir, lang, `${projectPage(project.id)}.html`), renderProjectPage(project, { lang, t, data }));
    }
  }

  // Root page: picks the visitor's language, French by default.
  writeFileSync(join(outDir, 'index.html'), `<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Charles Lepaire</title>
<meta http-equiv="refresh" content="0; url=./fr/index.html">
<script>try{var l=(navigator.language||'fr').slice(0,2);location.replace(l==='fr'?'./fr/index.html':'./en/index.html')}catch(e){}</script>
</head><body><a href="./fr/index.html">Français</a> · <a href="./en/index.html">English</a></body></html>
`);

  // Served at whatever URL was missed (e.g. /Portfolio/a/b), so relative links resolve from <base> instead.
  writeFileSync(join(outDir, '404.html'), `<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<base href="${esc(normalizeBase(basePath))}">
<title>404 · Charles Lepaire</title><link rel="stylesheet" href="./assets/tokens.css"><link rel="stylesheet" href="./assets/style.css"></head>
<body><main class="wrap page-head"><h1>404</h1><p class="lead">Page introuvable · Page not found</p>
<p class="actions"><a class="btn btn-primary" href="./fr/index.html">Accueil</a><a class="btn btn-ghost" href="./en/index.html">Home</a></p></main></body></html>
`);

  writeFileSync(join(outDir, 'manifest.webmanifest'), JSON.stringify({
    name: 'Charles Lepaire, portfolio',
    short_name: 'C. Lepaire',
    start_url: './',
    display: 'standalone',
    background_color: '#1f1f1f',
    theme_color: '#181818',
    icons: [{ src: 'assets/favicon.svg', sizes: 'any', type: 'image/svg+xml' }],
  }, null, 2));

  writeFileSync(join(outDir, '.nojekyll'), '');
  return outDir;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const out = build();
  console.log(`Site built in ${out}`);
}
