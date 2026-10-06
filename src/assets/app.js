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

  // Projects filter. The games have their own section, shown for "all" and "games" only.
  chipGroup('[data-filter]', 'data-filter', (group) => {
    document.querySelectorAll('[data-filterable] .card').forEach((card) => {
      card.hidden = group !== 'all' && card.dataset.group !== group;
    });
    const games = document.querySelector('[data-games]');
    if (games) games.hidden = group !== 'all' && group !== 'games';
  });

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
