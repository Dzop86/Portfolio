import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const PAGES = ['index', 'projects', 'research', 'method', 'contact'];

for (const lang of ['fr', 'en']) {
  for (const page of PAGES) {
    test(`${lang}/${page}: loads, is accessible and has no horizontal scroll`, async ({ page: p }) => {
      const errors = [];
      p.on('pageerror', (e) => errors.push(e.message));
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

test('theme toggle switches between light and dark', async ({ page }) => {
  await page.goto('/fr/index.html');
  const before = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
  await page.getByRole('button', { name: 'Changer de thème' }).click();
  const after = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
  expect(after).not.toBe(before);
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
