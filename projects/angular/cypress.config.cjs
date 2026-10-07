// Cypress against the site as served (npm run build at the root with the dashboards built, then
// npm run serve): the Angular dashboard lives at /angular/ and reads /api/v1/.
const { defineConfig } = require('cypress');

module.exports = defineConfig({
  e2e: {
    baseUrl: process.env.CYPRESS_BASE_URL || 'http://localhost:4173',
    specPattern: 'cypress/e2e/**/*.cy.ts',
    supportFile: 'cypress/support/e2e.ts',
    video: false,
    screenshotOnRunFailure: true,
  },
});
