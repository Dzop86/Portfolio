import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// The React dashboard (D43), as published under dashboard/, reading the site's API at ../api/v1/.
for (const theme of ['dark', 'light']) {
  for (const lang of ['fr', 'en']) {
    for (const view of ['projects', 'sprints', 'results', 'viewer']) {
      test(`dashboard ${lang} #${view} (${theme}): loads its data, is accessible and has no horizontal scroll`, async ({ page }) => {
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        await page.addInitScript((t) => localStorage.setItem('theme', t), theme);
        await page.goto(`/dashboard/?lang=${lang}#${view}`);
        await expect(page.locator('html')).toHaveAttribute('lang', lang);
        await expect(page.locator('main h2').first()).toBeVisible();
        await expect(page.getByRole('alert')).toHaveCount(0);

        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
        expect(overflow).toBeLessThanOrEqual(1);

        const a11y = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
        expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
        expect(errors).toEqual([]);
      });
    }
  }
}

test('dashboard: the views are links, the language switch keeps the view', async ({ page }) => {
  await page.goto('/dashboard/?lang=fr');
  await expect(page.locator('[data-project]').first()).toBeVisible();
  await page.getByRole('link', { name: 'Résultats' }).click();
  await expect(page).toHaveURL(/#results$/);
  await expect(page.locator('[data-bar]')).toHaveCount(5);
  await page.getByRole('link', { name: 'Read the dashboard in English' }).click();
  await expect(page).toHaveURL(/\?lang=en#results$/);
  await expect(page.getByRole('heading', { name: 'Results', level: 2 })).toBeVisible();
});

test('dashboard: a project card leads to its page on the site', async ({ page }) => {
  await page.goto('/dashboard/?lang=en');
  await page.locator('[data-project="parallele"]').getByRole('link', { name: 'Project page' }).click();
  await expect(page).toHaveURL(/\/en\/project-parallele\.html$/);
});

test('dashboard: touch targets are at least 44 px high', async ({ page }) => {
  await page.goto('/dashboard/?lang=fr');
  await expect(page.locator('[data-project]').first()).toBeVisible();
  const small = await page.evaluate(() =>
    [...document.querySelectorAll('header a, header button, nav a, select, .links a')]
      .filter((el) => el.getBoundingClientRect().height < 44)
      .map((el) => el.textContent));
  expect(small).toEqual([]);
});

test('dashboard viewer: topologie in WebAssembly reads the samples and a broken file', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/dashboard/?lang=en#viewer');
  const field = (key) => page.locator(`[data-field="${key}"]`);
  // The torus first: genus 1, total curvature 0.
  await expect(page.getByRole('heading', { name: 'Invariants of Torus' })).toBeVisible();
  await expect(field('genus')).toHaveText('1');
  await expect(field('total')).toHaveText('0 × 2π');
  await page.getByRole('button', { name: 'Möbius strip' }).click();
  await expect(field('orientable')).toHaveText('no');
  await expect(field('boundary')).toHaveText('1');
  // Without WebGL (some headless browsers) the canvas gives way to a notice; the invariants still work.
  const canvas = page.locator('canvas[data-viewer]');
  if (await canvas.count()) await expect(canvas).toHaveAttribute('aria-label', /Möbius strip/);
  else await expect(page.locator('[data-nowebgl]')).toContainText('WebGL');
  await page.getByLabel('Open an OBJ or PLY file').setInputFiles({
    name: 'broken.obj', mimeType: 'text/plain', buffer: Buffer.from('v 0 0 0\nv 1 0 0\nf 1 2 9\n'),
  });
  await expect(page.locator('[data-viewer-error]')).toHaveText('This file could not be read (line 3): a face refers to a missing vertex.');
  expect(errors).toEqual([]);
});

test('dashboard viewer: hovering the shape reads the curvature under the pointer', async ({ page, browserName, isMobile }) => {
  test.skip(isMobile, 'no hover on a touch screen');
  await page.goto('/dashboard/?lang=fr#viewer');
  await expect(page.getByRole('heading', { name: 'Invariants de Tore' })).toBeVisible();
  const canvas = page.locator('canvas[data-viewer]');
  test.skip(!(await canvas.count()), `no WebGL in headless ${browserName}`);
  const box = await canvas.boundingBox();
  // A point of the ring, left of the hole in the middle: the camera's vertical field is fixed, so the
  // ring's size on screen follows the canvas height (0.08 to 0.35 of it from the centre). hover() scrolls
  // the canvas into view first; the shape joins the scene just after the invariants show, so the pointer
  // moves again until it is there.
  const x = box.width / 2 - 0.22 * box.height;
  const y = box.height / 2;
  await expect(async () => {
    await canvas.hover({ position: { x, y } });
    await canvas.hover({ position: { x: x + 2, y } });
    await expect(page.locator('.viewer-tip')).toContainText(/K = -?\d/, { timeout: 500 });
  }).toPass();
});
