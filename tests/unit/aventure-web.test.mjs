// Integration test of the text adventure compiled to JavaScript by TeaVM (src/assets/wasm/aventure.js):
// the browser build plays whole games in both languages, as the Java tests do.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import * as game from '../../src/assets/wasm/aventure.js';

const play = (commands) => commands.map((c) => game.respond(c));

test('the module exports the three entry points', () => {
  for (const name of ['start', 'respond', 'over']) assert.equal(typeof game[name], 'function', name);
});

test('a whole game is won in French, accents and articles included', () => {
  assert.match(game.start('fr', 7), /^Le laboratoire de nuit/);
  play(['nord', 'est', 'prendre le badge', 'ouest', 'nord', 'prendre le parapluie', 'sud', 'haut']);
  let blows = 0;
  while (!game.respond('attaquer').includes("s'éteint")) assert.ok(++blows < 10, 'the robot goes down within 10 blows');
  assert.equal(game.respond('prendre la Clé'), 'Vous prenez la clé USB.');
  play(['bas', 'sud']);
  assert.match(game.respond('sud'), /Gagné !$/);
  assert.equal(game.over(), true);
});

test('a whole game is won in English, and restart starts again', () => {
  game.start('en', 3);
  assert.equal(game.over(), false);
  assert.match(game.respond('up'), /^You cannot go that way|^The server room door/);
  play(['n', 'e', 'take badge', 'w', 'n', 'take umbrella', 's', 'up']);
  let blows = 0;
  while (!game.respond('attack').includes('shuts down')) assert.ok(++blows < 10);
  play(['take key', 'down', 's']);
  assert.match(game.respond('s'), /You win!$/);
  assert.match(game.respond('restart'), /^The lab at night/);
  assert.equal(game.over(), false);
});

test('unknown commands and the guard', () => {
  game.start('en', 1);
  assert.equal(game.respond('dance'), 'I do not understand "dance". Type "help".');
  assert.match(game.respond('talk'), /badge.*umbrella/s);
});
