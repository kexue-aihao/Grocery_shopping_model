import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { budgetTiers, dishes, regions, sourceNotes, marketPrices } from '../src/data/dishes.js';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const payload = JSON.stringify({ budgetTiers, regions, dishes, sourceNotes, marketPrices }, null, 2);
const outputs = [
  path.join(root, 'desktop', 'GroceryBmi.App', 'Data', 'dishes.json'),
  path.join(root, 'android_flutter', 'grocery_bmi_app', 'assets', 'data', 'dishes.json')
];

for (const file of outputs) {
  await mkdir(path.dirname(file), { recursive: true });
  await writeFile(file, payload, 'utf8');
  console.log(`Exported app data: ${file}`);
}

