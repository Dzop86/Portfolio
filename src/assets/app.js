(() => {
  const root = document.documentElement;

  // Theme toggle: remembers the choice when storage is available.
  const toggle = document.querySelector('.theme-toggle');
  if (toggle) {
    toggle.addEventListener('click', () => {
      // Dark grey is the default whatever the OS preference (D11).
      const next = root.dataset.theme === 'light' ? 'dark' : 'light';
      root.dataset.theme = next;
      try { localStorage.setItem('theme', next); } catch (e) { /* storage unavailable */ }
    });
  }

  // Generic chip group: one chip pressed at a time, calls back with the value.
  function chipGroup(selector, attr, onChange) {
    const chips = document.querySelectorAll(selector);
    chips.forEach((chip) => chip.addEventListener('click', () => {
      chips.forEach((c) => c.setAttribute('aria-pressed', String(c === chip)));
      onChange(chip.getAttribute(attr));
    }));
  }

  // Projects filters: a category and a language, both applied. The games have their own section,
  // hidden when none of its cards is shown; a notice says when nothing matches at all.
  const filter = { group: 'all', language: 'all' };
  function applyProjectFilters() {
    const cards = document.querySelectorAll('[data-filterable] .card');
    cards.forEach((card) => {
      card.hidden = (filter.group !== 'all' && card.dataset.group !== filter.group)
        || (filter.language !== 'all' && !card.dataset.languages.split('|').includes(filter.language));
    });
    const games = document.querySelector('[data-games]');
    if (games) games.hidden = ![...games.querySelectorAll('.card')].some((card) => !card.hidden);
    const empty = document.querySelector('[data-filter-empty]');
    if (empty) empty.hidden = [...cards].some((card) => !card.hidden);
  }
  chipGroup('[data-filter]', 'data-filter', (group) => { filter.group = group; applyProjectFilters(); });
  chipGroup('[data-language]', 'data-language', (language) => { filter.language = language; applyProjectFilters(); });

  // Teaching filter, with live totals.
  const table = document.querySelector('[data-teaching]');
  if (table) {
    chipGroup('[data-level]', 'data-level', (level) => {
      let td = 0;
      let tp = 0;
      table.querySelectorAll('tbody tr').forEach((row) => {
        const show = level === 'all' || row.dataset.level === level;
        row.hidden = !show;
        if (show) {
          td += Number(row.dataset.td);
          tp += Number(row.dataset.tp);
        }
      });
      table.querySelector('[data-total="td"]').textContent = td;
      table.querySelector('[data-total="tp"]').textContent = tp;
      table.querySelector('[data-total="all"]').textContent = td + tp;
    });
  }

  // Risk register: each header button sorts by its column; numbers start with the highest, ids with R1.
  // A second click on the same column reverses the order; ties keep the order of the ids.
  const risks = document.querySelector('[data-risks]');
  if (risks) {
    const body = risks.tBodies[0];
    risks.querySelectorAll('[data-sort]').forEach((button) => button.addEventListener('click', () => {
      const key = button.dataset.sort;
      const th = button.closest('th');
      const current = th.getAttribute('aria-sort');
      const first = key === 'id' ? 'ascending' : 'descending';
      const order = current ? (current === 'ascending' ? 'descending' : 'ascending') : first;
      const sign = order === 'ascending' ? 1 : -1;
      const rows = [...body.rows].sort((a, b) =>
        sign * (Number(a.dataset[key]) - Number(b.dataset[key])) || Number(a.dataset.id) - Number(b.dataset.id));
      body.append(...rows);
      risks.querySelectorAll('th[aria-sort]').forEach((h) => h.removeAttribute('aria-sort'));
      th.setAttribute('aria-sort', order);
    }));
  }
})();
