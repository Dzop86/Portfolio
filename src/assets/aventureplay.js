// "The lab at night" on the project page (D30): the Java engine compiled to JavaScript by TeaVM
// (wasm/aventure.js, an ES module exporting start, respond and over), behind a small terminal.
const root = document.querySelector('[data-adventure]');
if (root) {
  const log = root.querySelector('[data-log]');
  const form = root.querySelector('form');
  const input = form.querySelector('input');
  const lang = document.documentElement.lang === 'fr' ? 'fr' : 'en';
  const history = [];
  let back = 0;
  let engine = null;

  const print = (text, kind) => {
    const p = document.createElement('p');
    p.className = kind;
    p.textContent = text;
    log.append(p);
    log.scrollTop = log.scrollHeight;
  };

  function run(command) {
    if (!engine || !command.trim()) return;
    print(`> ${command}`, 'adv-command');
    print(engine.respond(command), 'adv-answer');
    history.push(command);
    back = history.length;
  }

  function start() {
    log.replaceChildren();
    // A new game at each visit; the fight's outcome does not depend on it (see the project's Game.java).
    print(engine.start(lang, Math.floor(Math.random() * 2 ** 31)), 'adv-answer');
  }

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    run(input.value);
    input.value = '';
  });
  // Up and down walk through the commands already typed, as in a terminal.
  input.addEventListener('keydown', (e) => {
    if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
    e.preventDefault();
    back = Math.max(0, Math.min(history.length, back + (e.key === 'ArrowUp' ? -1 : 1)));
    input.value = history[back] ?? '';
  });
  for (const b of root.querySelectorAll('[data-command]')) b.addEventListener('click', () => run(b.dataset.command));
  root.querySelector('[data-restart]').addEventListener('click', () => engine && start());

  import('./wasm/aventure.js').then((module) => {
    engine = module;
    start();
    input.disabled = false;
  }).catch(() => print(root.dataset.error, 'adv-error'));
}
