#!/usr/bin/env python3
"""The village's music (sprint 69), written by this script rather than taken from a pack: Kenney's
sound packs have jingles but no loop. Eight bars of a quiet arpeggio on I-vi-IV-V in C, a bass and a
pentatonic tune drawn from a fixed seed, in plucked tones; the notes that ring past the end go on at
its start, so that it loops without a seam. Pure Python (wave, math, random): the same file on every run.

    python3 tools/music_gen.py godot/assets/music/village.wav
"""
import math
import random
import struct
import sys
import wave

RATE = 22050
BPM = 84
BEAT = 60 / BPM
BARS = 8

# Each bar's chord as MIDI notes: C, A minor, F, G, twice.
CHORDS = [[48, 52, 55], [45, 48, 52], [41, 45, 48], [43, 47, 50]] * 2
PENTATONIC = [60, 62, 64, 67, 69, 72, 74, 76]


def hz(note):
    return 440 * 2 ** ((note - 69) / 12)


def pluck(buf, start, note, length, volume, decay):
    """A soft plucked tone: a sine and its octave, a short attack, an exponential fall."""
    f = hz(note)
    first = int(start * RATE)
    count = int(length * RATE)
    for i in range(count):
        t = i / RATE
        attack = min(1.0, t / 0.01)
        envelope = attack * math.exp(-t * decay)
        # Fades to nothing at the end of the note, so that nothing is cut.
        envelope *= min(1.0, (count - i) / (0.05 * RATE))
        # Past the end, the note goes on at the start: the loop has no seam.
        buf[(first + i) % len(buf)] += volume * envelope * (math.sin(2 * math.pi * f * t) + 0.3 * math.sin(4 * math.pi * f * t))


def main(path):
    rng = random.Random(69)
    total = int(BARS * 4 * BEAT * RATE)
    buf = [0.0] * total
    for bar, chord in enumerate(CHORDS):
        t0 = bar * 4 * BEAT
        pluck(buf, t0, chord[0] - 12, 4 * BEAT, 0.22, 1.2)
        # Eighth notes up and down the chord.
        arpeggio = chord + [chord[0] + 12] + chord[::-1][:3] + [chord[1]]
        for k, note in enumerate(arpeggio):
            pluck(buf, t0 + k * BEAT / 2, note + 12, BEAT * 1.5, 0.08, 3.0)
        # A tune of quarter notes near the last one, resting now and then.
        at = rng.randrange(len(PENTATONIC))
        for beat in range(4):
            if rng.random() < 0.3:
                continue
            at = max(0, min(len(PENTATONIC) - 1, at + rng.choice([-2, -1, 1, 2])))
            pluck(buf, t0 + beat * BEAT, PENTATONIC[at], BEAT * 1.8, 0.12, 2.0)
    peak = max(abs(s) for s in buf) or 1.0
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(b"".join(struct.pack("<h", int(32767 * 0.8 * s / peak)) for s in buf))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "godot/assets/music/village.wav")
