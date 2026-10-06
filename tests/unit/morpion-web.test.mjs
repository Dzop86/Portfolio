// The page's tic-tac-toe (D34): rules rewritten in JavaScript, AI read from the move book that the Python
// program of projects/morpion computes. These tests check that both agree, and that the AI, playing from the
// book through the JavaScript rules, never loses.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { START, aiMove, isOver, legalMoves, loadBook, play, rowCol, toMove, winner, winningLine } from '../../src/assets/morpion-api.js';

const book = JSON.parse(readFileSync(join(ROOT, 'projects/morpion/data/book.json'), 'utf8'));

function reachable() {
  const seen = new Set([START]);
  const queue = [START];
  while (queue.length) {
    const pos = queue.shift();
    for (const cell of legalMoves(pos)) {
      const next = play(pos, cell);
      if (!seen.has(next)) {
        seen.add(next);
        queue.push(next);
      }
    }
  }
  return seen;
}

test('rules: turns, wins, draws and illegal moves', () => {
  assert.equal(toMove(START), 'X');
  assert.equal(toMove('X........'), 'O');
  assert.equal(winner('XXXOO....'), 'X');
  assert.deepEqual(winningLine('OXX.O.X.O'), [0, 4, 8]);
  assert.equal(winner('XOXXOOOXX'), null);
  assert.ok(isOver('XOXXOOOXX'));
  assert.deepEqual(legalMoves('XXXOO....'), []);
  assert.throws(() => play('X........', 0));
  assert.deepEqual(rowCol(5), { row: 2, col: 3 });
});

test('the JavaScript rules and the Python book see the same 5,478 positions and the same endings', () => {
  const positions = reachable();
  assert.equal(positions.size, 5478);
  assert.deepEqual(new Set(Object.keys(book.positions)), positions);
  for (const pos of positions) {
    const entry = book.positions[pos];
    if (isOver(pos)) assert.equal(entry, winner(pos) ?? '=', pos);
    else assert.ok(Array.isArray(entry) && entry.slice(1).every((c) => legalMoves(pos).includes(c)), pos);
  }
  assert.deepEqual(book.positions[START], [0, 0, 1, 2, 3, 4, 5, 6, 7, 8]);
});

test('the unbeatable AI never loses, as X or as O, against every possible opponent', () => {
  // Every game where the opponent tries every move and the AI tries each of its best moves.
  function losses(pos, ai) {
    if (isOver(pos)) return winner(pos) !== null && winner(pos) !== ai ? 1 : 0;
    const moves = toMove(pos) === ai ? book.positions[pos].slice(1) : legalMoves(pos);
    return moves.reduce((sum, cell) => sum + losses(play(pos, cell), ai), 0);
  }
  assert.equal(losses(START, 'X'), 0);
  assert.equal(losses(START, 'O'), 0);
});

test('the AI picks among the best moves, or any move as a beginner', () => {
  assert.equal(aiMove(book, 'XX.OO....', 'unbeatable'), 2); // win at once
  assert.equal(aiMove(book, 'X........', 'unbeatable'), 4); // the centre against a corner
  const seen = new Set();
  let n = 0;
  for (let i = 0; i < 9; i++) seen.add(aiMove(book, START, 'beginner', () => (n++ % 9) / 9));
  assert.deepEqual([...seen].sort(), [0, 1, 2, 3, 4, 5, 6, 7, 8]);
  assert.throws(() => aiMove(book, 'XXXOO....', 'unbeatable'));
});

test('the book loads through fetch, and a failed request is an error', async () => {
  const ok = await loadBook('book.json', async () => ({ ok: true, json: async () => book }));
  assert.equal(ok, book);
  await assert.rejects(loadBook('book.json', async () => ({ ok: false, status: 404 })));
});
