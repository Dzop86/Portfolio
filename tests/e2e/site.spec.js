import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PAGES = ['index', 'projects', 'research', 'method', 'contact', 'project-vitrine', 'project-lib-c'];

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
