// Integration test of the committed WebAssembly build of the Othello engine (projects/othello), through
// the page's wrapper: it must count positions and play like the native build.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { BLACK, WHITE, aiMove, loadOthello, pass, perft, play, reset, squareName, state, undo } from '../../src/assets/othello-api.js';

const lib = await loadOthello((await import('../../src/assets/wasm/othello.js')).default);

test('perft matches the published counts, as in the native build', () => {
  const expected = [1, 4, 12, 56, 244, 1396, 8200, 55092];
  expected.forEach((n, depth) => assert.equal(perft(lib, depth), n, `depth ${depth}`));
});

test('the starting position and its four legal moves', () => {
  reset(lib);
  const s = state(lib);
  assert.equal(s.toMove, BLACK);
  assert.deepEqual([s.black, s.white, s.plies], [2, 2, 0]);
  assert.deepEqual(s.legal.flatMap((ok, sq) => (ok ? [squareName(sq)] : [])), ['d3', 'c4', 'f5', 'e6']);
});

test('a move flips, an illegal one is refused, undo restores', () => {
  reset(lib);
  assert.equal(play(lib, 0), false, 'a1 is not a legal opening');
  assert.equal(play(lib, 19), true, 'd3');
  let s = state(lib);
  assert.deepEqual([s.black, s.white, s.toMove, s.plies], [4, 1, WHITE, 1]);
  assert.equal(pass(lib), false, 'white has moves: no pass');
  assert.equal(undo(lib), true);
  s = state(lib);
  assert.deepEqual([s.black, s.white, s.toMove, s.plies], [2, 2, BLACK, 0]);
  assert.equal(undo(lib), false, 'nothing left to undo');
});

test('two AIs play a whole game to the end, passes included, and the discs add up', () => {
  reset(lib);
  let moves = 0;
  while (!state(lib).over) {
    const s = state(lib);
    if (s.mustPass) assert.equal(pass(lib), true);
    else assert.equal(play(lib, aiMove(lib, s.toMove === BLACK ? 2 : 3)), true);
    assert.ok(++moves < 130, 'the game ends');
  }
  const s = state(lib);
  assert.ok(s.black + s.white <= 64 && s.black + s.white > 4);
  assert.equal(aiMove(lib, 3), -1, 'no move at the end');
});

test('the AI is deterministic', () => {
  reset(lib);
  play(lib, 19);
  assert.equal(aiMove(lib, 4), aiMove(lib, 4));
});
