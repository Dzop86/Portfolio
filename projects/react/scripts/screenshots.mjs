// Screenshots of the dashboard for the project page: the results view (dark theme) and the 3D viewer
// (light theme), in French and English, 1280 x 800, written to the site's src/assets/images/.
// Needs the site served with the dashboard built (npm run build at the root, then npm run serve):
//   node projects/react/scripts/screenshots.mjs [http://localhost:4173]
import { join } from 'node:path';
import { chromium } from '@playwright/test';

const base = process.argv[2] ?? 'http://localhost:4173';
const out = join(import.meta.dirname, '../../../src/assets/images');
const SHOTS = [
  { name: 'results', theme: 'dark', ready: (page) => page.locator('[data-bar]').first().waitFor() },
  { name: 'viewer', theme: 'light', ready: (page, lang) => page.getByRole('heading', { name: lang === 'fr' ? 'Invariants de Tore' : 'Invariants of Torus' }).waitFor() },
];

const browser = await chromium.launch();
for (const lang of ['fr', 'en']) {
  for (const shot of SHOTS) {
    const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
    await page.addInitScript((t) => localStorage.setItem('theme', t), shot.theme);
    await page.goto(`${base}/dashboard/?lang=${lang}#${shot.name}`);
    await shot.ready(page, lang);
    // The tabs and the start of the view, as a visitor lands on them.
    await page.locator('nav.tabs').scrollIntoViewIfNeeded();
    await page.evaluate(() => window.scrollTo(0, document.querySelector('nav.tabs').getBoundingClientRect().top + window.scrollY - 16));
    await page.waitForTimeout(300);
    await page.screenshot({ path: join(out, `react-${shot.name}-${lang}.png`) });
    await page.close();
    console.log(`react-${shot.name}-${lang}.png`);
  }
}
await browser.close();
