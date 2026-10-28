# Talking Kitty 会说话的猫咪

一款**无广告、完全离线**的 Android 应用，给孩子做"学说话"游戏。孩子对着话筒说话，猫咪会在停顿后用变高的音调回放孩子的话（类似 Talking Tom），同时内置**儿童锁**，防止孩子误触退出应用。

## 这是什么应用

- **语音回声**：自动监听，孩子说完（停顿约 1 秒）后自动变调回放，不需要按住任何按钮。
- **简单的猫形象**：MVP 阶段是 2D/低多边形的猫，之后可以换成 3D 模型。
- **无广告、无分析、无网络**：核心玩法不需要联网，没有广告 SDK。

## 麦克风权限

首次启动时会请求**麦克风权限**，这是唯一需要的敏感权限。录音只保存在内存或临时文件中，回放后即丢弃，不上传任何数据。

## 儿童锁

- 开启后：返回键、退出/设置/分享按钮都被屏蔽，尽量全屏沉浸。
- 家长解锁：长按屏幕任意角落约 **3 秒**，或点击屏幕顶部中间 **5 次**，然后输入 4 位 PIN。
- 首次进入家长界面时设置 PIN；忘记 PIN 可以清除应用数据恢复。
- 家长界面里还可以关闭儿童锁、调整音调、退出应用。
- 已知限制：系统级"主页/最近任务"仍可能离开应用；可以配合 Android 的"屏幕固定"功能作为额外保护（应用内不做实现）。

## 用 Unity Hub 打开项目

1. 安装 [Unity Hub](https://unity.com/download) 和一个 Unity LTS 版本（含 Android Build Support 模块）。
2. 在 Unity Hub 中选择 **Add → Add project from disk**，选择本仓库根目录。
3. 首次打开会自动导入 `Assets/` 并生成 `Library/` 等目录，请耐心等待。

## 构建 Android APK

1. 打开项目后，菜单 **File → Build Settings**。
2. 选择 **Android** 平台，点击 **Switch Platform**。
3. 确认场景 `Assets/Scenes/Main.unity` 已加入构建列表。
4. 点击 **Build**，输出 APK 文件。
5. 把 APK 传到手机上直接安装（侧载，不需要应用商店）。安装时允许"未知来源"即可。

详细设计文档见 [docs/superpowers/specs/2026-10-28-talking-kitty-design.md](docs/superpowers/specs/2026-10-28-talking-kitty-design.md)。

家长指南（安装与儿童锁使用说明，中文）将在后续任务中添加到 `docs/parent-guide.md`。

## 许可证

[MIT](LICENSE) © 2026 huaihaoge
