// The Ada crossroads on its project page (D46): the automaton is computed by the Ada program
// (`carrefour --automaton`, projects/ada/data/automaton.json); this module only follows its transitions.
// Shared by the page (adaplay.js) and the tests.

export const AXES = { ns: 0, ew: 1 };

/** A controller replayed from the automaton: the current state, the time, and the lights of each second. */
export class Crossroads {
  constructor(automaton, keep = 60) {
    this.states = automaton.states;
    this.keep = keep; // seconds of history kept for the timing diagram
    this.reset();
  }

  reset() {
    this.index = 0; // state 0 is Start
    this.time = 0;
    this.history = [];
  }

  get state() {
    return this.states[this.index];
  }

  /** A car or pedestrian waits on `axis` ('ns' or 'ew'): the transition the Ada Request_Crossing gives. */
  request(axis) {
    this.index = this.state.request[AXES[axis]];
  }

  /** One second passes: the lights of this second go to the history, then the Ada Tick transition. */
  tick() {
    this.history.push({ t: this.time, lights: this.state.lights });
    if (this.history.length > this.keep) this.history.shift();
    this.index = this.state.tick;
    this.time += 1;
  }
}

/**
 * What `carrefour SECONDS EW_REQUEST_TIME` prints, rebuilt from the automaton: the tests compare it with the
 * Ada program's own output, line for line.
 */
export function simulate(automaton, seconds, requestAt = -1) {
  const c = new Crossroads(automaton);
  const lines = ['  t  NS EW  phase'];
  for (let t = 0; t <= seconds; t++) {
    if (t === requestAt) {
      c.request('ew');
      lines.push('     -- east-west request --');
    }
    const { lights, phase } = c.state;
    lines.push(`${t < 10 ? '  ' : t < 100 ? ' ' : ''} ${t}  ${lights[0]}  ${lights[1]}  ${phase}`);
    c.tick();
  }
  return lines;
}

/** Checks the automaton before the page trusts it: indices in range, lights matching the phase. */
export function validate(automaton) {
  const n = automaton.states?.length ?? 0;
  if (n === 0) throw new Error('empty automaton');
  const ok = (i) => Number.isInteger(i) && i >= 0 && i < n;
  automaton.states.forEach((s, i) => {
    if (!ok(s.tick) || !ok(s.request?.[0]) || !ok(s.request?.[1])) throw new Error(`state ${i}: transition out of range`);
    if (!/^[RYG]{2}$/.test(s.lights) || !s.lights.includes('R')) throw new Error(`state ${i}: lights ${s.lights}`);
  });
  return automaton;
}
