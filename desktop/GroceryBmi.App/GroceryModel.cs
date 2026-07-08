using System.Text.Json;

namespace GroceryBmi.App;

public sealed class Dish
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Meals { get; set; } = [];
    public List<string> Regions { get; set; } = [];
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbs { get; set; }
    public double Fiber { get; set; }
    public double Cost { get; set; }
    public List<string> Ingredients { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string Color { get; set; } = "#16a34a";
    public string Icon { get; set; } = "🥗";
}

public sealed class BudgetTier { public double Value { get; set; } public string Label { get; set; } = string.Empty; public string Hint { get; set; } = string.Empty; }
public sealed class SourceNote { public string Name { get; set; } = string.Empty; public string Url { get; set; } = string.Empty; }
public sealed class MarketPrice
{
    public string Item { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public double NationalAvg { get; set; }
    public Dictionary<string, double> Regions { get; set; } = [];
    public string Source { get; set; } = string.Empty;
}
public sealed class RegionOption { public string Id { get; set; } = "national"; public string Name { get; set; } = "全国通用"; public string Hint { get; set; } = string.Empty; }

public sealed class AppData
{
    public List<BudgetTier> BudgetTiers { get; set; } = [];
    public List<RegionOption> Regions { get; set; } = [];
    public List<Dish> Dishes { get; set; } = [];
    public List<MarketPrice> MarketPrices { get; set; } = [];
    public List<SourceNote> SourceNotes { get; set; } = [];
}

public sealed record Profile(double HeightCm, double WeightKg) { public double Bmi => GroceryModel.CalculateBmi(HeightCm, WeightKg); }
public sealed record BmiCategory(string Label, string Tone, string Advice);
public sealed record MealSummary(double Cost, double Kcal, double Protein, double Fat, double Carbs, double Fiber, double BmiValue);

public sealed class DayPlan
{
    public int Day { get; init; }
    public List<Dish> Breakfast { get; init; } = [];
    public List<Dish> Lunch { get; init; } = [];
    public List<Dish> Dinner { get; init; } = [];
    public MealSummary BreakfastSummary { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public MealSummary LunchSummary { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public MealSummary DinnerSummary { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public MealSummary Total { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
}

public sealed class ShoppingItem
{
    public string Name { get; init; } = string.Empty;
    public int Portions { get; init; }
    public double EstimatedKg { get; init; }
    public double UnitPrice { get; init; }
    public double EstimatedCost { get; init; }
    public string Category { get; init; } = string.Empty;
    public List<string> Dishes { get; init; } = [];
}

public sealed class BudgetPlan
{
    public double Budget { get; init; }
    public int People { get; init; }
    public int Days { get; init; }
    public string Region { get; init; } = "national";
    public Profile Profile { get; init; } = new(170, 65);
    public BmiCategory BmiCategory { get; init; } = GroceryModel.GetBmiCategory(22);
    public List<DayPlan> DailyPlans { get; init; } = [];
    public List<ShoppingItem> ShoppingList { get; init; } = [];
    public double GrandTotal { get; init; }
}

public sealed class GroceryModel
{
    private readonly List<Dish> _dishes;
    private readonly List<MarketPrice> _marketPrices;
    public GroceryModel(IEnumerable<Dish> dishes, IEnumerable<MarketPrice>? marketPrices = null)
    {
        _dishes = dishes.ToList();
        _marketPrices = (marketPrices ?? []).ToList();
    }

    public static AppData LoadData()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Data", "dishes.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "desktop", "GroceryBmi.App", "Data", "dishes.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "dishes.json")
        };
        var file = candidates.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("找不到菜品数据 Data/dishes.json。", candidates[0]);
        var data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppData();
        if (data.Regions.Count == 0) data.Regions.Add(new RegionOption());
        foreach (var dish in data.Dishes.Where(d => d.Regions.Count == 0)) dish.Regions.Add("national");
        return data;
    }

    public static double Round(double value, int digits = 1) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private MarketPrice GetIngredientMarketPrice(string ingredient)
    {
        return _marketPrices.FirstOrDefault(p => p.Item == ingredient)
            ?? _marketPrices.FirstOrDefault(p => ingredient.Contains(p.Item, StringComparison.OrdinalIgnoreCase) || p.Item.Contains(ingredient, StringComparison.OrdinalIgnoreCase))
            ?? new MarketPrice { Item = ingredient, Category = "未分类", NationalAvg = 10, Regions = new Dictionary<string, double> { ["national"] = 10 }, Source = "项目默认估算" };
    }

    private double GetIngredientUnitPrice(string ingredient, string region)
    {
        var price = GetIngredientMarketPrice(ingredient);
        if (price.Regions.TryGetValue(region, out var regional)) return regional;
        if (price.Regions.TryGetValue("national", out var national)) return national;
        return price.NationalAvg;
    }

    private static double EstimateIngredientQuantityKg(string ingredient, Dish dish)
    {
        if (ingredient.Contains('酱') || ingredient.Contains('醋') || ingredient.Contains('椒') || ingredient.Contains('葱') || ingredient.Contains('姜') || ingredient.Contains('蒜') || ingredient.Contains('糖') || ingredient.Contains('油') || ingredient.Contains('料') || ingredient.Contains('茶') || ingredient.Contains('淀') || ingredient.Contains('可')) return 0.012;
        if (ingredient.Contains('米') || ingredient.Contains('面') || ingredient.Contains('粉') || ingredient.Contains('饭') || ingredient.Contains('馍') || ingredient.Contains('麦') || ingredient.Contains('粥') || ingredient.Contains('包') || ingredient.Contains("油条")) return dish.Type == "breakfast" ? 0.11 : 0.09;
        if (ingredient.Contains('牛') || ingredient.Contains('羊') || ingredient.Contains('猪') || ingredient.Contains('肉') || ingredient.Contains("排骨") || ingredient.Contains('鸡') || ingredient.Contains('鱼') || ingredient.Contains('虾') || ingredient.Contains('蛤') || ingredient.Contains("海蛎")) return dish.Type == "soup" ? 0.12 : 0.14;
        if (ingredient.Contains("豆腐") || ingredient.Contains("腐竹") || ingredient.Contains("豆花") || ingredient.Contains("黄豆") || ingredient.Contains("牛奶") || ingredient.Contains("鸡蛋") || ingredient.Contains("皮蛋")) return 0.12;
        if (ingredient.Contains('蘑') || ingredient.Contains('菇') || ingredient.Contains("木耳") || ingredient.Contains('笋') || ingredient.Contains("海带")) return 0.045;
        if (ingredient.Contains("蓝莓") || ingredient.Contains("花生") || ingredient.Contains("咸菜") || ingredient.Contains("榨菜") || ingredient.Contains("肉松")) return 0.04;
        return dish.Type == "vegetable" ? 0.18 : 0.10;
    }

    private double EstimateDishCostFromMarket(Dish dish, string region)
    {
        var total = dish.Ingredients.Sum(ingredient => GetIngredientUnitPrice(ingredient, region) * EstimateIngredientQuantityKg(ingredient, dish));
        return Round(Math.Max(total, 1.5), 1);
    }

    private void ApplyMarketCosts(string region)
    {
        foreach (var dish in _dishes) dish.Cost = EstimateDishCostFromMarket(dish, region);
    }

    public static double CalculateBmi(double heightCm, double weightKg) { var heightM = heightCm / 100.0; return heightM <= 0 || weightKg <= 0 ? 0 : Round(weightKg / (heightM * heightM), 1); }

    public static BmiCategory GetBmiCategory(double bmi)
    {
        if (bmi <= 0) return new("待输入", "neutral", "请输入身高体重以生成个性化菜品适配值。");
        if (bmi < 18.5) return new("偏瘦", "gain", "建议提高优质蛋白与主食比例，避免长期热量不足。");
        if (bmi < 24) return new("正常", "fit", "建议维持蛋白、蔬菜、主食的均衡搭配。");
        if (bmi < 28) return new("超重", "control", "建议优先选择低脂高纤菜品，控制油脂与精制主食。");
        return new("肥胖", "reduce", "建议选择低能量密度、高蛋白、高纤维搭配，并咨询专业人士。");
    }

    public static double GetMealEnergyTarget(double bmi, string meal)
    {
        var baseKcal = meal == "breakfast" ? 380 : meal == "lunch" ? 560 : 520;
        if (bmi <= 0) return baseKcal;
        if (bmi < 18.5) return baseKcal + (meal == "breakfast" ? 90 : 130);
        if (bmi < 24) return baseKcal;
        if (bmi < 28) return baseKcal - (meal == "breakfast" ? 55 : 90);
        return baseKcal - (meal == "breakfast" ? 80 : 140);
    }

    public static double CalculateDishBmiValue(Dish dish, Profile profile, string meal)
    {
        var bmi = profile.Bmi == 0 ? 22 : profile.Bmi;
        var targetKcal = GetMealEnergyTarget(bmi, meal);
        var energyFit = Clamp(34 - Math.Abs(dish.Kcal - targetKcal) / targetKcal * 34, 0, 34);
        var proteinTarget = meal == "breakfast" ? 24 : 32;
        var fiberTarget = meal == "breakfast" ? 6 : 8;
        var fatLimit = meal == "breakfast" ? 14 : 18;
        var energyCost = meal == "breakfast" ? 9 : 14;
        var proteinFit = Clamp(dish.Protein / proteinTarget * 24, 0, 24);
        var fiberFit = Clamp(dish.Fiber / fiberTarget * 18, 0, 18);
        var fatPenalty = Clamp((dish.Fat - fatLimit) * 0.7, 0, 12);
        var vegetableBonus = dish.Type is "vegetable" or "soup" or "lean-protein" or "breakfast" ? 8 : 3;
        var budgetFit = Clamp(16 - Math.Abs(dish.Cost - energyCost) * 0.45, 4, 16);
        var bmiAdjustment = bmi >= 24 && dish.Kcal < targetKcal ? 5 : bmi < 18.5 && dish.Protein >= 18 ? 5 : 0;
        return Round(Clamp(energyFit + proteinFit + fiberFit + vegetableBonus + budgetFit + bmiAdjustment - fatPenalty, 0, 100), 0);
    }

    public static MealSummary SummarizeCombo(IEnumerable<Dish> combo, Profile profile, string meal)
    {
        var list = combo.ToList(); var count = Math.Max(1, list.Count);
        return new MealSummary(Round(list.Sum(d => d.Cost), 1), Round(list.Sum(d => d.Kcal), 0), Round(list.Sum(d => d.Protein), 1), Round(list.Sum(d => d.Fat), 1), Round(list.Sum(d => d.Carbs), 1), Round(list.Sum(d => d.Fiber), 1), Round(list.Sum(d => CalculateDishBmiValue(d, profile, meal)) / count, 0));
    }

    public BudgetPlan GenerateBudgetPlan(double budget, int people, int days, double heightCm, double weightKg, string region = "national")
    {
        people = (int)Clamp(people, 1, 6); days = (int)Clamp(days, 1, 7);
        var profile = new Profile(heightCm, weightKg);
        ApplyMarketCosts(region);
        var dailyPlans = new List<DayPlan>(); var weeklyUsed = new Dictionary<string, int>();
        var breakfastRatio = budget <= 30 ? 0.25 : 0.22; var lunchRatio = budget <= 30 ? 0.38 : 0.40;
        for (var day = 1; day <= days; day++)
        {
            var breakfast = ChooseMealCombo("breakfast", budget * breakfastRatio / people, profile, [], day * 11, weeklyUsed, region);
            var lunch = ChooseMealCombo("lunch", budget * lunchRatio / people, profile, breakfast.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase), day * 17, weeklyUsed, region);
            var forbidden = breakfast.Concat(lunch).Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var dinner = ChooseMealCombo("dinner", budget * (1 - breakfastRatio - lunchRatio) / people, profile, forbidden, day * 31, weeklyUsed, region);
            foreach (var dish in breakfast.Concat(lunch).Concat(dinner)) weeklyUsed[dish.Id] = weeklyUsed.TryGetValue(dish.Id, out var old) ? old + 1 : 1;
            var bs = SummarizeCombo(breakfast, profile, "breakfast"); var ls = SummarizeCombo(lunch, profile, "lunch"); var ds = SummarizeCombo(dinner, profile, "dinner");
            var total = new MealSummary(Round((bs.Cost + ls.Cost + ds.Cost) * people, 1), Round((bs.Kcal + ls.Kcal + ds.Kcal) * people, 0), Round((bs.Protein + ls.Protein + ds.Protein) * people, 1), Round((bs.Fat + ls.Fat + ds.Fat) * people, 1), Round((bs.Carbs + ls.Carbs + ds.Carbs) * people, 1), Round((bs.Fiber + ls.Fiber + ds.Fiber) * people, 1), Round((bs.BmiValue + ls.BmiValue + ds.BmiValue) / 3, 0));
            dailyPlans.Add(new DayPlan { Day = day, Breakfast = breakfast, Lunch = lunch, Dinner = dinner, BreakfastSummary = bs, LunchSummary = ls, DinnerSummary = ds, Total = total });
        }
        return new BudgetPlan { Budget = budget, People = people, Days = days, Region = region, Profile = profile, BmiCategory = GetBmiCategory(profile.Bmi), DailyPlans = dailyPlans, ShoppingList = BuildShoppingList(dailyPlans, people, region), GrandTotal = Round(dailyPlans.Sum(p => p.Total.Cost), 1) };
    }

    private List<Dish> ChooseMealCombo(string meal, double targetCost, Profile profile, HashSet<string> forbiddenIds, int daySeed, Dictionary<string, int> weeklyUsed, string region)
    {
        var maxDishes = meal == "breakfast" ? 1 : targetCost >= 45 ? 4 : targetCost >= 22 ? 3 : 2;
        var minDishes = meal == "breakfast" ? 1 : targetCost >= 20 ? 2 : 1;
        var candidates = _dishes.Where(d => d.Meals.Contains(meal) && !forbiddenIds.Contains(d.Id)).OrderByDescending(d => CalculateDishBmiValue(d, profile, meal) + RegionScore(d, region)).ThenBy(d => d.Cost).Take(meal == "breakfast" ? 24 : 34).ToList();
        var best = new List<Dish>(); var bestScore = double.NegativeInfinity;
        foreach (var combo in Combinations(candidates, minDishes, maxDishes))
        {
            var summary = SummarizeCombo(combo, profile, meal);
            var costDistance = Math.Abs(summary.Cost - targetCost);
            var overBudgetPenalty = summary.Cost > targetCost * 1.12 ? (summary.Cost - targetCost) * 8 : 0;
            var nutritionScore = combo.Average(d => CalculateDishBmiValue(d, profile, meal));
            var diversity = ComboDiversityScore(combo, region);
            var repeatPenalty = combo.Sum(d => weeklyUsed.TryGetValue(d.Id, out var used) ? used * 6 : 0);
            var regionalBonus = combo.Sum(d => RegionScore(d, region));
            var seedNudge = (combo.Sum(d => d.Id.Length > 0 ? d.Id[0] : 0) + daySeed) % 9 / 10.0;
            var score = nutritionScore * 1.2 + diversity + regionalBonus - costDistance * 3.2 - overBudgetPenalty - repeatPenalty + seedNudge;
            if (score > bestScore) { bestScore = score; best = combo; }
        }
        return best;
    }

    private static double RegionScore(Dish dish, string region)
    {
        var regions = dish.Regions.Count == 0 ? new List<string> { "national" } : dish.Regions;
        if (region == "national") return regions.Contains("national") ? 6 : 2;
        if (regions.Contains(region)) return 18;
        return regions.Contains("national") ? 8 : 0;
    }

    private static double ComboDiversityScore(IReadOnlyCollection<Dish> combo, string region)
    {
        var types = combo.Select(d => d.Type).Distinct().Count(); var cuisines = combo.Select(d => d.Cuisine).Distinct().Count();
        var hasVegetable = combo.Any(d => d.Type == "vegetable" || d.Tags.Contains("绿色蔬菜") || d.Tags.Contains("高纤维"));
        var hasProtein = combo.Any(d => d.Type.Contains("protein", StringComparison.OrdinalIgnoreCase) || d.Protein >= 18);
        var hasSoup = combo.Any(d => d.Type == "soup"); var hasStaple = combo.Any(d => d.Type is "staple" or "breakfast");
        var regional = combo.Sum(d => RegionScore(d, region) / 4);
        return types * 5 + cuisines * 2 + (hasVegetable ? 12 : 0) + (hasProtein ? 12 : 0) + (hasSoup ? 3 : 0) + (hasStaple ? 3 : 0) + regional;
    }

    private static IEnumerable<List<Dish>> Combinations(IReadOnlyList<Dish> items, int minSize, int maxSize)
    {
        var stack = new List<Dish>(); foreach (var combo in Walk(0)) yield return combo;
        IEnumerable<List<Dish>> Walk(int start) { if (stack.Count >= minSize) yield return stack.ToList(); if (stack.Count == maxSize) yield break; for (var i = start; i < items.Count; i++) { stack.Add(items[i]); foreach (var combo in Walk(i + 1)) yield return combo; stack.RemoveAt(stack.Count - 1); } }
    }

    public List<ShoppingItem> BuildShoppingList(IEnumerable<DayPlan> dailyPlans, int people, string region)
    {
        var map = new Dictionary<string, (int Count, double Kg, double EstimatedCost, double UnitPrice, string Category, HashSet<string> Dishes)>();
        foreach (var plan in dailyPlans)
        foreach (var dish in plan.Breakfast.Concat(plan.Lunch).Concat(plan.Dinner))
        foreach (var ingredient in dish.Ingredients)
        {
            var priceInfo = GetIngredientMarketPrice(ingredient);
            var unitPrice = GetIngredientUnitPrice(ingredient, region);
            var kg = EstimateIngredientQuantityKg(ingredient, dish) * people;
            if (!map.TryGetValue(ingredient, out var item)) item = (0, 0, 0, unitPrice, priceInfo.Category, []);
            item.Count += people;
            item.Kg += kg;
            item.EstimatedCost += unitPrice * kg;
            item.UnitPrice = unitPrice;
            item.Category = priceInfo.Category;
            item.Dishes.Add(dish.Name);
            map[ingredient] = item;
        }
        return map.Select(kvp => new ShoppingItem { Name = kvp.Key, Portions = kvp.Value.Count, EstimatedKg = Round(kvp.Value.Kg, 3), UnitPrice = Round(kvp.Value.UnitPrice, 1), EstimatedCost = Round(kvp.Value.EstimatedCost, 1), Category = kvp.Value.Category, Dishes = kvp.Value.Dishes.Take(4).ToList() }).OrderByDescending(i => i.Portions).ThenBy(i => i.Name, StringComparer.Create(new System.Globalization.CultureInfo("zh-CN"), false)).ToList();
    }

    public static bool AssertNoDailyDuplicates(BudgetPlan plan)
    {
        return plan.DailyPlans.All(day => { var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); return day.Breakfast.Concat(day.Lunch).Concat(day.Dinner).All(d => seen.Add(d.Id)); });
    }
}

