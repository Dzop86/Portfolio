// Osmose's launcher page: Tauri's commands (src/main.rs) do the work; this draws it.
// At start the game updates by itself; "Sign in" signs in (or up), waits for the update, then plays.
const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const $ = (id) => document.getElementById(id);

let lang = 'fr';
let signUp = false;
let passwordSaved = false;
let status = null; // [key, ...args] of the line under the bar
let message = null; // the last refusal's key
let notice = null; // [key, ...args] of the last good news (an account created)
let updating = null; // the update going on, or done: resolves to true when the game is ready

const t = (key, ...args) => (window.TEXTS[lang][key] ?? key).replace(/\{(\d)\}/g, (_, i) => args[i]);
const size = (bytes) => {
  const [kilo, mega] = lang === 'fr' ? ['ko', 'Mo'] : ['kB', 'MB'];
  const number = bytes < 1e6 ? (bytes / 1e3).toFixed(0) : (bytes / 1e6).toFixed(1);
  return `${lang === 'fr' ? number.replace('.', ',') : number} ${bytes < 1e6 ? kilo : mega}`;
};

function draw() {
  document.documentElement.lang = lang;
  for (const el of document.querySelectorAll('[data-t]')) el.textContent = t(el.dataset.t);
  $('lang').textContent = lang === 'fr' ? 'EN' : 'FR';
  $('submit').textContent = t(signUp ? 'sign-up' : 'sign-in');
  $('mode').textContent = t(signUp ? 'to-sign-in' : 'to-sign-up');
  $('confirm-row').hidden = !signUp;
  $('password').placeholder = passwordSaved && !signUp ? t('password-saved') : '';
  $('password').autocomplete = signUp ? 'new-password' : 'current-password';
  $('status').textContent = status ? t(...status) : '';
  $('message').textContent = message ? t(`error.${message}`) : '';
  $('notice').textContent = notice ? t(...notice) : '';
}

/** Updates the game (once, or again after a failure); resolves to whether it is ready. */
function update() {
  status = ['checking'];
  draw();
  updating = invoke('update_game').then((report) => {
    status = report.downloaded === 0 ? ['up-to-date', report.version] : ['updated', report.version, size(report.received)];
    $('bar').value = $('bar').max;
    draw();
    return true;
  }, (problem) => {
    status = null;
    message = problem.code;
    draw();
    updating = null;
    return false;
  });
  return updating;
}

$('lang').addEventListener('click', () => {
  lang = lang === 'fr' ? 'en' : 'fr';
  draw();
  invoke('set_lang', { lang }).catch(() => {});
});

$('mode').addEventListener('click', () => {
  signUp = !signUp;
  message = null;
  notice = null;
  draw();
});

$('name').addEventListener('input', () => {
  // A remembered password belongs to the remembered name only.
  passwordSaved = false;
  draw();
});

$('form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const name = $('name').value.trim();
  const password = $('password').value;
  message = !name || (!password && (signUp || !passwordSaved)) ? 'fill-in'
    : signUp && password.length < 10 ? 'short'
    : signUp && password !== $('confirm').value ? 'mismatch'
    : null;
  draw();
  if (message) return;
  for (const b of document.querySelectorAll('form button')) b.disabled = true;
  try {
    if (signUp) {
      // The account is created, then the player signs in like with any account.
      await invoke('create_account', { name, password });
      signUp = false;
      passwordSaved = false;
      $('password').value = '';
      $('confirm').value = '';
      message = null;
      notice = ['created', name];
      $('password').focus();
      return;
    }
    notice = null;
    const complaint = await invoke('connect', {
      name, password, rememberName: $('remember-name').checked, rememberPassword: $('remember-password').checked,
    });
    $('password').value = '';
    $('confirm').value = '';
    message = complaint;
    status = ['waiting'];
    draw();
    if (!(await (updating ?? update()))) {
      message = message ?? 'unreachable';
      status = ['not-installed'];
      return;
    }
    await invoke('play', { lang });
    status = ['started'];
  } catch (problem) {
    message = problem.code;
    console.warn(problem.detail);
  } finally {
    for (const b of document.querySelectorAll('form button')) b.disabled = false;
    draw();
  }
});

listen('progress', ({ payload: [done, total, file, files] }) => {
  $('bar').max = Math.max(total, 1);
  $('bar').value = done;
  if (files > 0) status = ['progress', file, files, size(done), size(total)];
  draw();
});

invoke('start').then((s) => {
  lang = s.lang;
  passwordSaved = s.passwordSaved;
  $('name').value = s.name ?? '';
  $('remember-name').checked = s.name !== null;
  $('remember-password').checked = s.passwordSaved;
  draw();
  ($('name').value ? $('password') : $('name')).focus();
  update();
});
