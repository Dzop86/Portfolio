// Maille playground: parses with the WebAssembly parser, runs with the js_of_ocaml interpreter
// (globalThis.MailleInterp, set by wasm/maille-interp.js), shows the value or the located error, and
// the syntax tree, whose nodes move the editor's cursor to their position.
import { loadParser, runProgram } from './maille-api.js';

/** Nodes drawn at most; a larger tree ends with "... n more nodes". */
const MAX_NODES = 400;

const root = document.querySelector('[data-maille-playground]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const editor = root.querySelector('textarea');
  const runButton = root.querySelector('[data-run]');
  const result = root.querySelector('[data-result]');
  const error = root.querySelector('[data-error]');
  const treeBox = root.querySelector('[data-tree]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  let parser = null;

  const el = (tag, attrs = {}, text = '') => {
    const node = document.createElement(tag);
    Object.entries(attrs).forEach(([k, v]) => node.setAttribute(k, v));
    node.textContent = text;
    return node;
  };

  // Line and column (1-based, columns in characters as the lexer counts them) to an index in the text.
  function offsetOf(line, column) {
    const lines = editor.value.split('\n');
    let offset = 0;
    for (let i = 0; i < Math.min(line - 1, lines.length); i++) offset += lines[i].length + 1;
    const current = lines[Math.min(line - 1, lines.length - 1)] ?? '';
    return offset + Array.from(current).slice(0, Math.max(column - 1, 0)).join('').length;
  }

  function goTo(line, column) {
    const at = offsetOf(line, column);
    editor.focus();
    editor.setSelectionRange(at, at);
  }

  function drawTree(tree) {
    let budget = MAX_NODES;
    let hidden = 0;
    const count = (n) => 1 + n.children.reduce((sum, c) => sum + count(c), 0);
    function item(n) {
      const li = el('li');
      const button = el('button', { type: 'button', class: 'ast-node', 'aria-label': `${n.kind} ${n.label} – ${fill(labels.node, n)}` });
      button.append(el('span', { class: 'ast-kind' }, n.kind));
      if (n.label) button.append(' ', el('code', {}, n.label));
      button.append(' ', el('span', { class: 'muted ast-pos', 'aria-hidden': 'true' }, `${n.line}:${n.column}`));
      button.addEventListener('click', () => goTo(n.line, n.column));
      li.append(button);
      if (n.children.length) {
        const ul = el('ul');
        for (const c of n.children) {
          if (budget-- > 0) ul.append(item(c));
          else hidden += count(c);
        }
        li.append(ul);
      }
      return li;
    }
    budget--;
    const top = el('ul', { class: 'ast' });
    top.append(item(tree));
    treeBox.replaceChildren(top);
    if (hidden) treeBox.append(el('p', { class: 'meta' }, fill(labels.more, { n: hidden })));
  }

  function showError(r) {
    const where = fill(labels.at, r);
    error.replaceChildren(el('p', {}, fill(labels.message, { kind: labels[r.stage] ?? r.stage, where, message: r.message })));
    const button = el('button', { type: 'button', class: 'btn btn-ghost' }, labels.goto);
    button.addEventListener('click', () => goTo(r.line, r.column));
    error.append(button);
    error.hidden = false;
  }

  async function run() {
    error.hidden = true;
    result.textContent = parser ? labels.running : labels.loading;
    runButton.disabled = true;
    try {
      parser ??= loadParser((await import('./wasm/maillec.js')).default);
      const lib = await parser;
      if (!globalThis.MailleInterp) throw new Error('interpreter not loaded');
      // Let the "running" text paint before a long evaluation.
      await new Promise((resolve) => requestAnimationFrame(() => setTimeout(resolve)));
      const r = runProgram(lib, globalThis.MailleInterp, editor.value);
      if (r.stage === 'value') {
        result.textContent = `- : ${r.type} = ${r.value}`;
      } else {
        result.textContent = '';
        if (r.stage === 'syntax' && /too large/.test(r.message)) r.message = labels['too-large'];
        showError(r);
      }
      if (r.tree) drawTree(r.tree);
      else treeBox.replaceChildren();
    } catch {
      parser = null;
      result.textContent = '';
      error.replaceChildren(el('p', {}, labels['error.load']));
      error.hidden = false;
    } finally {
      runButton.disabled = false;
    }
  }

  runButton.addEventListener('click', run);
  editor.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      run();
    }
  });
  for (const button of root.querySelectorAll('[data-example]')) {
    button.addEventListener('click', () => {
      root.querySelectorAll('[data-example]').forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
      editor.value = button.dataset.source;
      run();
    });
  }

  // The first example runs when the playground comes into view.
  const first = root.querySelector('[data-example]');
  new IntersectionObserver((entries, observer) => {
    if (entries.some((e) => e.isIntersecting)) {
      observer.disconnect();
      first.click();
    }
  }).observe(root);
}
