(() => {
  const root = document.documentElement;

  // Theme toggle: remembers the choice when storage is available.
  const toggle = document.querySelector('.theme-toggle');
  if (toggle) {
    toggle.addEventListener('click', () => {
      const dark = root.dataset.theme
        ? root.dataset.theme === 'dark'
        : window.matchMedia('(prefers-color-scheme: dark)').matches;
      const next = dark ? 'light' : 'dark';
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

  // Projects filter.
  chipGroup('[data-filter]', 'data-filter', (group) => {
    document.querySelectorAll('[data-filterable] .card').forEach((card) => {
      card.hidden = group !== 'all' && card.dataset.group !== group;
    });
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
})();
