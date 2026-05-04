# HiddenWindow v1.2

[English](./README.md)

HiddenWindow 是一个 Windows 智能窗口管理工具：当用户将窗口拖动到屏幕的上/左/右/下边缘时，窗口自动吸附并隐藏到屏幕外，仅保留 5px 细边可见；鼠标移动到边缘时窗口滑出显示并自动置顶到前台，鼠标离开窗口区域后再次自动隐藏。支持多显示器，排除最大化与全屏窗口。

## 下载

每次发布都会通过 GitHub Actions 自动编译生成 `HiddenWindow.exe`。

👉 **[下载最新版本](https://github.com/Maziyang2/HiddenWindow/releases/latest)**

下载即用，无需安装。

## 功能
- 顶部/左侧/右侧/底部边缘吸附隐藏（保留 5px）
- 鼠标到边缘自动滑出，离开后自动隐藏
- 滑出显示时自动置顶并激活目标窗口
- 多显示器支持
- 排除全屏/最大化窗口
- 边缘检测灵敏度三档（20/50/70 px）
- 动画速度三档（快/适中/慢），默认适中
- 托盘常驻，提供开机自启开关

## 更新日志

详细更新内容请查看 [Releases 页面](https://github.com/Maziyang2/HiddenWindow/releases)。

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
3. 托盘图标中可调整灵敏度、动画速度与自启开关
4. 首次运行自动创建配置文件：`%AppData%\HiddenWindow\settings.json`
5. 退出程序可在托盘菜单中选择"退出"

## 目录结构
- `src/HiddenWindow` 主程序源码
- `README.md` 英文说明
- `README_zh.md` 中文说明
- `LICENSE` 许可证
- `.github/workflows/release.yml` 自动构建工作流

## 源文件说明
- `src/HiddenWindow/Program.cs` 程序入口
- `src/HiddenWindow/MainForm.cs` 托盘与配置菜单、开机自启开关
- `src/HiddenWindow/DockManager.cs` 窗口吸附、隐藏、显示与动画逻辑
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
