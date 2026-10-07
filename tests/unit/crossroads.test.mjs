// The Ada crossroads on its project page (D46): the automaton exported by the Ada program, replayed by
// crossroads-core.js, must give back the program's own simulation, line for line.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { Crossroads, simulate, validate } from '../../src/assets/crossroads-core.js';

const data = (name) => readFileSync(join(ROOT, 'projects/ada/data', name), 'utf8');
const automaton = JSON.parse(data('automaton.json'));

test('replaying the automaton gives the Ada program\'s simulation, line for line', () => {
  const expected = data('simulation-120-4.txt').replace(/\r\n/g, '\n').trimEnd().split('\n');
  assert.deepEqual(simulate(automaton, 120, 4), expected);
});

test('the automaton is valid, starts like the controller and never opens both axes', () => {
  validate(automaton);
  assert.deepEqual(automaton.timings, { ns_green: 30, ew_green: 20, yellow: 3, all_red: 2, min_green: 10 });
  const s0 = automaton.states[0];
  assert.deepEqual([s0.phase, s0.elapsed, s0.lights, s0.pending], ['NS_GREEN', 0, 'GR', [false, false]]);
  for (const s of automaton.states) assert.ok(s.lights.includes('R'), JSON.stringify(s));
});

test('a request shortens the other green to 10 s, is shown as pending, then served', () => {
  const c = new Crossroads(automaton);
  for (let i = 0; i < 4; i++) c.tick();
  c.request('ew');
  assert.deepEqual(c.state.pending, [false, true]);
  while (c.state.phase === 'NS_GREEN') c.tick();
  assert.equal(c.time, 10, 'north-south green ends at the minimum');
  while (c.state.phase !== 'EW_GREEN') c.tick();
  assert.deepEqual(c.state.pending, [false, false], 'served when east-west turns green');
  // A request on the axis that is green changes nothing.
  const before = c.index;
  c.request('ew');
  assert.equal(c.index, before);
});

test('the history keeps the last seconds only, for the timing diagram', () => {
  const c = new Crossroads(automaton, 5);
  for (let i = 0; i < 12; i++) c.tick();
  assert.deepEqual(c.history.map((h) => h.t), [7, 8, 9, 10, 11]);
  c.reset();
  assert.deepEqual([c.index, c.time, c.history.length], [0, 0, 0]);
});

test('a broken automaton is refused before the page trusts it', () => {
  const state = { phase: 'NS_GREEN', elapsed: 0, lights: 'GR', pending: [false, false], tick: 0, request: [0, 0] };
  assert.throws(() => validate({ states: [] }), /empty/);
  assert.throws(() => validate({ states: [{ ...state, tick: 1 }] }), /out of range/);
  assert.throws(() => validate({ states: [{ ...state, lights: 'GG' }] }), /lights/);
  assert.doesNotThrow(() => validate({ states: [state] }));
});
