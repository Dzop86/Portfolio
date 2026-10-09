// The launcher's page: Tauri's commands (src/main.rs) do the work; this draws it.
const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const $ = (id) => document.getElementById(id);

let lang = navigator.language.startsWith('fr') ? 'fr' : 'en';
let installed = null;
let signedInAs = null;
let status = null; // [key, ...args] of the last message under the bar

const t = (key, ...args) => (window.TEXTS[lang][key] ?? key).replace(/\{(\d)\}/g, (_, i) => args[i]);
const size = (bytes) => {
  const [kilo, mega] = lang === 'fr' ? ['ko', 'Mo'] : ['kB', 'MB'];
  return bytes < 1e6 ? `${(bytes / 1e3).toFixed(0)} ${kilo}` : `${(bytes / 1e6).toFixed(1)} ${mega}`;
};

function draw() {
  document.documentElement.lang = lang;
  for (const el of document.querySelectorAll('[data-t]')) el.textContent = t(el.dataset.t);
  $('lang').textContent = lang === 'fr' ? 'EN' : 'FR';
  $('version').textContent = installed ? t('installed', installed) : t('not-installed');
  $('play').disabled = !installed;
  $('sign-in').hidden = signedInAs !== null;
  $('signed-in').hidden = signedInAs === null;
  if (signedInAs) $('signed-in').textContent = t('signed-in', signedInAs);
  if (status) $('progress').textContent = t(...status);
}

function fail(problem) {
  $('message').textContent = t(`error.${problem.code}`);
  console.warn(problem.detail);
}

async function busy(action) {
  for (const b of document.querySelectorAll('button:not(#lang)')) b.disabled = true;
  $('message').textContent = '';
  try {
    await action();
  } catch (problem) {
    fail(problem);
  } finally {
    for (const b of document.querySelectorAll('button:not(#lang)')) b.disabled = false;
    draw();
  }
}

$('lang').addEventListener('click', () => {
  lang = lang === 'fr' ? 'en' : 'fr';
  draw();
});

$('sign-in').addEventListener('submit', (event) => {
  event.preventDefault();
  busy(async () => {
    await invoke('sign_in', { server: $('server').value, name: $('name').value, password: $('password').value });
    signedInAs = $('name').value;
    $('password').value = '';
  });
});

$('update').addEventListener('click', () => busy(async () => {
  status = ['checking'];
  $('bar').hidden = false;
  draw();
  const report = await invoke('update_game', { server: $('server').value });
  installed = report.version;
  status = report.downloaded === 0 && report.removed === 0
    ? ['up-to-date', report.version]
    : ['updated', report.version, report.downloaded, size(report.received), report.removed];
  $('bar').hidden = true;
}));

$('play').addEventListener('click', () => busy(async () => {
  await invoke('play', { server: $('server').value, lang });
  status = ['started'];
}));

listen('progress', ({ payload: [done, total, file, files] }) => {
  $('bar').max = Math.max(total, 1);
  $('bar').value = done;
  status = ['progress', file, files, size(done), size(total)];
  draw();
});

invoke('settings').then((s) => {
  $('server').value = s.server;
  // The system's language, when the web view does not say it (Windows and macOS read it the same way).
  if (!navigator.language || navigator.language === 'en-US') lang = s.lang;
  installed = s.installed;
  draw();
});
