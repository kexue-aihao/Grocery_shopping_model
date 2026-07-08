# Grocery_shopping_model

买菜 BMI 选购模型是 **Windows + Android 双平台软件项目**。当前逻辑已升级为：输入每日价格预算后，按地区人群偏好生成 **早餐 / 中餐 / 晚餐** 三餐菜品样式，并把早餐价格、早餐能量 BMI 适配值一并纳入计算。

## 当前平台范围

### Windows 桌面软件

工程位置：

```text
desktop/GroceryBmi.App/GroceryBmi.App.csproj
```

本地发布后的可执行文件：

```text
dist/software/windows/GroceryBmiShopping.exe
```

运行方式：

```powershell
npm.cmd run build:windows
npm.cmd run run:windows
```

Windows 版本使用 .NET Windows Forms 原生窗口运行。程序 logo 已同时用于：

- exe 内嵌图标：`ApplicationIcon`
- 程序启动后的窗口/任务栏图标：`Icon = new Icon(...)`
- 发布输出目录中的运行时图标资源

相关文件：

```text
desktop/GroceryBmi.App/Assets/logo.ico
desktop/GroceryBmi.App/Assets/logo.png
```

### Android Flutter 软件

Flutter 工程位置：

```text
android_flutter/grocery_bmi_app
```

关键文件：

```text
android_flutter/grocery_bmi_app/pubspec.yaml
android_flutter/grocery_bmi_app/lib/main.dart
android_flutter/grocery_bmi_app/assets/logo.png
android_flutter/grocery_bmi_app/assets/data/dishes.json
```

说明：按当前要求，Android 本地暂不编译；GitHub Actions 会在云端安装 Flutter、生成 Android 平台文件、构建 APK，并发布到 GitHub Release。

## 已实现核心功能

- **公开菜市场零售单价**：已加入覆盖常见肉禽、水产、蛋奶豆制品、粮油主食、蔬菜水果、菌菇干货、调味辅料等标准食品类的零售单价表，包含全国均价和地区价格系数。
- **实际购买成本估算**：菜品价格不再只依赖固定估算价，会按菜品食材、估算用量、所选地区零售单价计算每道菜实际需要花费多少钱。
- **购物清单成本明细**：合并购物清单显示食材估算重量、地区单价（元/kg）和预计购买花费，便于按实际采购预算核算。

- **输入单位后置显示**：身高、体重、人数、天数、每日预算等输入框后方明确显示 cm、kg、人、天、元/天，Windows/Web/Android Flutter 三端均已同步。
- **全国常见家常菜扩展**：菜品池已扩展到覆盖川菜、湘菜、粤菜、鲁菜、苏菜、浙菜、闽菜、徽菜、东北菜、西北菜、西南菜、华中家常菜等常见地区菜系。
- **地区菜系 BMI 计算**：所有新增菜品均包含估算价格和营养参数，可计算 BMI 适配值，并按地区标签进行加权选购。

- **手动预算**：输入每日价格预算，不再限定固定阶梯。
- **三餐计算**：早餐、中餐、晚餐都参与每日预算、营养、BMI 适配值和购物清单。
- **早餐 BMI 能量计算**：早餐单独使用早餐能量目标，并计算早餐菜品 BMI 适配值。
- **早餐菜品与价格**：已加入豆浆油条、包子小米粥、煎饼果子、热干面、胡辣汤、肠粉、皮蛋瘦肉粥、桂林米粉、云南小锅米线、上海粢饭团、山东杂粮煎饼、肉夹馍配汤、豆花饭、鸡蛋牛奶燕麦等常用早餐和估算价格。
- **全国菜品池**：菜品覆盖全国通用、华北/东北、华东/江浙沪、华南/粤闽、华中、西南/川渝云贵、西北等区域。
- **地区选择窗口**：Windows 端新增“选择地区/人群”按钮，弹窗选择对应地区后重新计算更适合当地人群的早中晚餐。
- **Android 地区选择**：Flutter 端提供地区人群下拉选择，并按地区生成三餐。
- **同日去重**：同一天早餐、中餐、晚餐不会重复同一道菜。
- **购物清单**：按早餐/中餐/晚餐、天数和人数合并食材采购清单。
- **统一 Logo**：Windows 与 Android 共用软件 logo。

## 地区选项

当前内置地区：

- 全国通用
- 华北/东北
- 华东/江浙沪
- 华南/粤闽
- 华中
- 西南/川渝云贵
- 西北

菜品会根据 `regions` 标签进行加权，优先选择适合所选地区的人群口味和常见餐食。

## GitHub 自构建 Release 工作流

工作流文件：

```text
.github/workflows/build-release.yml
```

触发方式：

1. 手动触发 `workflow_dispatch`，可输入 release tag。
2. 推送 `v*` tag，例如：

```powershell
git tag v1.1.0
git push origin v1.1.0
```

工作流会自动完成：

- 构建 Windows 桌面软件。
- 打包 Windows 发布包：`grocery-bmi-windows.zip`。
- 安装 Flutter。
- 生成 Android 平台文件。
- 构建 Android APK：`grocery-bmi-android.apk`。
- 创建或更新 GitHub Release。
- 将 Windows zip 和 Android APK 上传为 Release 程序包。

## 本地验证命令

当前本地只验证 Windows 和项目结构，不编译 Android：

```powershell
npm.cmd run test
npm.cmd run build:windows
npm.cmd run verify:windows
npm.cmd run verify:project
```

完整本地构建验证：

```powershell
npm.cmd run build
```

已验证通过：

```text
All model tests passed.
Native Windows software artifact verified: E:\Grocery_shopping_model\dist\software\windows\GroceryBmiShopping.exe
Project structure verified for breakfast, regional Windows + Android software workflow.
```

## Logo 资源

统一 logo 源文件和导出文件：

```text
assets/logo.svg
assets/logo-1024.png
assets/logo-512.png
assets/logo-192.png
desktop/GroceryBmi.App/Assets/logo.ico
desktop/GroceryBmi.App/Assets/logo.png
android_flutter/grocery_bmi_app/assets/logo.png
```

如需重新生成 logo：

```powershell
npm.cmd run generate:logo
```

## 数据说明

菜品名称、风格和搭配灵感参考公开菜谱站点、八大菜系和各地常见家常菜资料中的代表性菜品方向，例如川菜、湘菜、粤菜、鲁菜、苏菜、浙菜、闽菜、徽菜以及东北、西北、西南、华中等地区家常菜。本项目中的价格与营养值是用于选购模型演示的估算值，不代表实时市场价或医学/营养处方。实际采购请以当地菜市场、商超或线上买菜平台当日价格为准。


## 价格数据口径

项目内新增 `marketPrices` 标准食品零售单价表，导出到：

```text
src/data/dishes.js
desktop/GroceryBmi.App/Data/dishes.json
android_flutter/grocery_bmi_app/assets/data/dishes.json
```

价格口径参考公开数据类型：

- 国家统计局 50 个城市主要食品平均价格。
- 农业农村部全国农产品批发市场价格信息。
- 各地发改委/价格监测中心公开的居民主要食品零售价格监测。

当前软件中的价格采用“全国参考均价 + 地区系数”的标准化估算方式，单位统一为 `元/kg`。软件会根据菜品食材和估算用量计算：

```text
菜品预计成本 = Σ(食材地区零售价 × 食材估算用量)
```

购物清单会进一步汇总：

```text
食材名称 / 食材类别 / 估算重量 kg / 地区单价 元/kg / 预计购买花费 元
```

注意：公开价格具有时间和地区波动，本项目用于预算估算和选购模型计算；实际采购价格请以当地菜市场、商超或线上买菜平台当日价格为准。
