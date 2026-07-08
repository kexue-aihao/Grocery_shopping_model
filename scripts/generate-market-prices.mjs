import { readFile, writeFile } from 'node:fs/promises';
import { dishes } from '../src/data/dishes.js';

const priceOverrides = new Map(Object.entries({
  '牛肉': 68, '牛里脊': 72, '牛腩': 58, '牛肉片': 66, '牛肉粒': 70, '黄牛肉': 76,
  '羊肉': 72, '羊肉汤': 38, '猪肉末': 32, '肉末': 32, '猪肉馅': 32, '肉馅': 32, '五花肉': 34, '猪里脊': 38, '里脊肉': 38, '瘦猪肉': 34, '瘦肉': 34, '排骨': 46, '肋排': 52, '卤肉': 42, '火腿': 38,
  '鸡胸肉': 22, '鸡腿肉': 24, '鸡腿': 24, '鸡块': 22, '鸡翅': 32, '三黄鸡': 26, '土鸡': 38, '鸡蛋': 11,
  '鲈鱼': 46, '草鱼': 24, '武昌鱼': 30, '鳜鱼': 88, '鱼头': 34, '虾仁': 68, '虾皮': 42, '海蛎': 36, '花蛤': 18, '金枪鱼': 46,
  '北豆腐': 7, '嫩豆腐': 8, '豆腐': 7, '豆花': 6, '腐竹': 28, '豆腐皮': 16, '黄豆': 9,
  '番茄': 8, '西兰花': 13, '生菜': 8, '白菜': 4.5, '菠菜': 9, '油菜': 7, '上海青': 7.5, '空心菜': 8, '包菜': 4.5, '青菜': 6.5, '绿豆芽': 4.8, '豆芽': 4.8, '黄瓜': 7, '西葫芦': 6, '秋葵': 18, '冬瓜': 3.5, '菜花米': 10, '菜花': 8,
  '土豆': 4, '红薯': 5, '茄子': 7, '豆角': 10, '青椒': 8, '青红椒': 9, '红椒': 9, '彩椒': 15, '小米辣': 18, '干辣椒': 38, '胡萝卜': 4.5, '白萝卜': 3.5, '莲藕': 9, '山药': 12, '玉米': 6, '玉米粒': 8, '莴笋': 7, '韭菜': 9, '芹菜': 7, '香菜': 16, '大葱': 7, '小葱': 10, '葱': 8, '姜': 12, '姜丝': 12, '姜葱': 10, '蒜': 10, '姜蒜': 11,
  '香菇': 18, '榛蘑': 86, '菌菇': 22, '木耳': 48, '笋丁': 16, '笋干': 68, '海带': 8, '酸笋': 12, '荸荠': 10,
  '大米': 5.5, '米饭': 5.5, '糙米': 8, '燕麦': 16, '燕麦米': 12, '小米': 10, '糯米': 8, '杂粮面': 8, '绿豆面': 9, '米浆': 6, '米粉': 6, '米线': 6, '荞麦面': 12, '碱水面': 7, '宽面': 7, '粉丝': 9, '粉条': 9, '面筋': 10, '馍': 6, '白吉馍': 8, '薄脆': 16, '油条': 12, '油条碎': 12, '油馍头': 12, '肉包': 18,
  '牛奶': 12, '蓝莓': 60, '皮蛋': 18, '咸菜': 12, '榨菜': 12, '萝卜丁': 8, '肉松': 58,
  '豆瓣酱': 18, '剁椒': 18, '泡椒': 16, '花椒': 90, '黑胡椒': 90, '咖喱': 40, '香醋': 12, '陈醋': 10, '冰糖': 10, '生抽': 10, '酱油': 10, '蚝油': 18, '蒸鱼豉油': 18, '腐乳汁': 16, '沙姜酱': 24, '豆豉': 18, '豉油': 16, '豆瓣': 18, '酱料': 16, '蘸水': 20, '卤水': 16, '卤牛肉': 58, '卤香': 18,
  '高汤': 10, '丸子汤': 18, '酸汤': 18, '胡辣汤料': 20, '三七': 220, '枸杞': 80, '龙井茶': 360, '地瓜粉': 10, '淀粉': 8, '蛋清': 11, '可乐': 4, '番茄酱': 16, '花生': 18, '青豆': 8
}));

const regionFactor = { national: 1, north: 0.96, east: 1.12, south: 1.08, central: 0.98, southwest: 0.94, northwest: 1.03 };

function categoryOf(name) {
  if (/牛|羊|猪|肉|排骨|肋排|五花|里脊|腩|翅|鸡/.test(name)) return '肉禽类';
  if (/鱼|虾|海蛎|花蛤|蛤|蚝|金枪/.test(name)) return '水产类';
  if (/蛋|牛奶|豆腐|腐竹|豆花|黄豆|豆皮/.test(name)) return '蛋奶豆制品';
  if (/米|面|粉|馍|粥|燕麦|糯|饭|油条|包/.test(name)) return '粮油主食';
  if (/酱|醋|椒|葱|姜|蒜|糖|油|咖喱|豉|料|汤|茶|粉|淀粉|可乐/.test(name)) return '调味辅料';
  if (/菇|蘑|木耳|笋|海带/.test(name)) return '菌菇干货';
  return '蔬菜水果';
}

function defaultPrice(name) {
  for (const [key, price] of priceOverrides) if (name.includes(key) || key.includes(name)) return price;
  const category = categoryOf(name);
  return ({ '肉禽类': 32, '水产类': 36, '蛋奶豆制品': 11, '粮油主食': 8, '调味辅料': 18, '菌菇干货': 28, '蔬菜水果': 7 })[category] ?? 10;
}

const ingredients = [...new Set(dishes.flatMap((dish) => dish.ingredients))].sort((a, b) => a.localeCompare(b, 'zh-Hans-CN'));
const marketPrices = ingredients.map((name) => {
  const national = defaultPrice(name);
  const regional = Object.fromEntries(Object.entries(regionFactor).map(([region, factor]) => [region, Math.round(national * factor * 10) / 10]));
  return {
    item: name,
    category: categoryOf(name),
    unit: 'kg',
    nationalAvg: Math.round(national * 10) / 10,
    regions: regional,
    source: '公开价格监测参考：国家统计局50城主要食品均价、农业农村部农产品价格、地方发改委居民食品价格监测；项目内做地区系数标准化估算'
  };
});

let text = await readFile('src/data/dishes.js', 'utf8');
text = text.replace(/\nexport const marketPrices = \[[\s\S]*?\];\s*/m, '\n');
const exportText = `\nexport const marketPrices = ${JSON.stringify(marketPrices, null, 2)};\n`;
text = text + exportText;
await writeFile('src/data/dishes.js', text, 'utf8');
console.log(`Generated market prices: ${marketPrices.length}`);
