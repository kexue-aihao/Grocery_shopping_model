using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace GroceryBmi.App;

public sealed class MainForm : Form
{
    private readonly AppData _data;
    private readonly GroceryModel _model;
    private BudgetPlan _plan;

    private readonly NumericUpDown _height = new();
    private readonly NumericUpDown _weight = new();
    private readonly NumericUpDown _people = new();
    private readonly NumericUpDown _days = new();
    private readonly NumericUpDown _budget = new();
    private readonly Button _regionButton = new();
    private readonly Label _regionLabel = new();
    private string _selectedRegionId = "national";
    private readonly Label _bmiValue = new();
    private readonly Label _bmiStatus = new();
    private readonly Label _advice = new();
    private readonly Label _total = new();
    private readonly FlowLayoutPanel _summaryFlow = new();
    private readonly FlowLayoutPanel _planFlow = new();
    private readonly FlowLayoutPanel _shoppingFlow = new();
    private readonly FlowLayoutPanel _dishFlow = new();

    private static readonly Color Bg = Color.FromArgb(244, 248, 242);
    private static readonly Color Primary = Color.FromArgb(22, 163, 74);
    private static readonly Color Ink = Color.FromArgb(16, 32, 22);
    private static readonly Color Muted = Color.FromArgb(100, 112, 103);
    private static readonly Color Line = Color.FromArgb(225, 232, 224);

    public MainForm()
    {
        _data = GroceryModel.LoadData();
        _model = new GroceryModel(_data.Dishes, _data.MarketPrices);
        _plan = _model.GenerateBudgetPlan(50, 1, 3, 170, 65, _selectedRegionId);

        Text = "买菜 BMI 选购模型 - 软件版";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
        if (!File.Exists(iconPath)) iconPath = Path.Combine(Directory.GetCurrentDirectory(), "desktop", "GroceryBmi.App", "Assets", "logo.ico");
        if (File.Exists(iconPath)) Icon = new Icon(iconPath);
        MinimumSize = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
        Regenerate();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Bg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildControlPanel(), 0, 0);
        root.Controls.Add(BuildMainTabs(), 1, 0);
    }

    private Control BuildControlPanel()
    {
        var panel = new RoundPanel { Dock = DockStyle.Fill, BackColor = Color.White, Radius = 28, Padding = new Padding(22) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        panel.Controls.Add(flow);

        flow.Controls.Add(new Label
        {
            Text = "🥗 Grocery BMI",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold),
            ForeColor = Ink,
            AutoSize = false,
            Width = 292,
            Height = 42
        });
        flow.Controls.Add(new Label
        {
            Text = "这是本地 Windows 桌面软件，不是 file:// 网页。平台范围已缩减为 Windows 与 Android；Android 使用 Flutter 工程。",
            ForeColor = Muted,
            AutoSize = false,
            Width = 292,
            Height = 74
        });

        ConfigureNumber(_height, 120, 220, 170, "cm");
        ConfigureNumber(_weight, 30, 180, 65, "kg");
        ConfigureNumber(_people, 1, 6, 1, "人");
        ConfigureNumber(_days, 1, 7, 3, "天");
        ConfigureNumber(_budget, 10, 500, 50, "元");
        _budget.Increment = 5;
        _budget.DecimalPlaces = 0;

        flow.Controls.Add(Field("身高", _height, "cm"));
        flow.Controls.Add(Field("体重", _weight, "kg"));
        flow.Controls.Add(Field("用餐人数", _people, "人"));
        flow.Controls.Add(Field("计划天数", _days, "天"));
        flow.Controls.Add(Field("手动预算", _budget, "元/天"));
        ConfigureRegionControls();
        flow.Controls.Add(RegionSelectorPanel());

        var bmiCard = new RoundPanel { Width = 292, Height = 142, Radius = 22, BackColor = Color.FromArgb(240, 253, 244), Margin = new Padding(0, 12, 0, 8) };
        bmiCard.Controls.Add(new Label { Text = "当前 BMI", Location = new Point(18, 16), AutoSize = true, ForeColor = Muted, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) });
        _bmiValue.Location = new Point(16, 42);
        _bmiValue.Size = new Size(130, 64);
        _bmiValue.Font = new Font(Font.FontFamily, 34, FontStyle.Bold);
        _bmiValue.ForeColor = Ink;
        bmiCard.Controls.Add(_bmiValue);
        _bmiStatus.Location = new Point(174, 58);
        _bmiStatus.Size = new Size(92, 36);
        _bmiStatus.TextAlign = ContentAlignment.MiddleCenter;
        _bmiStatus.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
        bmiCard.Controls.Add(_bmiStatus);
        flow.Controls.Add(bmiCard);

        _advice.Width = 292;
        _advice.Height = 72;
        _advice.ForeColor = Muted;
        flow.Controls.Add(_advice);

        var regenerate = new Button
        {
            Text = "按手动预算生成方案",
            Width = 292,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            BackColor = Primary,
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 8)
        };
        regenerate.FlatAppearance.BorderSize = 0;
        regenerate.Click += (_, _) => Regenerate();
        flow.Controls.Add(regenerate);

        var publishInfo = new Button
        {
            Text = "打开软件编译目录",
            Width = 292,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Primary,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
        };
        publishInfo.FlatAppearance.BorderColor = Line;
        publishInfo.Click += (_, _) => OpenPublishFolder();
        flow.Controls.Add(publishInfo);

        return panel;
    }

    private Control BuildMainTabs()
    {
        var container = new RoundPanel { Dock = DockStyle.Fill, BackColor = Color.White, Radius = 28, Padding = new Padding(18) };
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) };
        container.Controls.Add(tabs);

        var planPage = new TabPage("每日方案");
        var shoppingPage = new TabPage("购物清单");
        var dishPage = new TabPage("菜品样式池");
        var compatPage = new TabPage("软件兼容");
        tabs.TabPages.AddRange([planPage, shoppingPage, dishPage, compatPage]);

        _summaryFlow.Dock = DockStyle.Top;
        _summaryFlow.Height = 118;
        _summaryFlow.FlowDirection = FlowDirection.LeftToRight;
        _summaryFlow.WrapContents = true;
        _summaryFlow.BackColor = Color.White;
        _planFlow.Dock = DockStyle.Fill;
        _planFlow.FlowDirection = FlowDirection.TopDown;
        _planFlow.WrapContents = false;
        _planFlow.AutoScroll = true;
        _planFlow.BackColor = Color.White;
        planPage.Controls.Add(_planFlow);
        planPage.Controls.Add(_summaryFlow);

        _shoppingFlow.Dock = DockStyle.Fill;
        _shoppingFlow.FlowDirection = FlowDirection.TopDown;
        _shoppingFlow.WrapContents = false;
        _shoppingFlow.AutoScroll = true;
        _shoppingFlow.BackColor = Color.White;
        shoppingPage.Controls.Add(_shoppingFlow);

        _dishFlow.Dock = DockStyle.Fill;
        _dishFlow.FlowDirection = FlowDirection.LeftToRight;
        _dishFlow.WrapContents = true;
        _dishFlow.AutoScroll = true;
        _dishFlow.BackColor = Color.White;
        dishPage.Controls.Add(_dishFlow);

        compatPage.Controls.Add(BuildCompatibilityPanel());
        return container;
    }

    private Control BuildCompatibilityPanel()
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(16), BackColor = Color.White };
        flow.Controls.Add(Title("软件形态说明", 22));
        flow.Controls.Add(Paragraph("当前已提供 Windows 桌面软件工程：desktop/GroceryBmi.App/GroceryBmi.App.csproj。它使用 .NET Windows Forms 原生窗口运行，启动后不是浏览器、不是 file:// 页面。预算采用手动输入。"));
        flow.Controls.Add(CompatibilityCard("Windows", "已本地编译", "dotnet publish 输出 GroceryBmiShopping.exe，可直接双击运行。"));
        flow.Controls.Add(CompatibilityCard("Android", "Flutter 工程", "Android 软件源码位于 android_flutter/grocery_bmi_app；GitHub Actions 会安装 Flutter 并构建 APK。"));
        flow.Controls.Add(Paragraph("说明：本机当前未检测到 Flutter/Android SDK，因此 Android 暂不在本地编译；已创建 GitHub 自构建工作流负责云端构建 APK 并发布 Release。"));
        return flow;
    }

    private void Regenerate()
    {
        var manualBudget = (double)_budget.Value;
        _plan = _model.GenerateBudgetPlan(manualBudget, (int)_people.Value, (int)_days.Value, (double)_height.Value, (double)_weight.Value, _selectedRegionId);
        _regionLabel.Text = $"当前地区：{RegionName(_selectedRegionId)}";
        RenderBmi();
        RenderSummary();
        RenderPlan();
        RenderShoppingList();
        RenderDishPool();
    }

    private void RenderBmi()
    {
        _bmiValue.Text = _plan.Profile.Bmi.ToString("0.0");
        _bmiStatus.Text = _plan.BmiCategory.Label;
        _bmiStatus.BackColor = _plan.BmiCategory.Tone switch
        {
            "fit" => Color.FromArgb(220, 252, 231),
            "control" => Color.FromArgb(255, 237, 213),
            "reduce" => Color.FromArgb(254, 226, 226),
            "gain" => Color.FromArgb(219, 234, 254),
            _ => Color.White
        };
        _bmiStatus.ForeColor = _plan.BmiCategory.Tone switch
        {
            "fit" => Color.FromArgb(21, 128, 61),
            "control" => Color.FromArgb(194, 65, 12),
            "reduce" => Color.FromArgb(185, 28, 28),
            "gain" => Color.FromArgb(37, 99, 235),
            _ => Ink
        };
        _advice.Text = _plan.BmiCategory.Advice;
    }

    private void RenderSummary()
    {
        _summaryFlow.SuspendLayout();
        _summaryFlow.Controls.Clear();
        _summaryFlow.Controls.Add(MetricCard("用户 BMI", _plan.Profile.Bmi.ToString("0.0"), _plan.BmiCategory.Label));
        _summaryFlow.Controls.Add(MetricCard("预算", $"¥{_plan.Budget:0}", "含早餐/中餐/晚餐"));
        _summaryFlow.Controls.Add(MetricCard("地区", RegionName(_plan.Region), "按地区人群偏好"));
        _summaryFlow.Controls.Add(MetricCard("菜品池", _data.Dishes.Count.ToString(), "全国早中晚餐"));
        _summaryFlow.Controls.Add(MetricCard("预计总价", $"¥{_plan.GrandTotal:0.0}", $"{_plan.Days} 天累计"));
        _summaryFlow.ResumeLayout();
        _total.Text = $"¥{_plan.GrandTotal:0.0}";
    }

    private void RenderPlan()
    {
        _planFlow.SuspendLayout();
        _planFlow.Controls.Clear();
        foreach (var day in _plan.DailyPlans)
        {
            var card = new RoundPanel { Width = 780, Height = 385, Radius = 24, BackColor = Color.FromArgb(249, 252, 249), Padding = new Padding(16), Margin = new Padding(0, 0, 0, 14) };
            card.Controls.Add(new Label
            {
                Text = $"DAY {day.Day}  三餐预算 ¥{day.Total.Cost:0.0} · 三餐BMI适配 {day.Total.BmiValue:0} · {day.Total.Kcal:0} kcal · 蛋白 {day.Total.Protein:0.0}g",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                ForeColor = Ink
            });
            var meals = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 44, 0, 0) };
            meals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            meals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            meals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            meals.Controls.Add(MealPanel("🌅 早餐", day.Breakfast, day.BreakfastSummary, "breakfast"), 0, 0);
            meals.Controls.Add(MealPanel("☀️ 中餐", day.Lunch, day.LunchSummary, "lunch"), 1, 0);
            meals.Controls.Add(MealPanel("🌙 晚餐", day.Dinner, day.DinnerSummary, "dinner"), 2, 0);
            card.Controls.Add(meals);
            _planFlow.Controls.Add(card);
        }
        _planFlow.ResumeLayout();
    }

    private Control MealPanel(string title, IEnumerable<Dish> dishes, MealSummary summary, string meal)
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8), BackColor = Color.White };
        flow.Controls.Add(new Label { Text = $"{title}  ¥{summary.Cost * _plan.People:0.0}", Width = 220, Height = 28, Font = new Font(Font.FontFamily, 11, FontStyle.Bold), ForeColor = Primary });
        foreach (var dish in dishes) flow.Controls.Add(DishRow(dish, meal, 220));
        return flow;
    }

    private Control DishRow(Dish dish, string meal, int width)
    {
        var score = GroceryModel.CalculateDishBmiValue(dish, _plan.Profile, meal);
        var panel = new RoundPanel { Width = width, Height = 70, Radius = 16, BackColor = Color.FromArgb(252, 252, 252), Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = dish.Icon, Location = new Point(10, 14), Size = new Size(42, 42), Font = new Font(Font.FontFamily, 21, FontStyle.Regular), TextAlign = ContentAlignment.MiddleCenter });
        panel.Controls.Add(new Label { Text = dish.Name, Location = new Point(58, 9), Size = new Size(110, 22), Font = new Font(Font.FontFamily, 10, FontStyle.Bold), ForeColor = Ink });
        panel.Controls.Add(new Label { Text = $"{dish.Style} · ¥{dish.Cost:0.0} · {dish.Kcal:0}kcal", Location = new Point(58, 34), Size = new Size(130, 22), ForeColor = Muted });
        panel.Controls.Add(new Label { Text = score.ToString("0"), Location = new Point(width - 58, 18), Size = new Size(40, 30), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(220, 252, 231), ForeColor = Primary, Font = new Font(Font.FontFamily, 11, FontStyle.Bold) });
        return panel;
    }

    private void RenderShoppingList()
    {
        _shoppingFlow.SuspendLayout();
        _shoppingFlow.Controls.Clear();
        var header = new RoundPanel { Width = 760, Height = 90, Radius = 22, BackColor = Primary, Margin = new Padding(0, 0, 0, 14) };
        header.Controls.Add(new Label { Text = "预计采购总价", Location = new Point(20, 16), Size = new Size(180, 22), ForeColor = Color.White, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) });
        header.Controls.Add(new Label { Text = $"¥{_plan.GrandTotal:0.0}", Location = new Point(20, 36), Size = new Size(220, 42), ForeColor = Color.White, Font = new Font(Font.FontFamily, 25, FontStyle.Bold) });
        header.Controls.Add(new Label { Text = "已按早餐/中餐/晚餐、人数和天数合并食材，方便一次性采购。", Location = new Point(280, 32), Size = new Size(420, 30), ForeColor = Color.White });
        _shoppingFlow.Controls.Add(header);

        foreach (var item in _plan.ShoppingList)
        {
            var row = new RoundPanel { Width = 760, Height = 64, Radius = 16, BackColor = Color.FromArgb(249, 252, 249), Margin = new Padding(0, 0, 0, 8) };
            row.Controls.Add(new Label { Text = item.Name, Location = new Point(16, 10), Size = new Size(150, 22), Font = new Font(Font.FontFamily, 11, FontStyle.Bold), ForeColor = Ink });
            row.Controls.Add(new Label { Text = $"用于：{string.Join("、", item.Dishes)} · {item.Category} · {item.EstimatedKg:0.###}kg × ¥{item.UnitPrice:0.0}/kg", Location = new Point(16, 34), Size = new Size(560, 20), ForeColor = Muted });
            row.Controls.Add(new Label { Text = $"¥{item.EstimatedCost:0.0}", Location = new Point(650, 18), Size = new Size(88, 28), TextAlign = ContentAlignment.MiddleRight, ForeColor = Primary, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) });
            _shoppingFlow.Controls.Add(row);
        }
        _shoppingFlow.ResumeLayout();
    }

    private void RenderDishPool()
    {
        _dishFlow.SuspendLayout();
        _dishFlow.Controls.Clear();
        foreach (var dish in _data.Dishes.OrderByDescending(d => GroceryModel.CalculateDishBmiValue(d, _plan.Profile, "lunch")))
        {
            var color = TryColor(dish.Color, Color.FromArgb(232, 248, 237));
            var card = new RoundPanel { Width = 178, Height = 150, Radius = 22, BackColor = Blend(color, Color.White, 0.78), Margin = new Padding(0, 0, 12, 12) };
            card.Controls.Add(new Label { Text = dish.Icon, Location = new Point(0, 14), Size = new Size(178, 42), Font = new Font(Font.FontFamily, 24), TextAlign = ContentAlignment.MiddleCenter });
            card.Controls.Add(new Label { Text = dish.Name, Location = new Point(10, 60), Size = new Size(158, 24), Font = new Font(Font.FontFamily, 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Ink });
            card.Controls.Add(new Label { Text = dish.Style, Location = new Point(10, 84), Size = new Size(158, 20), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Muted });
            card.Controls.Add(new Label { Text = $"BMI适配 {GroceryModel.CalculateDishBmiValue(dish, _plan.Profile, "lunch"):0} · ¥{dish.Cost:0.0}", Location = new Point(10, 112), Size = new Size(158, 24), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Primary, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) });
            _dishFlow.Controls.Add(card);
        }
        _dishFlow.ResumeLayout();
    }


    private RegionOption CurrentRegion() => _data.Regions.FirstOrDefault(r => r.Id == _selectedRegionId) ?? _data.Regions.FirstOrDefault() ?? new RegionOption();

    private string RegionName(string id) => (_data.Regions.FirstOrDefault(r => r.Id == id) ?? CurrentRegion()).Name;

    private void ConfigureRegionControls()
    {
        _regionButton.Text = "选择地区/人群";
        _regionButton.Width = 292;
        _regionButton.Height = 42;
        _regionButton.FlatStyle = FlatStyle.Flat;
        _regionButton.BackColor = Color.White;
        _regionButton.ForeColor = Primary;
        _regionButton.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
        _regionButton.FlatAppearance.BorderColor = Line;
        _regionButton.Click += (_, _) => OpenRegionPicker();
        _regionLabel.Text = $"当前地区：{RegionName(_selectedRegionId)}";
        _regionLabel.Width = 292;
        _regionLabel.Height = 34;
        _regionLabel.ForeColor = Muted;
    }

    private Control RegionSelectorPanel()
    {
        var panel = new Panel { Width = 292, Height = 82, Margin = new Padding(0, 0, 0, 8) };
        _regionButton.Location = new Point(0, 0);
        _regionLabel.Location = new Point(0, 48);
        panel.Controls.Add(_regionButton);
        panel.Controls.Add(_regionLabel);
        return panel;
    }

    private void OpenRegionPicker()
    {
        using var dialog = new Form
        {
            Text = "选择地区人群",
            Width = 430,
            Height = 460,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            BackColor = Color.White,
            Font = Font,
            Icon = Icon
        };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(18) };
        dialog.Controls.Add(flow);
        flow.Controls.Add(new Label { Text = "按地区选择更适合当地人群的早餐/中餐/晚餐", Width = 360, Height = 34, ForeColor = Ink, Font = new Font(Font.FontFamily, 11, FontStyle.Bold) });
        foreach (var region in _data.Regions)
        {
            var radio = new RadioButton { Text = $"{region.Name}：{region.Hint}", Width = 360, Height = 34, Checked = region.Id == _selectedRegionId, Tag = region.Id };
            flow.Controls.Add(radio);
        }
        var ok = new Button { Text = "应用地区并重新计算", Width = 360, Height = 44, BackColor = Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) =>
        {
            var selected = flow.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked)?.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(selected)) _selectedRegionId = selected!;
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };
        flow.Controls.Add(ok);
        if (dialog.ShowDialog(this) == DialogResult.OK) Regenerate();
    }

    private static Control Field(string label, Control control, string unit)
    {
        var panel = new Panel { Width = 292, Height = 74, Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = label, Location = new Point(0, 0), Size = new Size(292, 22), ForeColor = Muted, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold) });
        control.Location = new Point(0, 26);
        control.Width = 218;
        panel.Controls.Add(control);
        panel.Controls.Add(new Label { Text = unit, Location = new Point(226, 30), Size = new Size(66, 26), ForeColor = Ink, Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        return panel;
    }

    private void ConfigureNumber(NumericUpDown input, int min, int max, int value, string suffix)
    {
        input.Minimum = min;
        input.Maximum = max;
        input.Value = value;
        input.Width = 292;
        input.Height = 34;
        input.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        input.ThousandsSeparator = false;
        input.ValueChanged += (_, _) => Regenerate();
    }

    private static Control MetricCard(string label, string value, string hint)
    {
        var panel = new RoundPanel { Width = 170, Height = 92, Radius = 20, BackColor = Color.FromArgb(249, 252, 249), Margin = new Padding(0, 0, 12, 10) };
        panel.Controls.Add(new Label { Text = label, Location = new Point(14, 10), Size = new Size(140, 20), ForeColor = Muted, Font = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold) });
        panel.Controls.Add(new Label { Text = value, Location = new Point(14, 30), Size = new Size(140, 32), ForeColor = Ink, Font = new Font("Microsoft YaHei UI", 18, FontStyle.Bold) });
        panel.Controls.Add(new Label { Text = hint, Location = new Point(14, 64), Size = new Size(140, 18), ForeColor = Muted, Font = new Font("Microsoft YaHei UI", 8) });
        return panel;
    }

    private static Control Title(string text, int size) => new Label { Text = text, AutoSize = false, Width = 760, Height = 42, ForeColor = Ink, Font = new Font("Microsoft YaHei UI", size, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };

    private static Control Paragraph(string text) => new Label { Text = text, AutoSize = false, Width = 760, Height = 64, ForeColor = Muted, Font = new Font("Microsoft YaHei UI", 10), Margin = new Padding(0, 0, 0, 8) };

    private static Control CompatibilityCard(string title, string status, string detail)
    {
        var card = new RoundPanel { Width = 760, Height = 92, Radius = 20, BackColor = Color.FromArgb(249, 252, 249), Margin = new Padding(0, 0, 0, 10) };
        card.Controls.Add(new Label { Text = title, Location = new Point(18, 14), Size = new Size(150, 26), ForeColor = Ink, Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = status, Location = new Point(180, 17), Size = new Size(170, 24), ForeColor = Primary, Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = detail, Location = new Point(18, 48), Size = new Size(700, 28), ForeColor = Muted, Font = new Font("Microsoft YaHei UI", 9) });
        return card;
    }

    private static Color TryColor(string value, Color fallback)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }

    private static Color Blend(Color color, Color back, double amountBack)
    {
        var amountColor = 1 - amountBack;
        return Color.FromArgb(
            (int)(color.R * amountColor + back.R * amountBack),
            (int)(color.G * amountColor + back.G * amountBack),
            (int)(color.B * amountColor + back.B * amountBack));
    }

    private static void OpenPublishFolder()
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        try
        {
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开目录：{folder}\n{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

public sealed class RoundPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 18;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        using var pen = new Pen(Color.FromArgb(225, 232, 224));
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        using var path = RoundedRect(ClientRectangle, Radius);
        Region = new Region(path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var rect = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(rect, 180, 90);
        rect.X = bounds.Right - diameter - 1;
        path.AddArc(rect, 270, 90);
        rect.Y = bounds.Bottom - diameter - 1;
        path.AddArc(rect, 0, 90);
        rect.X = bounds.Left;
        path.AddArc(rect, 90, 90);
        path.CloseFigure();
        return path;
    }
}







