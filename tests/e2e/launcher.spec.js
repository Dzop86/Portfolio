// Osmose's launcher page (projects/rpg/launcher/ui) in a browser, with Tauri's commands played by a
// stand-in: the page alone is checked here, the commands by cargo test (launcher/core).
import { test, expect } from '@playwright/test';
import { pathToFileURL } from 'node:url';
import { join } from 'node:path';

const PAGE = pathToFileURL(join(import.meta.dirname, '../../projects/rpg/launcher/ui/index.html')).href;

/**
 * Opens the page with a stand-in for window.__TAURI__: `start` answers `start`, `update_game`
 * waits for the test to release it, `connect` refuses the passwords in `refused`. Every call is
 * recorded in window.calls.
 */
async function open(page, { start, refused = {} }) {
  await page.addInitScript(({ start, refused }) => {
    window.calls = [];
    let release;
    window.updated = new Promise((resolve) => { release = resolve; });
    window.releaseUpdate = () => release();
    const listeners = {};
    window.__TAURI__ = {
      core: {
        invoke: async (command, args) => {
          window.calls.push([command, args ?? null]);
          if (command === 'start') return start;
          if (command === 'update_game') {
            listeners.progress?.({ payload: [3e6, 6e6, 2, 4] });
            await window.updated;
            return { version: '1.0.12', downloaded: 4, received: 6e6 };
          }
          if (command === 'connect') {
            if (refused[args.password]) throw { code: refused[args.password], detail: 'refused' };
            return null;
          }
          return null;
        },
      },
      event: { listen: async (name, handler) => { listeners[name] = handler; } },
    };
  }, { start, refused });
  await page.goto(PAGE);
}

const commands = (page) => page.evaluate(() => window.calls.map(([c]) => c));

test('the launcher updates at once, signs in, waits for the update, then starts the game (sprint 51)', async ({ page }) => {
  await open(page, { start: { lang: 'fr', name: 'ada', passwordSaved: true, installed: null }, refused: { wrong: 'wrong-password' } });
  // Just a name, a password and "Sign in"; the remembered account filled in.
  await expect(page.getByRole('heading', { name: 'Osmose' })).toBeVisible();
  await expect(page.getByLabel('Nom de compte', { exact: true })).toHaveValue('ada');
  await expect(page.getByLabel('Mot de passe', { exact: true })).toHaveAttribute('placeholder', 'Mot de passe mémorisé');
  await expect(page.getByLabel('Mémoriser le nom de compte')).toBeChecked();
  await expect(page.getByLabel('Mémoriser le mot de passe')).toBeChecked();
  await expect(page.getByRole('button', { name: 'Jouer' })).toHaveCount(0);
  await expect(page.getByLabel('Confirmer le mot de passe')).toBeHidden();
  // The update's bar and line fit in the window (520 x 720).
  await page.setViewportSize({ width: 520, height: 720 });
  await expect(page.locator('#status')).toBeInViewport();
  await expect(page.locator('#status')).toHaveText('Mise à jour : fichier 2 sur 4 · 3,0 Mo sur 6,0 Mo');

  // A wrong password: the refusal, in words.
  await page.getByLabel('Mot de passe', { exact: true }).fill('wrong');
  await page.getByRole('button', { name: 'Se connecter' }).click();
  await expect(page.locator('#message')).toHaveText('Nom inconnu ou mot de passe faux.');

  // The remembered password: signed in, the game waits for the end of the update, then starts.
  await page.getByLabel('Mot de passe', { exact: true }).fill('');
  await page.getByRole('button', { name: 'Se connecter' }).click();
  await expect(page.locator('#status')).toHaveText('Connecté : fin de la mise à jour avant de lancer le jeu…');
  expect(await commands(page)).not.toContain('play');
  await page.evaluate(() => window.releaseUpdate());
  await expect(page.locator('#status')).toHaveText('Le jeu démarre. Bonne partie !');
  expect(await commands(page)).toEqual(['start', 'update_game', 'connect', 'connect', 'play']);
  const connect = await page.evaluate(() => window.calls.filter(([c]) => c === 'connect').at(-1)[1]);
  expect(connect).toEqual({ name: 'ada', password: '', rememberName: true, rememberPassword: true, signUp: false });
});

test('the launcher creates an account, checks the password on the page first, and speaks English (sprint 51)', async ({ page }) => {
  await open(page, { start: { lang: 'en', name: null, passwordSaved: false, installed: '1.0.11' } });
  await expect(page.getByLabel('Account name', { exact: true })).toBeFocused();
  await expect(page.getByLabel('Remember the account name')).not.toBeChecked();
  await page.getByRole('button', { name: 'No account yet? Create one' }).click();
  await expect(page.getByLabel('Confirm the password')).toBeVisible();
  await page.getByLabel('Account name', { exact: true }).fill('margaux');
  await page.getByLabel('Password', { exact: true }).fill('short');
  await page.getByRole('button', { name: 'Create the account and play' }).click();
  await expect(page.locator('#message')).toHaveText('A password has at least 10 characters.');
  await page.getByLabel('Password', { exact: true }).fill('a long new password');
  await page.getByLabel('Confirm the password').fill('another one');
  await page.getByRole('button', { name: 'Create the account and play' }).click();
  await expect(page.locator('#message')).toHaveText('The two passwords differ.');
  expect(await commands(page)).toEqual(['start', 'update_game']);

  await page.getByLabel('Confirm the password').fill('a long new password');
  await page.getByLabel('Remember the account name').check();
  await page.evaluate(() => window.releaseUpdate());
  await expect(page.locator('#status')).toHaveText('Game updated (version 1.0.12, 6.0 MB).');
  await page.getByRole('button', { name: 'Create the account and play' }).click();
  await expect(page.locator('#status')).toHaveText('The game is starting. Have fun!');
  const connect = await page.evaluate(() => window.calls.find(([c]) => c === 'connect')[1]);
  expect(connect).toEqual({ name: 'margaux', password: 'a long new password', rememberName: true, rememberPassword: false, signUp: true });
  // Back to French with one button.
  await page.getByRole('button', { name: 'Français / English' }).click();
  await expect(page.getByRole('button', { name: "J'ai déjà un compte" })).toBeVisible();
});
