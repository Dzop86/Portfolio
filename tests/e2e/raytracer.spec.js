import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// The ray tracer on its project page (D45): the C++ engine in WebAssembly, in a web worker.
const samples = (page) => page.locator('[data-raytracer]').getAttribute('data-samples').then((s) => Number(s ?? 0));

/** How many distinct colours the canvas shows: a blank or uniform image has one. */
const colours = (page) => page.locator('[data-raytracer] canvas').evaluate((c) => {
  const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data;
  const seen = new Set();
  for (let i = 0; i < d.length; i += 4 * 97) seen.add((d[i] << 16) | (d[i + 1] << 8) | d[i + 2]);
  return seen.size;
});

for (const lang of ['fr', 'en']) {
  test(`raytracer ${lang}: renders pass after pass, is accessible and fits the screen`, async ({ page }) => {
    const errors = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await page.goto(`/${lang}/project-raytracer.html`);
    await expect.poll(() => samples(page), { timeout: 30_000 }).toBeGreaterThanOrEqual(2);
    expect(await colours(page)).toBeGreaterThan(20);
    await expect(page.locator('[data-progress]')).toContainText(lang === 'fr' ? 'passes sur 256' : 'of 256 passes');
    await expect(page.locator('#rt-finish')).toBeDisabled(); // the spheres have their own materials

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    expect(overflow).toBeLessThanOrEqual(1);
    const a11y = await new AxeBuilder({ page }).include('[data-raytracer]').withTags(['wcag2a', 'wcag2aa']).analyze();
    expect(a11y.violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
    expect(errors).toEqual([]);
  });
}

test('raytracer: a mesh scene in glass, orbited with the keyboard, then paused', async ({ page }) => {
  await page.goto('/en/project-raytracer.html');
  await expect.poll(() => samples(page), { timeout: 30_000 }).toBeGreaterThanOrEqual(1);

  const image = () => page.locator('[data-raytracer] canvas').evaluate((c) => c.toDataURL());
  const spheres = await image();
  await page.getByLabel('Scene').selectOption('torus');
  await expect(page.getByLabel('Mesh material')).toBeEnabled({ timeout: 30_000 });
  await page.getByLabel('Mesh material').selectOption('glass');
  await expect(page.locator('[data-raytracer] canvas')).toHaveAttribute('aria-label', /Torus, glass/, { timeout: 30_000 });
  await expect.poll(() => samples(page), { timeout: 30_000 }).toBeGreaterThanOrEqual(2);
  // The count starts again with the new scene: these passes are the torus's, not the spheres'.
  expect(await image()).not.toBe(spheres);

  // Moving the camera starts again from the first pass.
  const yaw = page.getByLabel('Rotation');
  const before = await yaw.inputValue();
  await yaw.focus();
  await page.keyboard.press('ArrowRight');
  await expect(yaw).not.toHaveValue(before);
  await expect(page.locator('[data-view-value="yaw"]')).toHaveText(`${await yaw.inputValue()}°`);
  await expect.poll(() => samples(page), { timeout: 30_000 }).toBeGreaterThanOrEqual(1);

  // Another number of workers, or another finish, keeps the camera where it is and starts again.
  const turned = await yaw.inputValue();
  await page.getByLabel('Workers (CPU cores)').selectOption('1');
  await expect(page.locator('[data-progress]')).toContainText('· 1 worker(s)', { timeout: 30_000 });
  await expect(yaw).toHaveValue(turned);
  await page.getByLabel('Mesh material').selectOption('metal');
  await expect(page.locator('[data-raytracer] canvas')).toHaveAttribute('aria-label', /Torus, metal/, { timeout: 30_000 });
  await expect(yaw).toHaveValue(turned);
  await expect.poll(() => samples(page), { timeout: 30_000 }).toBeGreaterThanOrEqual(1);

  const pause = page.getByRole('button', { name: 'Pause' });
  await pause.click();
  await expect(page.getByRole('button', { name: 'Resume' })).toHaveAttribute('aria-pressed', 'true');
  const frozen = await samples(page);
  await page.waitForTimeout(1500);
  // At most the pass that was under way when the pause arrived.
  expect(await samples(page)).toBeLessThanOrEqual(frozen + 1);

  await page.getByRole('button', { name: 'Default view' }).click();
  await expect(page.getByRole('button', { name: 'Pause' })).toHaveAttribute('aria-pressed', 'false');
});
