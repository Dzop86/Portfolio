// Jest with jest-preset-angular, without zone.js (the application is zoneless, D44).
module.exports = {
  preset: 'jest-preset-angular',
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
  // The integration tests read the API that the site really builds: written once here (src/api.mjs).
  globalSetup: '<rootDir>/tests/global-setup.mjs',
  testMatch: ['<rootDir>/tests/**/*.spec.ts'],
  // A cold Windows runner can take seconds for the first number formatted in French (see projects/react).
  testTimeout: 20000,
};
