import { cp, mkdir, copyFile, rm } from 'node:fs/promises';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { build } from 'esbuild';

const root = fileURLToPath(new URL('..', import.meta.url));
const dist = join(root, 'dist');
const src = join(root, 'src');
const icons = join(dist, 'icons');

await rm(dist, { recursive: true, force: true });
await mkdir(dist, { recursive: true });
await mkdir(icons, { recursive: true });

await build({
  entryPoints: [join(src, 'content.js')],
  bundle: true,
  format: 'iife',
  target: ['chrome110'],
  outfile: join(dist, 'content.js'),
});

await Promise.all(
  ['manifest.json', 'background.js'].map((file) =>
    copyFile(join(src, file), join(dist, file))
  )
);

await cp(join(src, 'icons'), icons, { recursive: true });
