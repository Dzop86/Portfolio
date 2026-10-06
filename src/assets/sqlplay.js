// SQL playground: the visitor's query runs in a web worker (assets/sqlworker.js) on a SQLite copy of the
// benchmark database. A query still running after TIMEOUT_MS is stopped by terminating the worker.
const TIMEOUT_MS = 5000;

const root = document.querySelector('[data-sql-playground]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const lang = document.documentElement.lang;
  const editor = root.querySelector('textarea');
  const runButton = root.querySelector('[data-run]');
  const status = root.querySelector('[data-status]');
  const error = root.querySelector('[data-error]');
  const out = root.querySelector('[data-result]');
  const schemaList = root.querySelector('[data-schema]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  const num = (n) => n.toLocaleString(lang, { maximumFractionDigits: 4 });

  const el = (tag, attrs = {}, text = '') => {
    const node = document.createElement(tag);
    Object.entries(attrs).forEach(([k, v]) => node.setAttribute(k, v));
    node.textContent = text;
    return node;
  };

  // The worker answers each request with the same id; `ready` resolves once the database is loaded.
  let worker = null;
  let ready = null;
  let nextId = 0;
  const pending = new Map();

  function call(message) {
    const id = nextId++;
    return new Promise((resolve, reject) => {
      pending.set(id, { resolve, reject });
      worker.postMessage({ ...message, id });
    });
  }

  function start() {
    worker = new Worker(new URL('./sqlworker.js', import.meta.url), { type: 'module' });
    worker.onmessage = ({ data }) => {
      const p = pending.get(data.id);
      pending.delete(data.id);
      if (data.ok) p?.resolve(data);
      else p?.reject(Object.assign(new Error(data.error), { load: data.load }));
    };
    worker.onerror = () => stop(Object.assign(new Error(labels['error.load']), { load: true }));
    ready = call({ type: 'open' }).then(({ schema }) => showSchema(schema));
    return ready;
  }

  function stop(reason) {
    worker?.terminate();
    worker = null;
    ready = null;
    for (const p of pending.values()) p.reject(reason);
    pending.clear();
  }

  function showSchema(schema) {
    schemaList.replaceChildren(...schema.map(({ name, type, columns }) => {
      const li = el('li');
      li.append(el('code', {}, name), ` ${labels[`schema.${type}`]} · `, el('span', { class: 'muted' }, columns.join(', ')));
      return li;
    }));
  }

  function showError(text) {
    error.textContent = text;
    error.hidden = false;
  }

  function render({ result, ms }) {
    const time = fill(labels.ms, { ms: num(Math.round(ms * 10) / 10) });
    if (result.columns.length === 0) {
      status.textContent = `${fill(labels.changes, { n: num(result.changes) })} · ${time}`;
      return;
    }
    status.textContent = `${result.truncated
      ? fill(labels.truncated, { shown: num(result.rows.length), total: num(result.total) })
      : fill(result.total === 1 ? labels.row : labels.rows, { n: num(result.total) })} · ${time}`;
    if (result.total === 0) return;
    const table = el('table');
    table.append(el('caption', { class: 'visually-hidden' }, labels.caption));
    const head = el('tr');
    result.columns.forEach((c) => head.append(el('th', { scope: 'col' }, c)));
    table.append(el('thead'));
    table.tHead.append(head);
    const body = el('tbody');
    for (const row of result.rows) {
      const tr = el('tr');
      for (const v of row) {
        if (v === null) tr.append(el('td', { class: 'muted' }, 'NULL'));
        else tr.append(el('td', typeof v === 'number' ? { class: 'num' } : {}, typeof v === 'number' ? num(v) : String(v)));
      }
      body.append(tr);
    }
    table.append(body);
    const wrap = el('div', { class: 'table-wrap', role: 'region', 'aria-label': labels.caption, tabindex: '0' });
    wrap.append(table);
    out.append(wrap);
  }

  async function run() {
    const sql = editor.value;
    error.hidden = true;
    out.replaceChildren();
    status.textContent = labels.running;
    runButton.disabled = true;
    let timer;
    try {
      await (ready ?? start());
      const answer = call({ type: 'run', sql });
      timer = setTimeout(() => stop(Object.assign(new Error(fill(labels.timeout, { s: TIMEOUT_MS / 1000 })), { plain: true })), TIMEOUT_MS);
      render(await answer);
    } catch (e) {
      status.textContent = e.silent ? labels.reset : '';
      if (e.load) {
        stop(e);
        showError(labels['error.load']);
      } else if (e.plain) {
        showError(e.message);
      } else if (!e.silent) {
        showError(`${labels.error} ${e.message}`);
      }
    } finally {
      clearTimeout(timer);
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
  root.querySelector('[data-reset]').addEventListener('click', () => {
    stop(Object.assign(new Error(labels.reset), { silent: true }));
    out.replaceChildren();
    error.hidden = true;
    status.textContent = labels.reset;
  });
  for (const button of root.querySelectorAll('[data-example]')) {
    button.addEventListener('click', () => {
      root.querySelectorAll('[data-example]').forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
      editor.value = button.dataset.sql;
      run();
    });
  }

  // The first example runs when the playground comes into view, so the database loads only if it is seen.
  const first = root.querySelector('[data-example]');
  new IntersectionObserver((entries, observer) => {
    if (entries.some((e) => e.isIntersecting)) {
      observer.disconnect();
      first.click();
    }
  }).observe(root);
}
