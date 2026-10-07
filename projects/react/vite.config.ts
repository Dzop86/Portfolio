// The dashboard is published under the site's dashboard/ folder and reads the API at ../api/v1/ (D43).
// In development, the API comes from the site's last build (run npm run build at the repository root).
import { existsSync, readFileSync } from 'node:fs';
import { join, normalize } from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig, type Plugin } from 'vitest/config';

const SITE_DIST = join(import.meta.dirname, '../../dist');

function siteApi(): Plugin {
  return {
    name: 'site-api',
    configureServer(server) {
      server.middlewares.use('/api/', (req, res, next) => {
        const file = normalize(join(SITE_DIST, 'api', (req.url ?? '').split('?')[0] ?? ''));
        if (!file.startsWith(join(SITE_DIST, 'api')) || !existsSync(file)) return next();
        res.setHeader('Content-Type', 'application/json');
        res.end(readFileSync(file));
      });
    },
  };
}

export default defineConfig({
  base: './',
  plugins: [react(), siteApi()],
  // The 3D view's chunk is three.js (560 kB), loaded only with that view; the rest stays near 250 kB.
  build: { outDir: 'dist', emptyOutDir: true, target: 'es2022', chunkSizeWarningLimit: 600 },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/**/*.test.{ts,tsx}'],
    // A cold Windows runner took 5.2 s for the first number formatted in French (Node 22, its locale
    // data loaded on first use): the 5 s default failed a test that is otherwise instant.
    testTimeout: 20_000,
  },
});
