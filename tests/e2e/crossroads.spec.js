import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// The Ada crossroads on its project page (D46): the automaton computed by the Ada program, replayed live.
const lit = (page, axis) => page.locator(`[data-light="${axis}"] .cross-lamp.is-on`).getAttribute('data-lamp');

for (const lang of ['fr', 'en']) {
  test(`crossroads ${lang}: the lights run, are accessible and fit the screen`, async ({ page }) => {
    const errors = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await page.goto(`/${lang}/project-ada.html`);
    await expect(page.locator('[data-request="ns"]')).toBeEnabled();
    expect(await lit(page, 'ns')).toBe('G');
    expect(await lit(page, 'ew')).toBe('R');
    await expect(page.locator('[data-note]')).toContainText(/\d+ (états|states)/);
    // The clock moves on its own.
    await expect(page.locator('[data-clock]')).toContainText(/t = [1-9]/, { timeout: 5_000 });

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(1);
    const a11y = await new AxeBuilder({ page }).include('[data-crossroads]').withTags(['wcag2a', 'wcag2aa']).analyze();
    expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
    expect(errors).toEqual([]);
  });
}

test('crossroads: a request on east-west, followed second by second while paused', async ({ page }) => {
  await page.goto('/en/project-ada.html');
  const pause = page.getByRole('button', { name: 'Pause' });
  await expect(pause).toBeEnabled();
  await pause.click();
  await page.getByRole('button', { name: 'Start again' }).click();
  await expect(page.locator('[data-clock]')).toContainText('t = 0 s');

  const request = page.getByRole('button', { name: 'A car waits east-west' });
  await request.click();
  await expect(request).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('[data-car="ew"]')).toBeVisible();
  await expect(page.locator('[data-crossroads] [data-live]')).toHaveText('A car waits east-west: request recorded, served at this axis\'s next green.');
  // On the axis that is green, a request changes nothing, and the page says so.
  await page.getByRole('button', { name: 'A car waits north-south' }).click();
  await expect(page.getByRole('button', { name: 'A car waits north-south' })).toHaveAttribute('aria-pressed', 'false');
  await expect(page.locator('[data-crossroads] [data-live]')).toContainText('already green');

  // With the keyboard: ten seconds of green north-south at most, then amber.
  const step = page.getByRole('button', { name: 'One second' });
  await step.focus();
  for (let i = 0; i < 10; i++) await page.keyboard.press('Enter');
  await expect(page.locator('[data-clock]')).toContainText('t = 10 s');
  expect(await lit(page, 'ns')).toBe('Y');
  for (let i = 0; i < 5; i++) await page.keyboard.press('Enter');
  expect(await lit(page, 'ew')).toBe('G');
  await expect(request).toHaveAttribute('aria-pressed', 'false');
  await expect(page.locator('[data-car="ew"]')).toBeHidden();
  // Fifteen seconds in the timing diagram, two rows each, never green on both rows.
  await expect(page.locator('.cross-bar')).toHaveCount(30);
  expect(await page.locator('.cross-map').getAttribute('aria-label')).toBe('Crossroads: north-south on red, east-west on green.');
});
