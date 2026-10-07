import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PAGES = ['index', 'projects', 'research', 'method', 'contact', 'project-vitrine', 'project-lib-c', 'project-topologie', 'project-sql', 'project-langage', 'project-latex', 'project-gcartes', 'project-ml', 'project-othello', 'project-naval', 'project-aventure', 'project-bataille', 'project-morpion', 'project-rogue', 'project-qt'];

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
  await expect(page.locator('h1')).toHaveText('CV and experience');
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
  // Projects follow their sprints: topology (S4) comes after the C library (S2).
  await page.getByRole('link', { name: /3D topology/ }).click();
  await expect(page.locator('h1')).toHaveText('3D topology');
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

test('the Maille playground runs examples, shows the tree, and locates errors in the editor', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-langage.html');
  const root = page.locator('[data-maille-playground]');
  const result = root.locator('[data-result]');
  const error = root.locator('[data-error]');
  await root.scrollIntoViewIfNeeded();
  // The first example (genus of a scene) runs on its own.
  await expect(result).toHaveText('- : int = 2', { timeout: 15000 });
  await expect(root.locator('.ast > li > .ast-node .ast-kind')).toHaveText('let');

  await root.getByRole('button', { name: 'Polymorphism' }).click();
  await expect(result).toHaveText('- : int = 42');

  const editor = root.getByLabel('Maille program');
  await editor.fill('let f x = x + 1 in\nf 41');
  await editor.press('Control+Enter');
  await expect(result).toHaveText('- : int = 42');
  // A tree node puts the cursor at its position: "x + 1" starts at line 1, column 11.
  await root.getByRole('button', { name: /^binop \+/ }).click();
  expect(await editor.evaluate((e) => e.selectionStart)).toBe(12);

  await root.getByRole('button', { name: 'Type error' }).click();
  await expect(error).toContainText('Type error (line 2, column 8): this expression has type string');
  await error.getByRole('button', { name: 'Go to the error' }).click();
  expect(await editor.evaluate((e) => e.value.slice(e.selectionStart, e.selectionStart + 5))).toBe('"two"');

  await root.getByRole('button', { name: 'Syntax error' }).click();
  await expect(error).toContainText('Syntax error (line 1, column 9)');
  await expect(root.locator('[data-tree] li')).toHaveCount(0);

  await editor.fill('let rec f x = f x in f 0');
  await editor.press('Control+Enter');
  await expect(error).toContainText('recursion deeper than 1000 calls');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('the LaTeX editor renders the article live, and its diagnostics and outline lead to the right place', async ({ page }, info) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-latex.html');
  const root = page.locator('[data-latex-editor]');
  const preview = root.locator('[data-preview]');
  const status = root.locator('[data-status]');
  const mobile = !info.project.name.startsWith('desktop');
  await expect(preview.locator('.tex-title h3')).toHaveText('A portfolio generated by AI, reviewed and delivered properly');
  await expect(preview.locator('.katex-tag')).toHaveCount(3);
  await expect(status).toHaveText('No diagnostics.');

  // The outline scrolls the preview to the last section.
  await root.locator('.outline-link').last().click();
  await expect.poll(() => preview.evaluate((e) => e.scrollTop)).toBeGreaterThan(0);

  if (mobile) await root.getByRole('button', { name: 'Source', exact: true }).click();
  const source = root.getByLabel('LaTeX source');
  await source.fill('\\section{Mesh}\\label{m}\nSee \\ref{m} and $\\chi = V - E + F$.\n\\ref{nope}');
  if (mobile) await root.getByRole('button', { name: 'Preview', exact: true }).click();
  // The preview redraws after a short pause in typing: wait for the old article to go first.
  await expect(preview.locator('h4')).toHaveCount(1);
  await expect(preview.locator('h4')).toHaveText('1 Mesh');
  await expect(preview.locator('.katex')).toHaveCount(1);
  await expect(status).toHaveText('0 error(s), 1 warning(s): click to go to the line.');
  await root.locator('.diag').click();
  // \ref{nope} starts line 3, column 1; the diagnostic brings the source back into view.
  await expect(source).toBeVisible();
  expect(await source.evaluate((e) => e.value.slice(e.selectionStart, e.selectionStart + 10))).toBe('\\ref{nope}');

  await source.fill('{unclosed');
  await expect(status).toHaveText('1 error(s), 0 warning(s): click to go to the line.');
  await expect(root.locator('.diag-error')).toContainText('line 1, column 1: unclosed { : missing }');

  await root.getByRole('button', { name: 'Article in French' }).click();
  await expect(root.getByRole('button', { name: 'Article in French' })).toHaveAttribute('aria-pressed', 'true');
  if (mobile) await root.getByRole('button', { name: 'Preview', exact: true }).click();
  await expect(preview.locator('.tex-title h3')).toHaveText('Un portfolio généré par l\'IA, relu et livré proprement');
  await expect(preview.locator('.tex-abstract-title')).toHaveText('Résumé');

  const download = page.waitForEvent('download');
  await root.getByRole('button', { name: 'Download the .tex' }).click();
  expect((await download).suggestedFilename()).toBe('portfolio.fr.tex');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('the generalized maps course: darts, alpha moves, orbits and the quiz', async ({ page }) => {
  // About 13 s alone in Firefox (axe on a page with 48 SVG darts); more under the load of 5 browsers.
  test.setTimeout(60000);
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-gcartes.html');
  const figure = page.locator('[data-gmap-figure]');
  const status = figure.locator('[data-status]');
  await expect(figure.locator('[data-counts]')).toHaveText('Counted as orbits: 48 darts, 8 vertices, 12 edges, 6 faces, χ = 2.');

  // The decomposition shows one step at a time; the last one has the 16 darts of two squares.
  const decompose = page.locator('[data-decompose]');
  await expect(decompose.locator('[data-step]:visible')).toHaveCount(1);
  await expect(decompose.locator('[data-step="0"]')).toBeVisible();
  await decompose.getByRole('button', { name: '3. Cut by α0' }).click();
  await expect(decompose.locator('[data-step="3"]')).toBeVisible();
  await expect(decompose.locator('[data-step="0"]')).toBeHidden();
  await expect(decompose.locator('[data-step="3"] .gm-dart-line')).toHaveCount(16);
  await expect(decompose.getByRole('button', { name: '3. Cut by α0' })).toHaveAttribute('aria-pressed', 'true');

  await figure.locator('[data-dart="0"]').click();
  await expect(status).toHaveText('Dart 0: face Top, vertex e.');
  await expect(figure.locator('.gm-link')).toHaveCount(3);
  // alpha0 changes vertex, alpha1 and alpha2 keep it.
  await figure.getByRole('button', { name: 'Apply α0' }).click();
  await expect(status).toContainText('vertex f');
  await figure.getByRole('button', { name: 'Apply α1' }).click();
  await expect(status).toContainText('vertex f');
  await figure.getByRole('button', { name: 'Apply α2' }).click();
  await expect(status).toContainText('vertex f');
  await expect(status).not.toContainText('face Top');

  await figure.getByRole('button', { name: 'Vertex ⟨α1, α2⟩' }).click();
  await expect(figure.locator('.gm-dart.in-orbit')).toHaveCount(6);
  await figure.getByRole('button', { name: 'Edge ⟨α0, α2⟩' }).click();
  await expect(figure.locator('.gm-dart.in-orbit')).toHaveCount(4);
  await figure.getByRole('button', { name: 'Face ⟨α0, α1⟩' }).click();
  await expect(figure.locator('.gm-dart.in-orbit')).toHaveCount(8);
  await figure.getByRole('button', { name: 'All ⟨α0, α1, α2⟩' }).click();
  await expect(figure.locator('.gm-dart.in-orbit')).toHaveCount(48);
  await figure.getByRole('button', { name: 'All ⟨α0, α1, α2⟩' }).click();
  await expect(figure.locator('.gm-dart.in-orbit')).toHaveCount(0);

  const quiz = page.locator('[data-quiz]');
  await quiz.getByRole('radio', { name: '48' }).check();
  await quiz.getByRole('radio', { name: '⟨α1, α2⟩' }).check();
  await quiz.getByRole('radio', { name: 'an isolated vertex' }).check();
  await quiz.getByRole('button', { name: 'Check my answers' }).click();
  await expect(quiz.locator('[data-score]')).toHaveText('2 right answer(s) out of 6.');
  await expect(quiz.locator('[data-feedback]').nth(0)).toHaveText('Right.');
  await expect(quiz.locator('[data-feedback]').nth(2)).toHaveText('Not quite.');
  await expect(quiz.locator('[data-feedback]').nth(3)).toHaveText('No answer.');
  await expect(quiz.locator('[data-explain]').nth(0)).toBeVisible();

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('Othello: play a move, the AI answers, keyboard, undo, and playing white', async ({ page }, info) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-othello.html');
  const root = page.locator('[data-othello]');
  const status = root.locator('[data-status]');
  const cell = (name) => root.locator(`[data-sq="${'abcdefgh'.indexOf(name[0]) + 8 * (Number(name[1]) - 1)}"]`);
  await root.scrollIntoViewIfNeeded();
  await expect(status).toHaveText('Your turn.');
  await expect(root.locator('[data-score]')).toHaveText('Black 2, white 2');
  await expect(root.locator('.oth-cell.is-legal')).toHaveCount(4);
  await expect(cell('d3')).toHaveAttribute('aria-label', 'd3, empty, possible move');

  // Touch targets: every square at least 44 px wide, whatever the screen.
  const width = await cell('a1').evaluate((e) => e.getBoundingClientRect().width);
  expect(width).toBeGreaterThanOrEqual(44);
  // And at 375 px, the narrowest screen the site supports, without horizontal scroll.
  if (info.project.name === 'desktop-chromium') {
    const size = page.viewportSize();
    await page.setViewportSize({ width: 375, height: 800 });
    expect(await cell('a1').evaluate((e) => e.getBoundingClientRect().width)).toBeGreaterThanOrEqual(44);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
    await page.setViewportSize(size);
  }

  await cell('a1').click(); // not a legal move: nothing happens
  await expect(root.locator('[data-score]')).toHaveText('Black 2, white 2');
  await cell('d3').click();
  await expect(cell('d3')).toHaveAttribute('data-disc', 'black');
  await expect(status).toHaveText(/^The AI played [a-h][1-8]\. Your turn\.$/);
  await expect(root.locator('.oth-cell.is-last')).toHaveCount(1);

  // Keyboard: the board has one tab stop, the arrows move it.
  await cell('d3').focus();
  await page.keyboard.press('ArrowDown');
  await expect(cell('d4')).toBeFocused();
  await expect(cell('d4')).toHaveAttribute('tabindex', '0');

  await root.getByRole('button', { name: 'Undo my move' }).click();
  await expect(root.locator('[data-score]')).toHaveText('Black 2, white 2');
  await expect(status).toHaveText('Your turn.');
  await expect(root.getByRole('button', { name: 'Undo my move' })).toBeDisabled();

  // Playing white: the AI opens.
  await root.getByRole('radio', { name: 'white', exact: true }).check();
  await root.getByRole('button', { name: 'New game' }).click();
  await expect(status).toHaveText(/^The AI played (d3|c4|f5|e6)\. Your turn\.$/);
  await expect(root.locator('[data-score]')).toHaveText('Black 4, white 1');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('tic-tac-toe: play against the AI from the move book, keyboard, a whole game, and playing O', async ({ page }, info) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-morpion.html');
  const root = page.locator('[data-morpion]');
  const status = root.locator('[data-status]');
  const cell = (i) => root.locator(`[data-cell="${i}"]`);
  await root.scrollIntoViewIfNeeded();
  await expect(status).toHaveText('Your turn.');
  await expect(root.locator('.ttt-cell.is-legal')).toHaveCount(9);
  await expect(cell(4)).toHaveAttribute('aria-label', 'row 2, column 2, empty');

  // Touch targets of at least 44 px, also at 375 px without horizontal scroll.
  expect(await cell(0).evaluate((e) => e.getBoundingClientRect().width)).toBeGreaterThanOrEqual(44);
  if (info.project.name === 'desktop-chromium') {
    const size = page.viewportSize();
    await page.setViewportSize({ width: 375, height: 800 });
    expect(await cell(0).evaluate((e) => e.getBoundingClientRect().width)).toBeGreaterThanOrEqual(44);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)).toBeLessThanOrEqual(1);
    await page.setViewportSize(size);
  }

  // A corner: the only answer that does not lose is the centre, so the book makes the AI take it.
  await cell(0).click();
  await expect(cell(0)).toHaveAttribute('data-mark', 'X');
  await expect(status).toHaveText('The AI played row 2, column 2. Your turn.');
  await expect(cell(4)).toHaveAttribute('data-mark', 'O');
  await expect(cell(4)).toHaveAttribute('aria-label', 'row 2, column 2, O, last move');
  await cell(4).click(); // taken: nothing happens
  await expect(root.locator('[data-mark="X"]')).toHaveCount(1);

  // Keyboard: one tab stop, the arrows move it, Enter plays.
  await cell(0).focus();
  await page.keyboard.press('ArrowRight');
  await expect(cell(1)).toBeFocused();
  await expect(cell(1)).toHaveAttribute('tabindex', '0');

  // Play the first free square until the end: the unbeatable AI wins or draws, never loses.
  for (let turn = 0; turn < 5; turn++) {
    const text = await status.textContent();
    if (text.includes('Game over')) break;
    const free = root.locator('.ttt-cell.is-legal').first();
    await free.focus();
    await page.keyboard.press('Enter');
    await expect(status).toHaveText(/(Your turn|Game over)/);
  }
  await expect(status).toHaveText(/Game over: (the AI wins|a draw)\.$/);
  await expect(root.locator('.ttt-cell.is-legal')).toHaveCount(0);

  // Playing O: the AI opens.
  await root.getByRole('radio', { name: 'O', exact: true }).check();
  await root.getByRole('button', { name: 'New game' }).click();
  await expect(status).toHaveText(/^The AI played row [1-3], column [1-3]\. Your turn\.$/);
  await expect(root.locator('[data-mark="X"]')).toHaveCount(1);

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  expect(errors).toEqual([]);
});

test('the text adventure plays in the page: commands, quick buttons, history, a won game', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/en/project-aventure.html');
  const root = page.locator('[data-adventure]');
  const log = root.locator('[data-log]');
  const input = root.getByLabel('Your command');
  await expect(log).toContainText('The lab at night');
  await expect(input).toBeEnabled();

  await root.getByRole('button', { name: 'talk' }).click();
  await expect(log).toContainText('> talk');
  await expect(log).toContainText('umbrella');

  for (const c of ['n', 'e', 'take badge', 'w', 'n', 'take umbrella', 's', 'up']) {
    await input.fill(c);
    await input.press('Enter');
  }
  await expect(log).toContainText('You swipe your badge');
  for (let i = 0; i < 10 && !(await log.textContent()).includes('shuts down'); i++) {
    await input.fill('attack');
    await input.press('Enter');
  }
  for (const c of ['take key', 'down', 's', 's']) {
    await input.fill(c);
    await input.press('Enter');
  }
  await expect(log).toContainText('You win!');

  // Arrow up brings back the last command.
  await input.press('ArrowUp');
  await expect(input).toHaveValue('s');
  await root.getByRole('button', { name: 'New game' }).click();
  await expect(log).not.toContainText('You win!');

  const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(1);
  expect(errors).toEqual([]);
});

test('the risk register sorts by number, probability, impact and score, and back', async ({ page }) => {
  await page.goto('/en/method.html');
  const table = page.locator('[data-risks]');
  const ids = () => table.locator('tbody tr').evaluateAll((rows) => rows.map((r) => r.cells[0].textContent.trim()));
  const column = (key) => table.locator('tbody tr').evaluateAll((rows, k) => rows.map((r) => Number(r.dataset[k])), key);
  expect(await ids()).toEqual(['R1', 'R2', 'R3', 'R4', 'R5', 'R6', 'R7', 'R8', 'R9']);

  for (const [key, name] of [['score', 'Score'], ['p', 'Probability'], ['i', 'Impact']]) {
    await table.getByRole('button', { name }).click();
    const values = await column(key);
    expect(values).toEqual([...values].sort((a, b) => b - a));
    await expect(table.locator(`th:has([data-sort="${key}"])`)).toHaveAttribute('aria-sort', 'descending');
    await table.getByRole('button', { name }).click();
    expect(await column(key)).toEqual([...values].sort((a, b) => a - b));
    await expect(table.locator(`th:has([data-sort="${key}"])`)).toHaveAttribute('aria-sort', 'ascending');
  }
  // Highest score first, ties in the order of their numbers.
  await table.getByRole('button', { name: 'Score' }).click();
  expect((await ids()).slice(0, 3)).toEqual(['R2', 'R3', 'R1']);

  await table.getByRole('button', { name: 'No.' }).click();
  expect(await ids()).toEqual(['R1', 'R2', 'R3', 'R4', 'R5', 'R6', 'R7', 'R8', 'R9']);
  await table.getByRole('button', { name: 'No.' }).click();
  expect((await ids())[0]).toBe('R9');
  await expect(table.getByRole('img', { name: 'Probability 3, impact 3: score 9, high risk' })).toHaveCount(2);
});

test('the research page shows experience and education side by side on a wide screen', async ({ page }, info) => {
  await page.goto('/en/research.html');
  const exp = page.locator('[data-timeline="experience"]');
  const edu = page.locator('[data-timeline="education"]');
  await expect(exp.locator('li')).toHaveCount(7);
  await expect(page.getByRole('link', { name: 'The thesis on theses.fr' })).toHaveAttribute('href', /theses\.fr/);
  const [a, b] = [await exp.boundingBox(), await edu.boundingBox()];
  if (info.project.name.startsWith('desktop')) expect(Math.abs(a.y - b.y)).toBeLessThan(2);
  else expect(b.y).toBeGreaterThan(a.y + a.height - 1);
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
    const trackEl = row.querySelector('.gantt-track');
    const t = trackEl.getBoundingClientRect();
    return [[b.left, b.width], [t.left, t.width, Number(getComputedStyle(trackEl).getPropertyValue('--sprints'))]];
  });
  // The number of sprints comes from the page (24 since D25), not from the test.
  expect(track[2]).toBeGreaterThan(10);
  expect(Math.abs(bar[0] - (track[0] + (9 * track[1]) / track[2]))).toBeLessThan(2);
});

test('project filters show the chosen category and technology', async ({ page }) => {
  await page.goto('/fr/projects.html');
  const groups = page.getByRole('group', { name: 'Catégorie' });
  const techs = page.getByRole('group', { name: 'Techno' });
  const visible = page.locator('[data-filterable] .card:visible');
  const shown = (key) => visible.evaluateAll((els, k) => els.map((e) => e.dataset[k]), key);

  await groups.getByRole('button', { name: 'Web' }).click();
  await expect(visible.first()).toBeVisible();
  for (const group of await shown('group')) expect(group).toBe('web');
  await expect(page.locator('[data-games]')).toBeHidden();
  // "Jeux" shows the six games and their section, nothing else.
  await groups.getByRole('button', { name: 'Jeux' }).click();
  await expect(page.locator('[data-games]')).toBeVisible();
  await expect(visible).toHaveCount(6);
  expect(new Set(await shown('group'))).toEqual(new Set(['games']));

  // Both filters apply: the games written in Java.
  await techs.getByRole('button', { name: 'Java', exact: true }).click();
  await expect(visible).toHaveCount(2);
  for (const l of await shown('techs')) expect(l.split('|')).toContain('Java');
  // A technology alone, over every category: C++ (topology, Qt viewer, parallel computing).
  await groups.getByRole('button', { name: 'Tous' }).click();
  await techs.getByRole('button', { name: 'C++', exact: true }).click();
  for (const l of await shown('techs')) expect(l.split('|')).toContain('C++');
  await expect(page.locator('[data-games]')).toBeHidden();
  // No project at all: a notice says so.
  await groups.getByRole('button', { name: 'Jeux' }).click();
  await expect(visible).toHaveCount(0);
  await expect(page.locator('[data-filter-empty]')).toBeVisible();
  await techs.getByRole('button', { name: 'Tous' }).click();
  await groups.getByRole('button', { name: 'Tous' }).click();
  await expect(page.locator('[data-games]')).toBeVisible();
  await expect(page.locator('[data-filter-empty]')).toBeHidden();
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
