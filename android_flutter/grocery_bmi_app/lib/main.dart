import 'dart:convert';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

void main() => runApp(const GroceryBmiApp());

class GroceryBmiApp extends StatelessWidget {
  const GroceryBmiApp({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
        title: '买菜 BMI 选购模型',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF16A34A)), useMaterial3: true),
        home: const PlannerPage(),
      );
}

class PlannerPage extends StatefulWidget {
  const PlannerPage({super.key});
  @override
  State<PlannerPage> createState() => _PlannerPageState();
}

class _PlannerPageState extends State<PlannerPage> {
  final _height = TextEditingController(text: '170');
  final _weight = TextEditingController(text: '65');
  final _people = TextEditingController(text: '1');
  final _days = TextEditingController(text: '3');
  final _budget = TextEditingController(text: '50');
  String _region = 'national';
  AppData? _data;
  BudgetPlan? _plan;

  @override
  void initState() { super.initState(); _load(); }

  Future<void> _load() async {
    final raw = await rootBundle.loadString('assets/data/dishes.json');
    final data = AppData.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    setState(() { _data = data; _plan = _makePlan(data); });
  }

  double _num(TextEditingController c, double fallback) => double.tryParse(c.text.trim()) ?? fallback;
  BudgetPlan _makePlan(AppData data) => GroceryModel(data.dishes, data.marketPrices).generateBudgetPlan(
        budget: _num(_budget, 50).clamp(10, 500).toDouble(),
        people: _num(_people, 1).round().clamp(1, 6).toInt(),
        days: _num(_days, 3).round().clamp(1, 7).toInt(),
        heightCm: _num(_height, 170),
        weightKg: _num(_weight, 65),
        region: _region,
      );
  void _generate() { final data = _data; if (data != null) setState(() => _plan = _makePlan(data)); }

  @override
  Widget build(BuildContext context) {
    final data = _data, plan = _plan;
    if (data == null || plan == null) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    final regionInfo = data.regions.firstWhere((r) => r.id == plan.region, orElse: () => data.regions.first);
    return Scaffold(
      backgroundColor: const Color(0xFFF4F8F2),
      appBar: AppBar(title: Text('买菜 BMI · ${regionInfo.name}'), backgroundColor: const Color(0xFF16A34A), foregroundColor: Colors.white),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        _Header(plan: plan, region: regionInfo),
        const SizedBox(height: 12),
        _InputCard(height: _height, weight: _weight, people: _people, days: _days, budget: _budget, regions: data.regions, selectedRegion: _region, onRegion: (v) => setState(() { _region = v; _plan = _makePlan(data); }), onGenerate: _generate),
        const SizedBox(height: 12),
        _Summary(plan: plan, dishCount: data.dishes.length, region: regionInfo),
        const SizedBox(height: 12),
        for (final day in plan.dailyPlans) _DayCard(day: day, profile: plan.profile),
        _ShoppingList(plan: plan),
        const SizedBox(height: 12),
        _DishGallery(data: data, profile: plan.profile),
      ]),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.plan, required this.region});
  final BudgetPlan plan; final RegionOption region;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(18), decoration: BoxDecoration(gradient: const LinearGradient(colors: [Color(0xFF16A34A), Color(0xFF0F766E)]), borderRadius: BorderRadius.circular(24)),
    child: Row(children: [Image.asset('assets/logo.png', width: 78), const SizedBox(width: 14), Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text('BMI ${plan.profile.bmi.toStringAsFixed(1)} · ${plan.bmiCategory.label}', style: const TextStyle(color: Colors.white70, fontWeight: FontWeight.w800)),
      Text('¥${plan.budget.toStringAsFixed(0)}/天 · 早中晚三餐', style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.w900)),
      Text('${region.name}：${region.hint}', style: const TextStyle(color: Colors.white70)),
    ]))]),
  );
}

class _InputCard extends StatelessWidget {
  const _InputCard({required this.height, required this.weight, required this.people, required this.days, required this.budget, required this.regions, required this.selectedRegion, required this.onRegion, required this.onGenerate});
  final TextEditingController height, weight, people, days, budget; final List<RegionOption> regions; final String selectedRegion; final ValueChanged<String> onRegion; final VoidCallback onGenerate;
  @override
  Widget build(BuildContext context) => Card(elevation: 0, color: Colors.white, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(22)), child: Padding(padding: const EdgeInsets.all(14), child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
    const Text('生成参数', style: TextStyle(fontSize: 19, fontWeight: FontWeight.w900)), const SizedBox(height: 10),
    Wrap(spacing: 10, runSpacing: 10, children: [_NumberField('身高', height, 'cm'), _NumberField('体重', weight, 'kg'), _NumberField('人数', people, '人'), _NumberField('天数', days, '天'), _NumberField('手动预算', budget, '元/天')]),
    const SizedBox(height: 10), DropdownButtonFormField<String>(value: selectedRegion, decoration: InputDecoration(labelText: '选择地区人群', border: OutlineInputBorder(borderRadius: BorderRadius.circular(16))), items: [for (final r in regions) DropdownMenuItem(value: r.id, child: Text('${r.name} · ${r.hint}'))], onChanged: (v) { if (v != null) onRegion(v); }),
    const SizedBox(height: 12), FilledButton.icon(onPressed: onGenerate, icon: const Icon(Icons.restaurant_menu), label: const Text('按地区和三餐预算生成菜品')),
  ])));
}

class _NumberField extends StatelessWidget { const _NumberField(this.label, this.controller, this.unit); final String label, unit; final TextEditingController controller; @override Widget build(BuildContext context) => SizedBox(width: 170, child: TextField(controller: controller, keyboardType: TextInputType.number, decoration: InputDecoration(labelText: label, suffixText: unit, border: OutlineInputBorder(borderRadius: BorderRadius.circular(16))))); }
class _Summary extends StatelessWidget { const _Summary({required this.plan, required this.dishCount, required this.region}); final BudgetPlan plan; final int dishCount; final RegionOption region; @override Widget build(BuildContext context) => Wrap(spacing: 10, runSpacing: 10, children: [_Metric('BMI', plan.profile.bmi.toStringAsFixed(1), plan.bmiCategory.label), _Metric('预算', '¥${plan.budget.toStringAsFixed(0)}', '含早中晚'), _Metric('地区', region.name, '人群偏好'), _Metric('菜品池', '$dishCount', '全国早中晚餐'), _Metric('总价', '¥${plan.grandTotal.toStringAsFixed(1)}', '${plan.days}天')]); }
class _Metric extends StatelessWidget { const _Metric(this.label, this.value, this.hint); final String label, value, hint; @override Widget build(BuildContext context) => SizedBox(width: 158, child: Card(elevation: 0, color: Colors.white, child: Padding(padding: const EdgeInsets.all(12), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(label, style: const TextStyle(color: Colors.black54, fontWeight: FontWeight.w800)), Text(value, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900)), Text(hint, style: const TextStyle(color: Colors.black54))])))); }

class _DayCard extends StatelessWidget { const _DayCard({required this.day, required this.profile}); final DayPlan day; final Profile profile; @override Widget build(BuildContext context) => Card(elevation: 0, color: Colors.white, margin: const EdgeInsets.only(bottom: 12), shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(22)), child: Padding(padding: const EdgeInsets.all(14), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text('DAY ${day.day} · 三餐 ¥${day.total.cost.toStringAsFixed(1)} · BMI ${day.total.bmiValue.toStringAsFixed(0)}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w900)), _MealBlock('🌅 早餐', day.breakfast, day.breakfastSummary, profile, 'breakfast'), _MealBlock('☀️ 中餐', day.lunch, day.lunchSummary, profile, 'lunch'), _MealBlock('🌙 晚餐', day.dinner, day.dinnerSummary, profile, 'dinner')]))); }
class _MealBlock extends StatelessWidget { const _MealBlock(this.title, this.dishes, this.summary, this.profile, this.meal); final String title, meal; final List<Dish> dishes; final MealSummary summary; final Profile profile; @override Widget build(BuildContext context) => Container(margin: const EdgeInsets.only(top: 10), padding: const EdgeInsets.all(10), decoration: BoxDecoration(color: const Color(0xFFF9FCF9), borderRadius: BorderRadius.circular(16)), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text('$title · ¥${summary.cost.toStringAsFixed(1)} · ${summary.kcal.toStringAsFixed(0)}kcal', style: const TextStyle(fontWeight: FontWeight.w900, color: Color(0xFF16A34A))), for (final d in dishes) _DishTile(d, profile, meal)])); }
class _DishTile extends StatelessWidget { const _DishTile(this.dish, this.profile, this.meal); final Dish dish; final Profile profile; final String meal; @override Widget build(BuildContext context) => ListTile(contentPadding: EdgeInsets.zero, leading: CircleAvatar(backgroundColor: _color(dish.color).withOpacity(.14), child: Text(dish.icon)), title: Text(dish.name, style: const TextStyle(fontWeight: FontWeight.w900)), subtitle: Text('${dish.style} · ¥${dish.cost.toStringAsFixed(1)} · ${dish.kcal.toStringAsFixed(0)}kcal'), trailing: Chip(label: Text(GroceryModel.calculateDishBmiValue(dish, profile, meal).toStringAsFixed(0)), backgroundColor: const Color(0xFFDCFCE7))); }
class _ShoppingList extends StatelessWidget { const _ShoppingList({required this.plan}); final BudgetPlan plan; @override Widget build(BuildContext context) => Card(elevation: 0, color: Colors.white, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(22)), child: Padding(padding: const EdgeInsets.all(14), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text('合并购物清单 · ¥${plan.grandTotal.toStringAsFixed(1)}', style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w900)), for (final i in plan.shoppingList) ListTile(contentPadding: EdgeInsets.zero, title: Text(i.name, style: const TextStyle(fontWeight: FontWeight.w800)), subtitle: Text('用于：\ · \ · \kg × ¥\/kg'), trailing: Text('¥\', style: const TextStyle(color: Color(0xFF16A34A), fontWeight: FontWeight.w900))) ]))); }
class _DishGallery extends StatelessWidget { const _DishGallery({required this.data, required this.profile}); final AppData data; final Profile profile; @override Widget build(BuildContext context) { final list = [...data.dishes]..sort((a,b)=>GroceryModel.calculateDishBmiValue(b, profile, b.meals.first).compareTo(GroceryModel.calculateDishBmiValue(a, profile, a.meals.first))); return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [const Text('全国早中晚餐菜品样式池', style: TextStyle(fontSize: 19, fontWeight: FontWeight.w900)), Wrap(spacing: 8, runSpacing: 8, children: [for (final d in list.take(24)) Chip(avatar: Text(d.icon), label: Text('${d.name} ¥${d.cost.toStringAsFixed(1)}'))])]); }}

Color _color(String hex) { final value = int.tryParse(hex.replaceFirst('#', ''), radix: 16) ?? 0x16A34A; return Color(0xFF000000 | value); }

class AppData { AppData({required this.regions, required this.dishes, required this.marketPrices}); final List<RegionOption> regions; final List<Dish> dishes; final List<MarketPrice> marketPrices; factory AppData.fromJson(Map<String,dynamic> j) => AppData(regions: (j['regions'] as List? ?? [{'id':'national','name':'全国通用','hint':'兼顾南北大众口味'}]).map((e)=>RegionOption.fromJson(Map<String,dynamic>.from(e as Map))).toList(), dishes: (j['dishes'] as List).map((e)=>Dish.fromJson(Map<String,dynamic>.from(e as Map))).toList(), marketPrices: (j['marketPrices'] as List? ?? []).map((e)=>MarketPrice.fromJson(Map<String,dynamic>.from(e as Map))).toList()); }
class MarketPrice { MarketPrice({required this.item, required this.category, required this.nationalAvg, required this.regions}); final String item, category; final double nationalAvg; final Map<String,double> regions; factory MarketPrice.fromJson(Map<String,dynamic> j)=>MarketPrice(item:j['item'], category:j['category'], nationalAvg:(j['nationalAvg'] as num).toDouble(), regions: (j['regions'] as Map<String,dynamic>).map((k,v)=>MapEntry(k,(v as num).toDouble()))); }
class RegionOption { RegionOption({required this.id, required this.name, required this.hint}); final String id,name,hint; factory RegionOption.fromJson(Map<String,dynamic> j)=>RegionOption(id:j['id'] as String, name:j['name'] as String, hint:j['hint'] as String? ?? ''); }
class Dish { Dish({required this.id, required this.name, required this.style, required this.cuisine, required this.type, required this.meals, required this.regions, required this.kcal, required this.protein, required this.fat, required this.carbs, required this.fiber, required this.cost, required this.ingredients, required this.tags, required this.color, required this.icon}); final String id,name,style,cuisine,type,color,icon; final List<String> meals,regions,ingredients,tags; final double kcal,protein,fat,carbs,fiber,cost; factory Dish.fromJson(Map<String,dynamic> j)=>Dish(id:j['id'], name:j['name'], style:j['style'], cuisine:j['cuisine'], type:j['type'], meals:List<String>.from(j['meals']), regions:List<String>.from(j['regions'] ?? ['national']), kcal:(j['kcal'] as num).toDouble(), protein:(j['protein'] as num).toDouble(), fat:(j['fat'] as num).toDouble(), carbs:(j['carbs'] as num).toDouble(), fiber:(j['fiber'] as num).toDouble(), cost:(j['cost'] as num).toDouble(), ingredients:List<String>.from(j['ingredients']), tags:List<String>.from(j['tags']), color:j['color'], icon:j['icon']); Dish withCost(double newCost)=>Dish(id:id,name:name,style:style,cuisine:cuisine,type:type,meals:meals,regions:regions,kcal:kcal,protein:protein,fat:fat,carbs:carbs,fiber:fiber,cost:newCost,ingredients:ingredients,tags:tags,color:color,icon:icon); }
class Profile { Profile({required this.heightCm, required this.weightKg}); final double heightCm,weightKg; double get bmi => GroceryModel.calculateBmi(heightCm, weightKg); }
class BmiCategory { BmiCategory(this.label,this.tone,this.advice); final String label,tone,advice; }
class MealSummary { MealSummary(this.cost,this.kcal,this.protein,this.fat,this.carbs,this.fiber,this.bmiValue); final double cost,kcal,protein,fat,carbs,fiber,bmiValue; }
class DayPlan { DayPlan({required this.day, required this.breakfast, required this.lunch, required this.dinner, required this.breakfastSummary, required this.lunchSummary, required this.dinnerSummary, required this.total}); final int day; final List<Dish> breakfast,lunch,dinner; final MealSummary breakfastSummary,lunchSummary,dinnerSummary,total; }
class ShoppingItem { ShoppingItem({required this.name, required this.portions, required this.estimatedKg, required this.unitPrice, required this.estimatedCost, required this.category, required this.dishes}); final String name, category; final int portions; final double estimatedKg, unitPrice, estimatedCost; final List<String> dishes; }
class BudgetPlan { BudgetPlan({required this.budget, required this.people, required this.days, required this.region, required this.profile, required this.bmiCategory, required this.dailyPlans, required this.shoppingList, required this.grandTotal}); final double budget,grandTotal; final int people,days; final String region; final Profile profile; final BmiCategory bmiCategory; final List<DayPlan> dailyPlans; final List<ShoppingItem> shoppingList; }

class GroceryModel {
  GroceryModel(this.dishes, this.marketPrices); final List<Dish> dishes; final List<MarketPrice> marketPrices;
  static double round(double v,[int d=1]){final f=math.pow(10,d);return (v*f).round()/f;}
  static double calculateBmi(double h,double w){final m=h/100;return m<=0||w<=0?0:round(w/(m*m),1);} static BmiCategory getBmiCategory(double b){if(b<18.5)return BmiCategory('偏瘦','gain','增加优质蛋白'); if(b<24)return BmiCategory('正常','fit','维持均衡'); if(b<28)return BmiCategory('超重','control','低脂高纤'); return BmiCategory('肥胖','reduce','低能量密度');}
  static double getMealEnergyTarget(double bmi,String meal){final base=meal=='breakfast'?380:meal=='lunch'?560:520; if(bmi<18.5)return (base+(meal=='breakfast'?90:130)).toDouble(); if(bmi<24)return base.toDouble(); if(bmi<28)return (base-(meal=='breakfast'?55:90)).toDouble(); return (base-(meal=='breakfast'?80:140)).toDouble();}
  static double calculateDishBmiValue(Dish d,Profile p,String meal){final bmi=p.bmi==0?22:p.bmi,target=getMealEnergyTarget(bmi,meal); final e=(34-(d.kcal-target).abs()/target*34).clamp(0,34).toDouble(); final pr=(d.protein/(meal=='breakfast'?24:32)*24).clamp(0,24).toDouble(); final fi=(d.fiber/(meal=='breakfast'?6:8)*18).clamp(0,18).toDouble(); final fat=((d.fat-(meal=='breakfast'?14:18))*.7).clamp(0,12).toDouble(); final bonus=['vegetable','soup','lean-protein','breakfast'].contains(d.type)?8:3; final cost=(16-(d.cost-(meal=='breakfast'?9:14)).abs()*.45).clamp(4,16).toDouble(); final adj=bmi>=24&&d.kcal<target?5:bmi<18.5&&d.protein>=18?5:0; return round((e+pr+fi+bonus+cost+adj-fat).clamp(0,100).toDouble(),0);}
  MarketPrice priceInfo(String ing)=>marketPrices.firstWhere((p)=>p.item==ing||ing.contains(p.item)||p.item.contains(ing), orElse:()=>MarketPrice(item:ing,category:'未分类',nationalAvg:10,regions:{'national':10})); double unitPrice(String ing,String region){final p=priceInfo(ing);return p.regions[region]??p.regions['national']??p.nationalAvg;} double qty(String ing,Dish d){if(RegExp('酱|醋|椒|葱|姜|蒜|糖|油|料|茶|淀|可').hasMatch(ing))return .012;if(RegExp('米|面|粉|饭|馍|麦|粥|包|油条').hasMatch(ing))return d.type=='breakfast' ? 0.11 : 0.09;if(RegExp('牛|羊|猪|肉|排骨|鸡|鱼|虾|蛤|海蛎').hasMatch(ing))return d.type=='soup' ? 0.12 : 0.14;if(RegExp('豆腐|腐竹|豆花|黄豆|牛奶|鸡蛋|皮蛋').hasMatch(ing))return .12;if(RegExp('蘑|菇|木耳|笋|海带').hasMatch(ing))return .045;return d.type=='vegetable' ? 0.18 : 0.10;} double estimateDishCost(Dish d,String region)=>round(math.max(1.5,d.ingredients.fold(0.0,(s,i)=>s+unitPrice(i,region)*qty(i,d))).toDouble(),1);
  MealSummary sum(List<Dish> c,Profile p,String meal){final len=math.max(1,c.length); return MealSummary(round(c.fold(0.0,(s,d)=>s+d.cost),1), round(c.fold(0.0,(s,d)=>s+d.kcal),0), round(c.fold(0.0,(s,d)=>s+d.protein),1), round(c.fold(0.0,(s,d)=>s+d.fat),1), round(c.fold(0.0,(s,d)=>s+d.carbs),1), round(c.fold(0.0,(s,d)=>s+d.fiber),1), round(c.fold(0.0,(s,d)=>s+calculateDishBmiValue(d,p,meal))/len,0));}
  BudgetPlan generateBudgetPlan({required double budget, required int people, required int days, required double heightCm, required double weightKg, required String region}){people=people.clamp(1,6).toInt();days=days.clamp(1,7).toInt();final p=Profile(heightCm:heightCm,weightKg:weightKg);final used=<String,int>{};final daily=<DayPlan>[];final br=budget <= 30 ? 0.25 : 0.22,lr=budget <= 30 ? 0.38 : 0.40; for(var day=1;day<=days;day++){final b=_choose('breakfast',budget*br/people,p,{},used,region,day*11);final l=_choose('lunch',budget*lr/people,p,b.map((e)=>e.id).toSet(),used,region,day*17);final d=_choose('dinner',budget*(1-br-lr)/people,p,{...b.map((e)=>e.id),...l.map((e)=>e.id)},used,region,day*31);for(final x in [...b,...l,...d]){used[x.id]=(used[x.id]??0)+1;}final bs=sum(b,p,'breakfast'),ls=sum(l,p,'lunch'),ds=sum(d,p,'dinner');final total=MealSummary(round((bs.cost+ls.cost+ds.cost)*people,1),round((bs.kcal+ls.kcal+ds.kcal)*people,0),round((bs.protein+ls.protein+ds.protein)*people,1),round((bs.fat+ls.fat+ds.fat)*people,1),round((bs.carbs+ls.carbs+ds.carbs)*people,1),round((bs.fiber+ls.fiber+ds.fiber)*people,1),round((bs.bmiValue+ls.bmiValue+ds.bmiValue)/3,0));daily.add(DayPlan(day:day,breakfast:b,lunch:l,dinner:d,breakfastSummary:bs,lunchSummary:ls,dinnerSummary:ds,total:total));}return BudgetPlan(budget:budget,people:people,days:days,region:region,profile:p,bmiCategory:getBmiCategory(p.bmi),dailyPlans:daily,shoppingList:shopping(daily,people,region),grandTotal:round(daily.fold(0.0,(s,d)=>s+d.total.cost),1));}
  List<Dish> _choose(String meal,double target,Profile p,Set<String> forbid,Map<String,int> used,String region,int seed){final cand=dishes.where((d)=>d.meals.contains(meal)&&!forbid.contains(d.id)).map((d)=>d.withCost(estimateDishCost(d,region))).toList()..sort((a,b)=>(calculateDishBmiValue(b,p,meal)+_r(b,region)).compareTo(calculateDishBmiValue(a,p,meal)+_r(a,region)));final source=cand.take(meal=='breakfast'?24:34).toList();final combos=_combos(source,meal=='breakfast'||target<20?1:2,meal=='breakfast'?1:target>=45?4:target>=22?3:2);var best=<Dish>[];var bestScore=double.negativeInfinity;for(final c in combos){final s=sum(c,p,meal);final score=c.fold(0.0,(v,d)=>v+calculateDishBmiValue(d,p,meal)+_r(d,region)-(used[d.id]??0)*6)-(s.cost-target).abs()*3.2+(seed%9)/10; if(score>bestScore){bestScore=score;best=c;}}return best;}
  double _r(Dish d,String r){if(r=='national')return d.regions.contains('national')?6:2;if(d.regions.contains(r))return 18;return d.regions.contains('national')?8:0;} List<List<Dish>> _combos(List<Dish> items,int min,int max){final res=<List<Dish>>[];void walk(int i,List<Dish> st){if(st.length>=min)res.add([...st]);if(st.length==max)return;for(var x=i;x<items.length;x++){st.add(items[x]);walk(x+1,st);st.removeLast();}}walk(0,[]);return res;} List<ShoppingItem> shopping(List<DayPlan> days,int people,String region){final map=<String,({int count,double kg,double cost,double unit,String category,Set<String> dishes})>{};for(final day in days){for(final d in [...day.breakfast,...day.lunch,...day.dinner]){for(final ing in d.ingredients){final p=priceInfo(ing);final u=unitPrice(ing, region);final q=qty(ing,d)*people;final old=map[ing]??(count:0,kg:0,cost:0,unit:u,category:p.category,dishes:<String>{});old.dishes.add(d.name);map[ing]=(count:old.count+people,kg:old.kg+q,cost:old.cost+u*q,unit:u,category:p.category,dishes:old.dishes);}}}final list=map.entries.map((e)=>ShoppingItem(name:e.key,portions:e.value.count,estimatedKg:round(e.value.kg,3),unitPrice:round(e.value.unit,1),estimatedCost:round(e.value.cost,1),category:e.value.category,dishes:e.value.dishes.take(4).toList())).toList();list.sort((a,b)=>b.estimatedCost.compareTo(a.estimatedCost));return list;}
}





