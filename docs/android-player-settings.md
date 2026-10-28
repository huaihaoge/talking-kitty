# Android Player Settings 核对清单

> 本清单需要在 **Unity Editor** 中手动应用（项目尚未在 Editor 中打开过，`ProjectSettings/ProjectSettings.asset` 还未生成）。打开项目后逐项核对。

打开项目：Unity Hub → Add project from disk → 选择仓库根目录（Unity **2022.3.62f3c1**）。首次打开会导入资源并生成 `ProjectSettings/`。提交前确认 `ProjectSettings/ProjectSettings.asset` 已入库。

## File → Build Settings

- [ ] 平台切换为 **Android**（Switch Platform）
- [ ] 场景列表第 0 位为 `Assets/Scenes/Main.unity`（若尚未生成，先运行菜单 **TalkingKitty → Build Main Scene**）

## File → Build Settings → Player Settings（Android）

### Identification

- [ ] **Company Name**: `huaihaoge`
- [ ] **Product Name**: `TalkingKitty`
- [ ] **Package Name**: `com.huaihaoge.talkingkitty`（Other Settings → Identification，需勾选 Override Default Package Name）
- [ ] **Minimum API Level**: `24 (Android 7.0)`
- [ ] **Target API Level**: `Automatic (highest installed)` 或设备当前适配的最新 API

### Configuration

- [ ] **Scripting Backend**: `IL2CPP`（Other Settings → Configuration）
- [ ] **Target Architectures**: 仅勾选 **ARM64**（ARMv7 不勾选）
- [ ] **Active Input Handling** 保持默认

### Resolution and Presentation

- [ ] **Default Orientation**: `Portrait`
- [ ] 勾选 Start in fullscreen / 沉浸式全屏（如版本支持）

### 权限（Permission / Manifest）

- [ ] 仅需要 **`RECORD_AUDIO`**（应用内已通过 `Permission.RequestUserPermission` 动态请求）
- [ ] **不勾选** `Write External Storage`、`Internet` 等其他权限；确认 Player Settings 的权限列表中没有 INTERNET（Unity 只有在用到网络时才会自动加入）
- [ ] 如构建出的 APK 仍带 INTERNET 权限：从 Unity 安装目录复制 `PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml` 到 `Assets/Plugins/Android/AndroidManifest.xml`，删除 `INTERNET` 权限行后重新构建（仅在此情况下才需要自定义 manifest，不要凭空手写）

### 其他

- [ ] 不集成任何广告/分析 SDK（Packages 中无 Ads 包）
- [ ] 构建 **Build** 输出到 `Builds/TalkingKitty.apk`（`Builds/` 已 gitignore）

## 提交

- [ ] 确认 `ProjectSettings/ProjectSettings.asset`（以及 manifest，如创建了）已 `git add` 并提交
- [ ] 不要提交 APK 二进制
