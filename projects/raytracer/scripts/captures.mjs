// Renders the project page's high-definition captures with the native engine (rt_render) and writes them as
// PNG to src/assets/images/. Usage: node projects/raytracer/scripts/captures.mjs [path/to/rt_render]
// (default: projects/raytracer/build/rt_render, built in Release).
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { deflateSync, crc32 } from 'node:zlib';

const ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const samples = join(ROOT, 'projects/topologie/samples');

// name -> rt_render options; one each for the three materials and for a light the visitor can set.
export const CAPTURES = {
  spheres: ['--scene', 'spheres'],
  'torus-glass': ['--scene', 'mesh', '--mesh', join(samples, 'torus.obj'), '--finish', 'glass'],
  'mobius-metal': ['--scene', 'mesh', '--mesh', join(samples, 'mobius.obj'), '--finish', 'metal', '--fuzz', '0.02'],
  'saddle-sunset': ['--scene', 'mesh', '--mesh', join(samples, 'saddle.obj'), '--light-azimuth', '60', '--light-elevation', '18', '--kelvin', '2600'],
};

/** RGB bytes of a binary PPM (P6, 255). */
export function readPpm(data) {
  const header = data.subarray(0, 32).toString('latin1').match(/^P6\s+(\d+)\s+(\d+)\s+255\s/);
  if (!header) throw new Error('not a binary PPM');
  return { width: Number(header[1]), height: Number(header[2]), rgb: data.subarray(header[0].length) };
}

/** A PNG (8-bit RGB, no filter) of the given pixels: signature, IHDR, IDAT, IEND. */
export function encodePng({ width, height, rgb }) {
  const chunk = (type, body) => {
    const out = Buffer.alloc(12 + body.length);
    out.writeUInt32BE(body.length, 0);
    out.write(type, 4, 'latin1');
    body.copy(out, 8);
    out.writeUInt32BE(crc32(out.subarray(4, 8 + body.length)), 8 + body.length);
    return out;
  };
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr.set([8, 2, 0, 0, 0], 8); // 8 bits, RGB, deflate, no filter method choice, no interlace
  const raw = Buffer.alloc((width * 3 + 1) * height);
  for (let y = 0; y < height; y++) Buffer.from(rgb.buffer, rgb.byteOffset + y * width * 3, width * 3).copy(raw, y * (width * 3 + 1) + 1);
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr),
    chunk('IDAT', deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const renderer = process.argv[2] ?? join(ROOT, 'projects/raytracer/build/rt_render');
  const work = mkdtempSync(join(tmpdir(), 'rt-captures-'));
  try {
    for (const [name, options] of Object.entries(CAPTURES)) {
      const ppm = join(work, `${name}.ppm`);
      execFileSync(renderer, [...options, '--size', '640x360', '--spp', '256', '-o', ppm], { stdio: 'inherit' });
      const png = join(ROOT, 'src/assets/images', `raytracer-${name}.png`);
      writeFileSync(png, encodePng(readPpm(readFileSync(ppm))));
      console.log(`written ${png}`);
    }
  } finally {
    rmSync(work, { recursive: true, force: true });
  }
}
