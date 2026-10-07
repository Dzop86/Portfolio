import { mkdirSync, rmSync, writeFileSync, cpSync, readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
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
  // Tic-tac-toe (D34): the move book computed by the Python program of projects/morpion.
  cpSync(join(ROOT, 'projects/morpion/data/book.json'), join(outDir, 'assets/samples/morpion/book.json'));
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
  // LaTeX editor (D23): the TypeScript library and KaTeX bundled into one module, KaTeX's stylesheet and
  // fonts next to it.
  cpSync(join(ROOT, 'node_modules/katex/dist/katex.min.css'), join(outDir, 'assets/katex/katex.min.css'));
  cpSync(join(ROOT, 'node_modules/katex/dist/fonts'), join(outDir, 'assets/katex/fonts'), { recursive: true });
  buildSync({
    entryPoints: [join(ROOT, 'src/latexeditor/editor.js')],
    outfile: join(outDir, 'assets/latexeditor.js'),
    bundle: true,
    minify: true,
    format: 'esm',
    target: 'es2022',
    legalComments: 'inline',
    logLevel: 'error',
  });
  // Generalized maps course (D24): its script bundled with the projects/gcartes library.
  buildSync({
    entryPoints: [join(ROOT, 'src/gcourse/course.js')],
    outfile: join(outDir, 'assets/gcourse.js'),
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

  // Every stylesheet and script link carries a fingerprint of the file (style.css?v=1a2b3c4d): GitHub
  // Pages lets browsers keep assets for ten minutes, and a new page with an old style.css drew the
  // charts all black (remark of Charles). A changed file now has a new address.
  const fingerprints = new Map();
  const versioned = (html) => html.replace(/(href|src)="((?:\.\.?\/)assets\/([^"?#]+\.(?:css|js)))"/g, (_, attr, url, file) => {
    if (!fingerprints.has(file)) fingerprints.set(file, createHash('sha256').update(readFileSync(join(outDir, 'assets', file))).digest('hex').slice(0, 10));
    return `${attr}="${url}?v=${fingerprints.get(file)}"`;
  });
  const writePage = (path, html) => writeFileSync(path, versioned(html));

  for (const lang of LANGS) {
    const t = makeT(data.i18n[lang], lang);
    mkdirSync(join(outDir, lang), { recursive: true });
    for (const page of PAGES) {
      writePage(join(outDir, lang, `${page}.html`), renderPage(page, { lang, t, data }));
    }
    for (const project of data.projects) {
      writePage(join(outDir, lang, `${projectPage(project.id)}.html`), renderProjectPage(project, { lang, t, data }));
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
  writePage(join(outDir, '404.html'), `<!doctype html>
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
