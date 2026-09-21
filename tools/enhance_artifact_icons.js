const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const dir = path.resolve('outputs/artifact_manager_app/assets/artifacts');
(async () => {
  for (const name of fs.readdirSync(dir).filter(x => x.endsWith('.png'))) {
    const source = path.join(dir, name);
    const temp = path.join(dir, name.replace('.png', '.enhanced.png'));
    await sharp(source)
      .trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .resize(128, 128, { fit: 'contain', kernel: sharp.kernel.lanczos3, background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .sharpen({ sigma: 0.7, m1: 0.4, m2: 0.2 })
      .extend({ top: 8, bottom: 8, left: 8, right: 8, background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png({ compressionLevel: 9 })
      .toFile(temp);
    fs.renameSync(temp, source);
  }
})().catch(error => { console.error(error); process.exit(1); });
