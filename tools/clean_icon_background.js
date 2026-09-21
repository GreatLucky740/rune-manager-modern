const sharp = require('sharp');

async function clean(source, destination) {
  const { data, info } = await sharp(source).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width: w, height: h } = info;
  const seen = new Uint8Array(w * h);
  const queue = [];
  const pale = i => {
    const p = i * 4, r = data[p], g = data[p + 1], b = data[p + 2], a = data[p + 3];
    return a < 18 || (r > 195 && g > 195 && b > 195 && Math.max(r, g, b) - Math.min(r, g, b) < 42);
  };
  const add = i => { if (!seen[i] && pale(i)) { seen[i] = 1; queue.push(i); } };
  for (let x = 0; x < w; x++) { add(x); add((h - 1) * w + x); }
  for (let y = 0; y < h; y++) { add(y * w); add(y * w + w - 1); }
  for (let q = 0; q < queue.length; q++) {
    const i = queue[q], x = i % w, y = Math.floor(i / w);
    if (x) add(i - 1); if (x + 1 < w) add(i + 1); if (y) add(i - w); if (y + 1 < h) add(i + w);
  }
  for (let i = 0; i < seen.length; i++) if (seen[i]) data[i * 4 + 3] = 0;
  // Remove the pale antialias fringe immediately touching the extracted background.
  for (let pass = 0; pass < 2; pass++) {
    const clear = [];
    for (let y = 1; y < h - 1; y++) for (let x = 1; x < w - 1; x++) {
      const i = y * w + x, p = i * 4;
      if (data[p + 3] === 0) continue;
      const r = data[p], g = data[p + 1], b = data[p + 2];
      const nearClear = data[p - 1] === 0 || data[p + 7] === 0 || data[p - 4 * w + 3] === 0 || data[p + 4 * w + 3] === 0;
      if (nearClear && r > 175 && g > 175 && b > 175 && Math.max(r, g, b) - Math.min(r, g, b) < 55) clear.push(p + 3);
    }
    for (const alpha of clear) data[alpha] = 0;
  }
  // Keep the main connected symbol and discard isolated screenshot specks.
  const visited = new Uint8Array(w * h), components = [];
  for (let start = 0; start < w * h; start++) {
    if (visited[start] || data[start * 4 + 3] < 24) continue;
    const component = [], pending = [start]; visited[start] = 1;
    for (let q = 0; q < pending.length; q++) {
      const i = pending[q], x = i % w, y = Math.floor(i / w); component.push(i);
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
        if (!dx && !dy) continue; const nx = x + dx, ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const n = ny * w + nx; if (!visited[n] && data[n * 4 + 3] >= 24) { visited[n] = 1; pending.push(n); }
      }
    }
    components.push(component);
  }
  components.sort((a, b) => b.length - a.length);
  const keep = new Uint8Array(w * h), minimum = components.length ? Math.max(12, components[0].length * .012) : 0; for (const component of components) if (component.length >= minimum) for (const i of component) keep[i] = 1;
  for (let i = 0; i < w * h; i++) if (!keep[i]) data[i * 4 + 3] = 0;
  await sharp(data, { raw: { width: w, height: h, channels: 4 } })
    .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .resize(128, 128, { fit: 'contain', kernel: sharp.kernel.lanczos3, background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .extend({ top: 8, bottom: 8, left: 8, right: 8, background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 })
    .toFile(destination);
}

clean(process.argv[2], process.argv[3]).catch(error => { console.error(error); process.exit(1); });
