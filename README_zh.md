# HiddenWindow v1.3

[English](./README.md)

HiddenWindow 是一个 Windows 智能窗口管理工具：当用户将窗口拖动到屏幕的上/左/右/下边缘时，窗口自动吸附并隐藏到屏幕外，仅保留细边可见；鼠标移动到边缘时窗口滑出显示并自动置顶到前台，鼠标离开窗口区域后再次自动隐藏。支持多显示器，排除最大化与全屏窗口。

## 下载

每次发布都会通过 GitHub Actions 自动编译生成 `HiddenWindow.exe`。

👉 **[下载最新版本](https://github.com/Maziyang2/HiddenWindow/releases/latest)**

下载即用，无需安装。

## 功能
- 顶部/左侧/右侧/底部边缘吸附隐藏
- 鼠标到边缘自动滑出，离开后自动隐藏
- 滑出显示时自动置顶并激活目标窗口
- 多显示器支持
- 排除全屏/最大化窗口
- 全局热键 `Ctrl+Alt+H` 暂停/恢复吸附
- 设置窗口：滑块自由调节灵敏度、可见边宽度、动画时长、隐藏延迟
- 平滑缓入缓出动画
- 边缘提示 — 鼠标悬停在隐藏窗口边缘时显示窗口标题
- 动画速度：三档预设 + 自定义时长
- 边缘检测灵敏度：10–100 px 可调
- 托盘常驻，提供开机自启开关
- 启动时自动检查更新

## 更新日志

详细更新内容请查看 [Releases 页面](https://github.com/Maziyang2/HiddenWindow/releases)。

### v1.3 (2026-05-05)
- **新增：** 设置窗口，支持滑块调节所有选项
- **新增：** 全局热键 `Ctrl+Alt+H` 暂停/恢复吸附
- **新增：** 边缘提示 — 鼠标悬停时显示窗口标题
- **新增：** 自动更新检查
- **新增：** 动画时长自定义 + 缓入缓出曲线
- **新增：** 隐藏延迟可调（50–2000 ms）
- **新增：** 可见边宽度可调（2–15 px）
- **修复：** 动画改为平滑缓动，不再线性移动

### v1.2 (2026-05-04)
- **新增：** 底部边缘吸附隐藏
- **新增：** GitHub Actions 自动编译发布流程

### v1.1 (2026-03-07)
- **新增：** 隐藏窗口滑出时自动置顶并激活焦点
- **修复：** 减少非预期抢焦点，仅对应边缘触发滑出与置顶
- **变更：** 程序集版本更新为 `1.1.0.0`

## 使用方式
1. 从 [Releases](https://github.com/Maziyang2/HiddenWindow/releases) 下载 `HiddenWindow.exe`
2. 直接运行，程序驻留在系统托盘
3. 右键托盘图标 → "设置..." 打开完整设置窗口
4. 按 `Ctrl+Alt+H` 快速暂停/恢复吸附
5. 首次运行自动创建配置文件：`%AppData%\HiddenWindow\settings.json`
6. 退出程序可在托盘菜单中选择"退出"

## 目录结构
- `src/HiddenWindow` 主程序源码
- `README.md` 英文说明
- `README_zh.md` 中文说明
- `LICENSE` 许可证
- `.github/workflows/release.yml` 自动构建工作流

## 源文件说明
- `src/HiddenWindow/Program.cs` 程序入口
- `src/HiddenWindow/MainForm.cs` 托盘、热键、更新检查
- `src/HiddenWindow/DockManager.cs` 窗口吸附、隐藏、缓动动画
- `src/HiddenWindow/SettingsForm.cs` 设置窗口（滑块调节）
- `src/HiddenWindow/EdgeHintForm.cs` 边缘悬停标题提示
- `src/HiddenWindow/WinApi.cs` Win32 API P/Invoke 封装
- `src/HiddenWindow/Settings.cs` 配置读写与默认值

## 从源码构建
需要安装 .NET 8 SDK。

```powershell
dotnet build .\src\HiddenWindow\HiddenWindow.csproj -c Release
```

```powershell
dotnet publish .\src\HiddenWindow\HiddenWindow.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

输出路径：`src/HiddenWindow/bin/Release/net8.0-windows/win-x64/publish/HiddenWindow.exe`
