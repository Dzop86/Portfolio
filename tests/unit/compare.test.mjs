// The comparison of the React and Angular dashboards (D44), as measured and as shown on the project page.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';
import { readComparison } from '../../src/templates.mjs';

test('the comparison has every measure for both frameworks', () => {
  const c = readComparison();
  for (const side of [c.react, c.angular]) {
    assert.match(side.version, /^\d+\.\d+\.\d+$/);
    for (const v of [side.buildMs, side.bundle.initial.gzip, side.bundle.initial.raw, side.lines, side.tests, side.packages]) assert.ok(v > 0);
    assert.ok(side.bundle.initial.gzip < side.bundle.initial.raw, 'compressed is smaller');
  }
  assert.ok(c.shared.lines > 0 && c.shared.files.length > 0);
  assert.match(c.machine.date, /^\d{4}-\d{2}-\d{2}$/);
});

test('the Angular project page shows the measures and links to both dashboards, in each language', () => {
  const out = build(mkdtempSync(join(tmpdir(), 'compare-')));
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-angular.html'), 'utf8');
    const table = html.slice(html.indexOf('data-compare'), html.indexOf('</table>', html.indexOf('data-compare')));
    assert.equal((table.match(/<tr data-row=/g) || []).length, 6);
    assert.ok(html.includes(`href="../angular/?lang=${lang}" data-dashboard="angular"`));
    assert.ok(html.includes(`href="../dashboard/?lang=${lang}"`));
    assert.ok(html.includes(`href="../angular/?lang=${lang}" data-link="demo"`));
    // Both screenshots of the Angular dashboard, in the page's language, with a description.
    for (const name of ['projects', 'results']) {
      const img = `angular-${name}-${lang}.png`;
      assert.match(html, new RegExp(`<img src="\\.\\./assets/images/${img}" width="1280" height="800" loading="lazy" alt="[^"]{40,}">`), img);
      assert.ok(existsSync(join(out, 'assets/images', img)), img);
    }
  }
});
