import { dishes, marketPrices } from './data/dishes.js';

export function round(value, digits = 1) {
  const factor = 10 ** digits;
  return Math.round((Number(value) + Number.EPSILON) * factor) / factor;
}

export function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}


const marketPriceMap = new Map(marketPrices.map((price) => [price.item, price]));

export function getIngredientMarketPrice(ingredient, region = 'national') {
  const direct = marketPriceMap.get(ingredient);
  const fuzzy = direct || marketPrices.find((price) => ingredient.includes(price.item) || price.item.includes(ingredient));
  if (!fuzzy) return { item: ingredient, category: '未分类', unit: 'kg', nationalAvg: 10, regions: { national: 10 }, source: '项目默认估算' };
  const regionalPrice = fuzzy.regions?.[region] ?? fuzzy.regions?.national ?? fuzzy.nationalAvg;
  return { ...fuzzy, activePrice: regionalPrice };
}

export function estimateIngredientQuantityKg(ingredient, dish = {}) {
  const name = String(ingredient);
  const type = dish.type || '';
  if (/酱|醋|椒|葱|姜|蒜|糖|油|咖喱|豉|料|茶|淀粉|蛋清|蘸水|可乐|枸杞|三七|八角/.test(name)) return 0.012;
  if (/花椒|黑胡椒|龙井茶/.test(name)) return 0.004;
  if (/米|面|粉|饭|馍|燕麦|糯|小米|粉丝|粉条|油条|包|薄脆/.test(name)) return type === 'breakfast' ? 0.11 : 0.09;
  if (/牛|羊|猪|肉|排骨|肋排|五花|里脊|腩|鸡|鱼|虾|海蛎|花蛤|金枪/.test(name)) return type === 'soup' ? 0.12 : 0.14;
  if (/豆腐|腐竹|豆花|黄豆|牛奶|鸡蛋|皮蛋/.test(name)) return 0.12;
  if (/蘑|菇|木耳|笋干|海带/.test(name)) return 0.045;
  if (/蓝莓|花生|咸菜|榨菜|萝卜丁|肉松/.test(name)) return 0.04;
  return type === 'vegetable' ? 0.18 : 0.1;
}

export function estimateDishCostFromMarket(dish, region = 'national') {
  const ingredients = dish.ingredients || [];
  const total = ingredients.reduce((sum, ingredient) => {
    const price = getIngredientMarketPrice(ingredient, region).activePrice;
    return sum + price * estimateIngredientQuantityKg(ingredient, dish);
  }, 0);
  return round(Math.max(total, 1.5), 1);
}

function withMarketCost(dish, region = 'national') {
  return { ...dish, baseCost: dish.cost, cost: estimateDishCostFromMarket(dish, region) };
}

export function calculateBMI(heightCm, weightKg) {
  const heightM = Number(heightCm) / 100;
  const weight = Number(weightKg);
  if (!heightM || !weight || heightM <= 0 || weight <= 0) return 0;
  return round(weight / (heightM * heightM), 1);
}

export function getBMICategory(bmi) {
  if (!bmi) return { label: '待输入', tone: 'neutral', advice: '请输入身高体重以生成个性化菜品适配值。' };
  if (bmi < 18.5) return { label: '偏瘦', tone: 'gain', advice: '建议提高优质蛋白与主食比例，避免长期热量不足。' };
  if (bmi < 24) return { label: '正常', tone: 'fit', advice: '建议维持蛋白、蔬菜、主食的均衡搭配。' };
  if (bmi < 28) return { label: '超重', tone: 'control', advice: '建议优先选择低脂高纤菜品，控制油脂与精制主食。' };
  return { label: '肥胖', tone: 'reduce', advice: '建议选择低能量密度、高蛋白、高纤维搭配，并咨询专业人士。' };
}

export function getMealEnergyTarget(bmi, meal = 'lunch') {
  const base = meal === 'breakfast' ? 380 : meal === 'lunch' ? 560 : 520;
  if (!bmi) return base;
  if (bmi < 18.5) return base + (meal === 'breakfast' ? 90 : 130);
  if (bmi < 24) return base;
  if (bmi < 28) return base - (meal === 'breakfast' ? 55 : 90);
  return base - (meal === 'breakfast' ? 80 : 140);
}

export function calculateDishBMIValue(dish, profile = {}, meal = 'lunch') {
  const bmi = profile.bmi || calculateBMI(profile.heightCm, profile.weightKg) || 22;
  const targetKcal = getMealEnergyTarget(bmi, meal);
  const energyFit = clamp(34 - (Math.abs(dish.kcal - targetKcal) / targetKcal) * 34, 0, 34);
  const proteinFit = clamp((dish.protein / (meal === 'breakfast' ? 24 : 32)) * 24, 0, 24);
  const fiberFit = clamp((dish.fiber / (meal === 'breakfast' ? 6 : 8)) * 18, 0, 18);
  const fatPenalty = clamp((dish.fat - (meal === 'breakfast' ? 14 : 18)) * 0.7, 0, 12);
  const vegetableBonus = ['vegetable', 'soup', 'lean-protein', 'breakfast'].includes(dish.type) ? 8 : 3;
  const expectedCost = meal === 'breakfast' ? 9 : 14;
  const budgetFit = clamp(16 - Math.abs(dish.cost - expectedCost) * 0.45, 4, 16);
  const bmiAdjustment = bmi >= 24 && dish.kcal < targetKcal ? 5 : bmi < 18.5 && dish.protein >= 18 ? 5 : 0;
  const score = energyFit + proteinFit + fiberFit + vegetableBonus + budgetFit + bmiAdjustment - fatPenalty;
  return round(clamp(score, 0, 100), 0);
}

export function summarizeCombo(combo, profile = {}, meal = 'lunch') {
  const totals = combo.reduce((acc, dish) => {
    acc.cost += dish.cost;
    acc.kcal += dish.kcal;
    acc.protein += dish.protein;
    acc.fat += dish.fat;
    acc.carbs += dish.carbs;
    acc.fiber += dish.fiber;
    acc.bmiValue += calculateDishBMIValue(dish, profile, meal);
    return acc;
  }, { cost: 0, kcal: 0, protein: 0, fat: 0, carbs: 0, fiber: 0, bmiValue: 0 });
  const len = combo.length || 1;
  return {
    cost: round(totals.cost, 1),
    kcal: round(totals.kcal, 0),
    protein: round(totals.protein, 1),
    fat: round(totals.fat, 1),
    carbs: round(totals.carbs, 1),
    fiber: round(totals.fiber, 1),
    bmiValue: round(totals.bmiValue / len, 0)
  };
}

function combinations(items, minSize, maxSize) {
  const result = [];
  const stack = [];
  function walk(start) {
    if (stack.length >= minSize) result.push([...stack]);
    if (stack.length === maxSize) return;
    for (let i = start; i < items.length; i += 1) {
      stack.push(items[i]);
      walk(i + 1);
      stack.pop();
    }
  }
  walk(0);
  return result;
}

function dishRegions(dish) {
  return dish.regions || ['national'];
}

function regionScore(dish, region = 'national') {
  const regions = dishRegions(dish);
  if (region === 'national') return regions.includes('national') ? 6 : 2;
  if (regions.includes(region)) return 18;
  if (regions.includes('national')) return 8;
  return 0;
}

function comboDiversityScore(combo, region = 'national') {
  const types = new Set(combo.map((dish) => dish.type));
  const cuisines = new Set(combo.map((dish) => dish.cuisine));
  const hasVegetable = combo.some((dish) => dish.type === 'vegetable' || dish.tags.includes('绿色蔬菜') || dish.tags.includes('高纤维'));
  const hasProtein = combo.some((dish) => dish.type.includes('protein') || dish.protein >= 18);
  const hasSoup = combo.some((dish) => dish.type === 'soup');
  const hasStaple = combo.some((dish) => dish.type === 'staple' || dish.type === 'breakfast');
  const regional = combo.reduce((sum, dish) => sum + regionScore(dish, region) / 4, 0);
  return types.size * 5 + cuisines.size * 2 + (hasVegetable ? 12 : 0) + (hasProtein ? 12 : 0) + (hasSoup ? 3 : 0) + (hasStaple ? 3 : 0) + regional;
}

function chooseMealCombo({ meal, targetCost, profile, forbiddenIds = new Set(), daySeed = 0, weeklyUsed = new Map(), region = 'national' }) {
  const maxDishes = meal === 'breakfast' ? 1 : targetCost >= 45 ? 4 : targetCost >= 22 ? 3 : 2;
  const minDishes = meal === 'breakfast' ? 1 : targetCost >= 20 ? 2 : 1;
  const candidates = dishes
    .filter((dish) => dish.meals.includes(meal) && !forbiddenIds.has(dish.id))
    .map((dish) => withMarketCost(dish, region))
    .map((dish) => ({ ...dish, score: calculateDishBMIValue(dish, profile, meal), regionalScore: regionScore(dish, region) }))
    .sort((a, b) => (b.score + b.regionalScore) - (a.score + a.regionalScore) || a.cost - b.cost)
    .slice(0, meal === 'breakfast' ? 24 : 34);

  const allCombos = combinations(candidates, minDishes, maxDishes);
  let best = allCombos[0] || [];
  let bestScore = -Infinity;
  for (const combo of allCombos) {
    const summary = summarizeCombo(combo, profile, meal);
    const costDistance = Math.abs(summary.cost - targetCost);
    const overBudgetPenalty = summary.cost > targetCost * 1.12 ? (summary.cost - targetCost) * 8 : 0;
    const nutritionScore = combo.reduce((sum, dish) => sum + dish.score, 0) / combo.length;
    const diversity = comboDiversityScore(combo, region);
    const repeatPenalty = combo.reduce((sum, dish) => sum + (weeklyUsed.get(dish.id) || 0) * 6, 0);
    const regionalBonus = combo.reduce((sum, dish) => sum + dish.regionalScore, 0);
    const seedNudge = ((combo.reduce((sum, dish) => sum + dish.id.charCodeAt(0), 0) + daySeed) % 9) / 10;
    const score = nutritionScore * 1.2 + diversity + regionalBonus - costDistance * 3.2 - overBudgetPenalty - repeatPenalty + seedNudge;
    if (score > bestScore) {
      bestScore = score;
      best = combo;
    }
  }
  return best.map(({ score, regionalScore, ...dish }) => dish);
}

export function generateBudgetPlan(options = {}) {
  const budget = Number(options.budget || 50);
  const people = clamp(Number(options.people || 1), 1, 6);
  const days = clamp(Number(options.days || 3), 1, 7);
  const region = options.region || 'national';
  const profile = {
    heightCm: Number(options.heightCm || 170),
    weightKg: Number(options.weightKg || 65)
  };
  profile.bmi = calculateBMI(profile.heightCm, profile.weightKg);

  const dailyPlans = [];
  const weeklyUsed = new Map();
  const breakfastRatio = budget <= 30 ? 0.25 : 0.22;
  const lunchRatio = budget <= 30 ? 0.38 : 0.40;

  for (let day = 1; day <= days; day += 1) {
    const breakfastTarget = (budget * breakfastRatio) / people;
    const lunchTarget = (budget * lunchRatio) / people;
    const dinnerTarget = (budget * (1 - breakfastRatio - lunchRatio)) / people;
    const breakfast = chooseMealCombo({ meal: 'breakfast', targetCost: breakfastTarget, profile, daySeed: day * 11, weeklyUsed, region });
    const lunch = chooseMealCombo({ meal: 'lunch', targetCost: lunchTarget, profile, forbiddenIds: new Set(breakfast.map((dish) => dish.id)), daySeed: day * 17, weeklyUsed, region });
    const forbidden = new Set([...breakfast, ...lunch].map((dish) => dish.id));
    const dinner = chooseMealCombo({ meal: 'dinner', targetCost: dinnerTarget, profile, forbiddenIds: forbidden, daySeed: day * 31, weeklyUsed, region });

    for (const dish of [...breakfast, ...lunch, ...dinner]) {
      weeklyUsed.set(dish.id, (weeklyUsed.get(dish.id) || 0) + 1);
    }

    const breakfastSummary = summarizeCombo(breakfast, profile, 'breakfast');
    const lunchSummary = summarizeCombo(lunch, profile, 'lunch');
    const dinnerSummary = summarizeCombo(dinner, profile, 'dinner');
    const total = {
      cost: round((breakfastSummary.cost + lunchSummary.cost + dinnerSummary.cost) * people, 1),
      kcal: round((breakfastSummary.kcal + lunchSummary.kcal + dinnerSummary.kcal) * people, 0),
      protein: round((breakfastSummary.protein + lunchSummary.protein + dinnerSummary.protein) * people, 1),
      fiber: round((breakfastSummary.fiber + lunchSummary.fiber + dinnerSummary.fiber) * people, 1),
      bmiValue: round((breakfastSummary.bmiValue + lunchSummary.bmiValue + dinnerSummary.bmiValue) / 3, 0)
    };
    dailyPlans.push({ day, breakfast, lunch, dinner, breakfastSummary, lunchSummary, dinnerSummary, total });
  }

  return {
    budget,
    people,
    days,
    region,
    profile,
    bmiCategory: getBMICategory(profile.bmi),
    dailyPlans,
    shoppingList: buildShoppingList(dailyPlans, people, region),
    grandTotal: round(dailyPlans.reduce((sum, plan) => sum + plan.total.cost, 0), 1)
  };
}

export function buildShoppingList(dailyPlans, people = 1, region = 'national') {
  const ingredientMap = new Map();
  for (const plan of dailyPlans) {
    for (const dish of [...plan.breakfast, ...plan.lunch, ...plan.dinner]) {
      for (const ingredient of dish.ingredients) {
        const price = getIngredientMarketPrice(ingredient, region);
        const kg = estimateIngredientQuantityKg(ingredient, dish) * people;
        const item = ingredientMap.get(ingredient) || { name: ingredient, count: 0, kg: 0, estimatedCost: 0, unitPrice: price.activePrice, category: price.category, dishes: new Set() };
        item.count += people;
        item.kg += kg;
        item.estimatedCost += price.activePrice * kg;
        item.unitPrice = price.activePrice;
        item.category = price.category;
        item.dishes.add(dish.name);
        ingredientMap.set(ingredient, item);
      }
    }
  }
  return [...ingredientMap.values()]
    .map((item) => ({
      name: item.name,
      portions: item.count,
      estimatedKg: round(item.kg, 3),
      unitPrice: round(item.unitPrice, 1),
      estimatedCost: round(item.estimatedCost, 1),
      category: item.category,
      dishes: [...item.dishes].slice(0, 4)
    }))
    .sort((a, b) => b.portions - a.portions || a.name.localeCompare(b.name, 'zh-Hans-CN'));
}

export function assertNoDailyDuplicates(plan) {
  return plan.dailyPlans.every((day) => {
    const seen = new Set();
    for (const dish of [...day.breakfast, ...day.lunch, ...day.dinner]) {
      if (seen.has(dish.id)) return false;
      seen.add(dish.id);
    }
    return true;
  });
}

