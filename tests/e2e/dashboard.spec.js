import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// The React dashboard (D43), as published under dashboard/, reading the site's API at ../api/v1/.
for (const theme of ['dark', 'light']) {
  for (const lang of ['fr', 'en']) {
    for (const view of ['projects', 'sprints', 'results']) {
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
