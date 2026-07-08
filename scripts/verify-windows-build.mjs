import assert from 'node:assert/strict';
import { access, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const exe = path.join(root, 'dist', 'software', 'windows', 'GroceryBmiShopping.exe');
const data = path.join(root, 'dist', 'software', 'windows', 'Data', 'dishes.json');
await access(exe);
const json = JSON.parse(await readFile(data, 'utf8'));
assert.ok(json.dishes.length >= 30, 'published software data should include the dish pool');
assert.ok(json.budgetTiers.some((tier) => tier.value === 30));
assert.ok(json.budgetTiers.some((tier) => tier.value === 50));
assert.ok(json.budgetTiers.some((tier) => tier.value === 100));
console.log(`Native Windows software artifact verified: ${exe}`);
