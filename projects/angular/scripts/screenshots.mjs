// Screenshots of the Angular dashboard for the project page: the projects view (dark theme) and the results
// view (light theme), in French and English, 1280 x 800, written to the site's src/assets/images/.
// Needs the site served with this dashboard built (npm run build here and at the root, then npm run serve):
//   node projects/angular/scripts/screenshots.mjs [http://localhost:4173]
// Playwright comes from the site's own dependencies, as for projects/react/scripts/screenshots.mjs.
import { join } from 'node:path';
import { chromium } from '@playwright/test';

const base = process.argv[2] ?? 'http://localhost:4173';
const out = join(import.meta.dirname, '../../../src/assets/images');
const SHOTS = [
  { name: 'projects', theme: 'dark', ready: (page) => page.locator('[data-project]').first().waitFor() },
  { name: 'results', theme: 'light', ready: (page) => page.locator('[data-table="parallel"]').waitFor() },
];

const browser = await chromium.launch();
for (const lang of ['fr', 'en']) {
  for (const shot of SHOTS) {
    const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
    await page.addInitScript((t) => localStorage.setItem('theme', t), shot.theme);
    await page.goto(`${base}/angular/?lang=${lang}#/${shot.name}`);
    await shot.ready(page);
    // The tabs and the start of the view, as a visitor lands on them.
    await page.evaluate(() => window.scrollTo(0, document.querySelector('nav.tabs').getBoundingClientRect().top + window.scrollY - 16));
    await page.waitForTimeout(300);
    await page.screenshot({ path: join(out, `angular-${shot.name}-${lang}.png`) });
    await page.close();
    console.log(`angular-${shot.name}-${lang}.png`);
  }
}
await browser.close();
