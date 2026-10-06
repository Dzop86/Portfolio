import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PAGES = ['index', 'projects', 'research', 'method', 'contact', 'project-vitrine', 'project-lib-c', 'project-topologie', 'project-sql'];

// Every page, in both languages and both themes: axe also checks colour contrast.
for (const theme of ['dark', 'light']) {
  for (const lang of ['fr', 'en']) {
    for (const page of PAGES) {
      test(`${lang}/${page} (${theme}): loads, is accessible and has no horizontal scroll`, async ({ page: p }) => {
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        await p.addInitScript((t) => localStorage.setItem('theme', t), theme);
        await p.goto(`/${lang}/${page}.html`);
        await expect(p.locator('h1')).toBeVisible();
        await expect(p.locator('html')).toHaveAttribute('lang', lang);

        const overflow = await p.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
        expect(overflow).toBeLessThanOrEqual(1);

        const a11y = await new AxeBuilder({ page: p }).withTags(['wcag2a', 'wcag2aa']).analyze();
        expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
        expect(errors).toEqual([]);
      });
    }
  }
}

test('the root page redirects to a language', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/(fr|en)\/index\.html$/);
});

test('language switch keeps the current page', async ({ page }) => {
  await page.goto('/fr/research.html');
  await page.getByRole('link', { name: 'English' }).click();
  await expect(page).toHaveURL(/\/en\/research\.html$/);
  await expect(page.locator('h1')).toHaveText('Research and teaching');
});

test('the site is dark grey by default, even when the OS prefers light', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await page.goto('/fr/index.html');
  const bg = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
  expect(bg).toBe('rgb(31, 31, 31)');
});

test('theme toggle switches to light and remembers it', async ({ page }) => {
  await page.goto('/fr/index.html');
  await page.getByRole('button', { name: 'Changer de thème' }).click();
  const bg = () => page.evaluate(() => getComputedStyle(document.body).backgroundColor);
  expect(await bg()).toBe('rgb(255, 255, 255)');
  await page.reload();
  expect(await bg()).toBe('rgb(255, 255, 255)');
});

test('a project card opens its detail page, which links to the next project', async ({ page }) => {
  await page.goto('/en/projects.html');
  await page.getByRole('link', { name: 'C mesh library' }).click();
  await expect(page).toHaveURL(/\/en\/project-lib-c\.html$/);
  await expect(page.locator('h1')).toHaveText('C mesh library');
  await page.getByRole('link', { name: /Qt\/OpenGL viewer/ }).click();
  await expect(page.locator('h1')).toHaveText('Qt/OpenGL viewer');
});

test('the lib-c demo reads a sample and a dropped file in the browser', async ({ page }) => {
  await page.goto('/en/project-lib-c.html');
  const result = page.locator('[data-mesh-demo] [data-result]');
  await page.getByRole('button', { name: 'Torus (OBJ)' }).click();
  await expect(result).toContainText('Euler characteristic');
  await expect(result.locator('[data-field="euler"]')).toHaveText('0');
  await expect(result.locator('[data-field="edges"]')).toHaveText('144');
  await expect(result).toContainText('genus g = 1');

  await page.locator('[data-mesh-demo] input[type=file]').setInputFiles({
    name: 'broken.obj', mimeType: 'text/plain', buffer: Buffer.from('v 0 0 0\nv 1 0 0\nf 1 2 3\n'),
  });
  await expect(result).toContainText('line 3');
  await expect(result).toContainText('missing vertex');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
});

test('the topology viewer shows invariants of the samples and of a dropped file', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-topologie.html');
  const result = page.locator('[data-topo-viewer] [data-result]');
  // The torus is loaded by default.
  await expect(result.locator('[data-field="genus"]')).toHaveText('1');
  await expect(result.locator('[data-field="euler"]')).toHaveText('0');

  await page.getByRole('button', { name: 'Möbius strip' }).click();
  await expect(result.locator('[data-field="orientable"]')).toHaveText('no');
  await expect(result.locator('[data-field="boundary"]')).toHaveText('1');

  await page.getByRole('button', { name: 'Sphere' }).click();
  await expect(result.locator('[data-field="genus"]')).toHaveText('0');
  // Without WebGL (headless Firefox here) the canvas gives way to a notice; the invariants above still work.
  const canvas = page.locator('[data-topo-viewer] canvas');
  if (await canvas.count()) await expect(canvas).toHaveAttribute('aria-label', /Sphere/);
  else await expect(page.locator('[data-topo-viewer] .viewer-stage')).toContainText('WebGL');

  await page.locator('[data-topo-viewer] input[type=file]').setInputFiles({
    name: 'broken.obj', mimeType: 'text/plain', buffer: Buffer.from('v 0 0 0\nv 1 0 0\nf 1 2 3\n'),
  });
  await expect(page.locator('[data-topo-viewer] [data-error]')).toContainText('line 3');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  expect(errors).toEqual([]);
});

test('the SQL playground runs the examples, a typed query, and survives errors, changes and endless queries', async ({ page }) => {
  // Three loads of the database and a deliberate 5 s timeout: more than the default 30 s on slower engines.
  test.setTimeout(60000);
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-sql.html');
  const root = page.locator('[data-sql-playground]');
  const status = root.locator('[data-status]');
  const error = root.locator('[data-error]');
  await root.scrollIntoViewIfNeeded();
  // The first example runs on its own once the playground is in view.
  await expect(status).toHaveText(/^18 rows · /, { timeout: 15000 });
  await expect(root.locator('tbody tr')).toHaveCount(18);
  await expect(root.locator('thead th').first()).toHaveText('implementation');

  await root.getByRole('button', { name: 'Measured complexity' }).click();
  await expect(status).toHaveText(/^6 rows · /);
  await expect(root.getByRole('button', { name: 'Measured complexity' })).toHaveAttribute('aria-pressed', 'true');

  const editor = root.getByLabel('SQL query (SQLite dialect)');
  await editor.fill('SELECT count(*) AS n FROM measurement');
  await editor.press('Control+Enter');
  await expect(root.locator('tbody td')).toHaveText('630');

  await editor.fill('SELECT * FROM measurement');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(status).toHaveText(/^first 200 rows of 630 · /);

  await editor.fill('SELECT * FROM nowhere');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(error).toHaveText(/SQL error: no such table: nowhere/);

  await editor.fill('DELETE FROM measurement');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(status).toHaveText(/^Query done, 630 rows changed/);
  await expect(error).toBeHidden();

  await root.getByRole('button', { name: 'Reload the database' }).click();
  await editor.fill('SELECT count(*) FROM measurement');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(root.locator('tbody td')).toHaveText('630');

  // An endless query is stopped by terminating the worker; the next query reloads the database.
  await editor.fill('WITH RECURSIVE r(x) AS (SELECT 1 UNION ALL SELECT x + 1 FROM r) SELECT count(*) FROM r');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(error).toHaveText(/Query stopped after 5 s/, { timeout: 15000 });
  await editor.fill('SELECT count(*) FROM file');
  await root.getByRole('button', { name: 'Run', exact: true }).click();
  await expect(root.locator('tbody td')).toHaveText('45');

  await root.locator('summary').click();
  await expect(root.locator('[data-schema] li')).toHaveCount(11);

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('roadmap sprint numbers sit above their columns, on every screen', async ({ page }) => {
  await page.goto('/fr/method.html');
  // Every visible sprint number is centred over the column of that sprint, as the bars are.
  const offsets = await page.evaluate(() => {
    const track = document.querySelector('.gantt-head .gantt-track').getBoundingClientRect();
    const count = Number(getComputedStyle(document.querySelector('.gantt-head .gantt-track')).getPropertyValue('--sprints'));
    const column = track.width / count;
    return [...document.querySelectorAll('.gantt-head .gantt-track span')]
      .filter((s) => getComputedStyle(s).visibility !== 'hidden')
      .map((s) => {
        const r = s.getBoundingClientRect();
        const expected = track.left + (Number(s.textContent) - 0.5) * column;
        return Math.abs(r.left + r.width / 2 - expected) / column;
      });
  });
  expect(offsets.length).toBeGreaterThan(4);
  for (const off of offsets) expect(off).toBeLessThan(0.3);
  // And a bar spans exactly its sprints: S10 starts where column 10 starts.
  const [bar, track] = await page.evaluate(() => {
    const row = [...document.querySelectorAll('.gantt-row')].find((r) => r.textContent.trim().startsWith('S10 '));
    const b = row.querySelector('.gantt-bar').getBoundingClientRect();
    const t = row.querySelector('.gantt-track').getBoundingClientRect();
    return [[b.left, b.width], [t.left, t.width]];
  });
  expect(Math.abs(bar[0] - (track[0] + (9 * track[1]) / 27))).toBeLessThan(2);
});

test('project filter shows only the chosen group', async ({ page }) => {
  await page.goto('/fr/projects.html');
  await page.getByRole('button', { name: 'Web' }).click();
  const visible = page.locator('[data-filterable] .card:visible');
  await expect(visible.first()).toBeVisible();
  for (const group of await visible.evaluateAll((els) => els.map((e) => e.dataset.group))) {
    expect(group).toBe('web');
  }
});

test('teaching filter updates the totals', async ({ page }) => {
  await page.goto('/fr/research.html');
  await expect(page.locator('[data-total="all"]')).toHaveText('376');
  await page.getByRole('button', { name: 'L1', exact: true }).click();
  // L1: 32 + 44 + 36 + 24 = 136 hours.
  await expect(page.locator('[data-total="all"]')).toHaveText('136');
});

test('a deep unknown URL shows a styled 404 with working links', async ({ page }) => {
  const response = await page.goto('/fr/missing/deeper/page.html');
  expect(response.status()).toBe(404);
  await expect(page.locator('h1')).toHaveText('404');
  const sheetsLoaded = await page.evaluate(() => {
    const sheets = [...document.styleSheets];
    return sheets.length >= 2 && sheets.every((s) => s.cssRules.length > 0);
  });
  expect(sheetsLoaded).toBe(true);
  await page.getByRole('link', { name: 'Accueil' }).click();
  await expect(page).toHaveURL(/\/fr\/index\.html$/);
});
