import assert from 'node:assert/strict';
import { dishes, regions, marketPrices } from '../src/data/dishes.js';
import { assertNoDailyDuplicates, calculateBMI, calculateDishBMIValue, estimateDishCostFromMarket, generateBudgetPlan, getIngredientMarketPrice, getMealEnergyTarget } from '../src/model.js';

assert.equal(calculateBMI(170, 65), 22.5, 'BMI calculation should match expected value');
assert.ok(dishes.length >= 80, 'dish style pool should contain nationwide breakfast/lunch/dinner and regional cuisines');
assert.ok(dishes.some((dish) => dish.meals.includes('breakfast')), 'dish pool should include breakfast items');
assert.ok(regions.length >= 7, 'regional selector should include nationwide regional groups');
assert.ok(marketPrices.length >= 150, 'market price table should include standard retail unit prices for common ingredients');
for (const ingredient of new Set(dishes.flatMap((dish) => dish.ingredients))) {
  const price = getIngredientMarketPrice(ingredient, 'national');
  assert.ok(price.activePrice > 0, `ingredient ${ingredient} should have a usable retail unit price`);
}
assert.ok(getMealEnergyTarget(22, 'breakfast') < getMealEnergyTarget(22, 'lunch'), 'breakfast energy target should be lower than lunch');

const expectedCuisines = ['川菜', '湘菜', '粤菜', '鲁菜', '苏菜', '浙菜', '闽菜', '徽菜', '东北菜', '西北菜'];
for (const cuisine of expectedCuisines) {
  assert.ok(dishes.some((dish) => dish.cuisine === cuisine), `dish pool should include ${cuisine}`);
}
for (const region of ['national', 'north', 'east', 'south', 'central', 'southwest', 'northwest']) {
  assert.ok(dishes.some((dish) => (dish.regions || ['national']).includes(region)), `dish pool should include region ${region}`);
}
for (const dish of dishes) {
  const score = calculateDishBMIValue(dish, { heightCm: 170, weightKg: 65 }, dish.meals[0]);
  const marketCost = estimateDishCostFromMarket(dish, 'national');
  assert.ok(score >= 0 && score <= 100, `${dish.name} BMI fit score should be 0-100`);
  assert.ok(marketCost > 0, `${dish.name} should have positive market-price cost estimate`);
}

for (const budget of [30, 50, 90, 120]) {
  for (const region of ['national', 'north', 'east', 'south', 'central', 'southwest', 'northwest']) {
    const plan = generateBudgetPlan({ budget, heightCm: 170, weightKg: 65, people: 1, days: 3, region });
    assert.equal(plan.dailyPlans.length, 3, `manual budget ${budget} should generate requested days`);
    assert.equal(plan.region, region, 'plan should retain selected region');
    assert.ok(assertNoDailyDuplicates(plan), `manual budget ${budget}/${region} should not repeat meals within a day`);
    assert.ok(plan.shoppingList.length > 0, `manual budget ${budget}/${region} should produce a shopping list`);
    assert.ok(plan.shoppingList.every((item) => item.estimatedCost > 0 && item.unitPrice > 0 && item.estimatedKg > 0), 'shopping list should include actual retail cost estimate fields');
    for (const day of plan.dailyPlans) {
      assert.ok(day.breakfast.length > 0 && day.lunch.length > 0 && day.dinner.length > 0, `day ${day.day} should include breakfast, lunch and dinner`);
      assert.ok(day.total.cost <= budget * 1.6, `day ${day.day} should stay near manual budget`);
    }
  }
}

console.log('All model tests passed.');
