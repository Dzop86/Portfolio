// Live LaTeX editor of the latex project page (D23): the source is parsed and rendered by
// projects/latex on every pause in typing; diagnostics move the cursor to their line and column, the
// outline scrolls the preview to its section. Bundled by esbuild into assets/latexeditor.js.
import { renderLatex } from '../../projects/latex/src/index.ts';

/** Pause in typing before the preview is redrawn. */
const DELAY_MS = 150;

const root = document.querySelector('[data-latex-editor]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const lang = document.documentElement.lang === 'fr' ? 'fr' : 'en';
  const source = root.querySelector('textarea');
  const preview = root.querySelector('[data-preview]');
  const diagList = root.querySelector('[data-diagnostics]');
  const status = root.querySelector('[data-status]');
  const outlineList = root.querySelector('[data-outline]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  let timer;
  let articleLang = lang;

  const el = (tag, attrs = {}, text = '') => {
    const node = document.createElement(tag);
    Object.entries(attrs).forEach(([k, v]) => node.setAttribute(k, v));
    node.textContent = text;
    return node;
  };

  // On a narrow screen one pane shows at a time; the tabs switch it.
  function show(view) {
    root.dataset.view = view;
    root.querySelectorAll('[data-tab]').forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.tab === view)));
  }

  // Line and column (1-based, columns in characters) to an index in the source.
  function offsetOf(line, column) {
    const lines = source.value.split('\n');
    let offset = 0;
    for (let i = 0; i < Math.min(line - 1, lines.length); i++) offset += lines[i].length + 1;
    const current = lines[Math.min(line - 1, lines.length - 1)] ?? '';
    return offset + Array.from(current).slice(0, Math.max(column - 1, 0)).join('').length;
  }

  function goTo(line, column) {
    show('source');
    const at = offsetOf(line, column);
    source.focus();
    source.setSelectionRange(at, at);
    // Bring the line into view: about one line height per line above it.
    const lineHeight = parseFloat(getComputedStyle(source).lineHeight) || 20;
    source.scrollTop = Math.max(0, (line - 3) * lineHeight);
  }

  function update() {
    // The page has its h1 and the editor's h2: the article's title is an h3, its sections h4.
    const r = renderLatex(source.value, { lang: articleLang, headingLevel: 3 });
    preview.innerHTML = r.html;
    const errors = r.diagnostics.filter((d) => d.severity === 'error').length;
    status.textContent = r.diagnostics.length
      ? fill(labels.count, { errors, warnings: r.diagnostics.length - errors })
      : labels.none;
    status.dataset.state = errors ? 'error' : r.diagnostics.length ? 'warning' : 'ok';
    diagList.replaceChildren(...r.diagnostics.map((d) => {
      const li = el('li', { class: `diag-${d.severity}` });
      const button = el('button', { type: 'button', class: 'diag' });
      button.append(el('span', { class: 'diag-kind' }, labels[d.severity]), ` ${fill(labels.diag, { ...d.pos, message: d.message })}`);
      button.addEventListener('click', () => goTo(d.pos.line, d.pos.column));
      li.append(button);
      return li;
    }));
    outlineList.replaceChildren(...r.outline.map((e) => {
      const li = el('li', { class: `outline-${e.level}` });
      const button = el('button', { type: 'button', class: 'outline-link' }, `${e.number} ${e.title}`);
      button.addEventListener('click', () => {
        show('preview');
        // Scroll the preview only (scrollIntoView would also move the page under its sticky header).
        const target = preview.querySelector(`#${CSS.escape(e.id)}`);
        if (target) preview.scrollTop = target.offsetTop - 8;
      });
      li.append(button);
      return li;
    }));
  }

  source.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(update, DELAY_MS);
  });
  for (const button of root.querySelectorAll('[data-article]')) {
    button.addEventListener('click', () => {
      root.querySelectorAll('[data-article]').forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
      articleLang = button.dataset.article;
      source.value = button.dataset.source;
      update();
    });
  }
  for (const tab of root.querySelectorAll('[data-tab]')) tab.addEventListener('click', () => show(tab.dataset.tab));
  root.querySelector('[data-download]').addEventListener('click', () => {
    const url = URL.createObjectURL(new Blob([source.value], { type: 'text/x-tex;charset=utf-8' }));
    const a = el('a', { href: url, download: `portfolio.${articleLang}.tex` });
    document.body.append(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  });

  show('preview');
  update();
}
