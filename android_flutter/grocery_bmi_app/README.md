# Android Flutter 软件版

这是“买菜 BMI 选购模型”的 Android Flutter 工程源码。

## 说明

- 预算方式：手动输入每日预算。
- 平台目标：Android。
- 本机暂不编译 Android；GitHub Actions 会安装 Flutter SDK 并构建 APK。

## 云端构建

推送 tag 或手动触发 `.github/workflows/build-release.yml` 后，会生成：

- Windows 软件压缩包
- Android APK
- GitHub Release
