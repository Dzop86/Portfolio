import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // URLs as #/results: GitHub Pages serves the one index.html, with no rewriting of other paths.
    provideRouter(routes, withHashLocation()),
    provideHttpClient(withFetch()),
  ],
};
