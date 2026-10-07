// Builds the site's static API (src/api.mjs at the repository root) once for the whole test run, and
// writes it where the tests read it: the same files as dist/api/v1/, without building the site.
import { mkdirSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

export default async function globalSetup() {
  const { buildApi } = await import('../../../src/api.mjs');
  const { loadData } = await import('../../../src/lib.mjs');
  const dir = join(tmpdir(), `portfolio-api-${process.pid}`);
  mkdirSync(dir, { recursive: true });
  for (const [file, body] of Object.entries(buildApi(loadData()))) writeFileSync(join(dir, file), JSON.stringify(body));
  process.env.PORTFOLIO_API_DIR = dir;
}
