import { mkdir, readdir, readFile, rm, stat, writeFile, copyFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const dist = path.join(root, 'dist');

function ensureInsideRoot(target) {
  const resolved = path.resolve(target);
  if (!resolved.startsWith(root)) {
    throw new Error(`Refusing to write outside workspace: ${resolved}`);
  }
  return resolved;
}

async function copyDir(from, to) {
  await mkdir(to, { recursive: true });
  for (const entry of await readdir(from)) {
    const source = path.join(from, entry);
    const target = path.join(to, entry);
    const info = await stat(source);
    if (info.isDirectory()) await copyDir(source, target);
    else await copyFile(source, target);
  }
}

async function stampHtml(file) {
  const html = await readFile(file, 'utf8');
  const stamped = html.replace('</body>', `  <!-- build-time: ${new Date().toISOString()} -->\n  </body>`);
  await writeFile(file, stamped, 'utf8');
}

ensureInsideRoot(dist);
await rm(dist, { recursive: true, force: true });
await mkdir(dist, { recursive: true });
await copyFile(path.join(root, 'index.html'), path.join(dist, 'index.html'));
await copyFile(path.join(root, 'manifest.webmanifest'), path.join(dist, 'manifest.webmanifest'));
await copyFile(path.join(root, 'sw.js'), path.join(dist, 'sw.js'));
await copyFile(path.join(root, 'README.md'), path.join(dist, 'README.md'));
await copyDir(path.join(root, 'src'), path.join(dist, 'src'));
await copyDir(path.join(root, 'assets'), path.join(dist, 'assets'));
await stampHtml(path.join(dist, 'index.html'));
console.log(`Build complete: ${dist}`);
