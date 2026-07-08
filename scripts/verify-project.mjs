import assert from 'node:assert/strict';
import { access, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const requiredFiles = [
  'desktop/GroceryBmi.App/GroceryBmi.App.csproj',
  'desktop/GroceryBmi.App/MainForm.cs',
  'desktop/GroceryBmi.App/GroceryModel.cs',
  'desktop/GroceryBmi.App/Assets/logo.ico',
  'desktop/GroceryBmi.App/Assets/logo.png',
  'android_flutter/grocery_bmi_app/pubspec.yaml',
  'android_flutter/grocery_bmi_app/lib/main.dart',
  'android_flutter/grocery_bmi_app/assets/logo.png',
  'android_flutter/grocery_bmi_app/assets/data/dishes.json',
  '.github/workflows/build-release.yml'
];
for (const file of requiredFiles) await access(path.join(root, file));

const data = JSON.parse(await readFile(path.join(root, 'android_flutter/grocery_bmi_app/assets/data/dishes.json'), 'utf8'));
assert.ok(data.dishes.length >= 80, 'shared dish data should include expanded nationwide home-style cuisine pool');
assert.ok(data.dishes.some((dish) => dish.meals.includes('breakfast')), 'shared data should include breakfast dishes');
assert.ok(data.dishes.every((dish) => Array.isArray(dish.meals)), 'every dish should have meal tags');
assert.ok(data.regions.length >= 7, 'shared data should include region selector options');
assert.ok(data.marketPrices.length >= 150, 'shared data should include public-market retail unit price table');
assert.ok(data.marketPrices.every((p) => p.unit === 'kg' && p.nationalAvg > 0 && p.regions?.national > 0), 'every market price should expose kg retail unit price fields');
for (const cuisine of ['川菜', '湘菜', '粤菜', '鲁菜', '苏菜', '浙菜', '闽菜', '徽菜', '东北菜', '西北菜']) {
  assert.ok(data.dishes.some((dish) => dish.cuisine === cuisine), `shared data should include ${cuisine}`);
}

const csproj = await readFile(path.join(root, 'desktop/GroceryBmi.App/GroceryBmi.App.csproj'), 'utf8');
assert.match(csproj, /<TargetFramework>net8\.0-windows<\/TargetFramework>/, 'Windows app should target .NET 8 for GitHub Actions');
assert.match(csproj, /<ApplicationIcon>Assets\\logo\.ico<\/ApplicationIcon>/, 'Windows executable should embed logo icon');
assert.match(csproj, /Content Include="Assets\\logo\.ico"/, 'Windows publish output should copy logo icon for taskbar/runtime icon');

const mainForm = await readFile(path.join(root, 'desktop/GroceryBmi.App/MainForm.cs'), 'utf8');
assert.match(mainForm, /Icon = new Icon\(iconPath\)/, 'Windows window/taskbar icon should be set from logo.ico at startup');
assert.match(mainForm, /Field\("身高", _height, "cm"\)/, 'Windows height input should show unit after the input');
assert.match(mainForm, /Field\("体重", _weight, "kg"\)/, 'Windows weight input should show unit after the input');
assert.match(mainForm, /new Label \{ Text = unit/, 'Windows field helper should render a post-input unit label');
assert.match(mainForm, /选择地区\/人群/, 'Windows UI should include a region selection button');
assert.match(mainForm, /MealPanel\("🌅 早餐"/, 'Windows plan should render breakfast');
assert.match(mainForm, /GenerateBudgetPlan\(manualBudget, .*_selectedRegionId\)/, 'Windows generation should pass selected region to model');

const csharpModel = await readFile(path.join(root, 'desktop/GroceryBmi.App/GroceryModel.cs'), 'utf8');
assert.match(csharpModel, /meal == "breakfast" \? 380/, 'Windows model should include breakfast energy target');
assert.match(csharpModel, /BuildShoppingList[\s\S]*Breakfast/, 'Windows shopping list should include breakfast dishes');
assert.match(csharpModel, /RegionScore/, 'Windows model should score dishes by selected region');
assert.match(csharpModel, /MarketPrice/, 'Windows model should load market retail price data');
assert.match(csharpModel, /EstimateDishCostFromMarket/, 'Windows model should estimate dish cost from ingredient retail prices');

const index = await readFile(path.join(root, 'index.html'), 'utf8');
assert.match(index, /<em>cm<\/em>/, 'Web height input should show cm after input');
assert.match(index, /<em>kg<\/em>/, 'Web weight input should show kg after input');
assert.match(index, /<em>元\/天<\/em>/, 'Web budget input should show unit after input');

const webApp = await readFile(path.join(root, 'src/app.js'), 'utf8');
assert.match(webApp, /budgetInput/, 'Web companion should use manual budget input');
assert.match(webApp, /regionSelect/, 'Web companion should expose region selector');
assert.doesNotMatch(webApp, /澎湃|鸿蒙/, 'Platform text should be reduced to Windows and Android');

const flutterMain = await readFile(path.join(root, 'android_flutter/grocery_bmi_app/lib/main.dart'), 'utf8');
assert.match(flutterMain, /suffixText: unit/, 'Flutter inputs should display units after values');
assert.match(flutterMain, /DropdownButtonFormField<String>/, 'Flutter app should expose region selector');
assert.match(flutterMain, /🌅 早餐/, 'Flutter app should render breakfast');
assert.match(flutterMain, /Image\.asset\('assets\/logo\.png'/, 'Flutter UI should display logo');
assert.match(flutterMain, /region: _region/, 'Flutter generation should pass selected region');
assert.doesNotMatch(flutterMain, /\\?\\.\\d/, 'Dart source should not contain accidental null-aware decimal syntax such as ?.25');
assert.match(flutterMain, /MarketPrice/, 'Flutter app should parse market retail price data');
assert.match(flutterMain, /estimateDishCost/, 'Flutter app should estimate dish cost from ingredient retail prices');

const workflow = await readFile(path.join(root, '.github/workflows/build-release.yml'), 'utf8');
assert.match(workflow, /flutter build apk --debug/, 'GitHub workflow should build Android APK');
assert.match(workflow, /ncipollo\/release-action@v1/, 'GitHub workflow should create a Release');
console.log('Project structure verified for units, regional cuisines, BMI and Windows + Android workflow.');


