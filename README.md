# HiddenWindow v1.2

[中文版](./README_zh.md)

HiddenWindow is a Windows smart window management tool: when you drag a window to the top/left/right/bottom edge of the screen, it auto-snaps and hides off-screen, leaving only a 5px thin edge visible. Moving the mouse to that edge slides the window back out and brings it to the foreground; moving the mouse away hides it again. Supports multi-monitor setups and excludes maximized/fullscreen windows.

## Version
- Current: `v1.2`

## Features
- Snap & hide to top/left/right/bottom edges (5px visible edge)
- Mouse-to-edge slide-out, leave-to-hide
- Auto bring-to-front & activate on slide-out
- Multi-monitor support
- Excludes fullscreen/maximized windows
- Edge detection sensitivity: 3 levels (20/50/70 px)
- Animation speed: 3 levels (Fast/Medium/Slow), default Medium
- System tray with auto-start toggle

## Changelog
### v1.2 (2026-05-04)
- Added: bottom edge docking support

### v1.1 (2026-03-07)
- Added: hidden window auto brings to top and activates focus on slide-out
- Optimization: triggers slide-out and top-most only when cursor enters the trigger zone from the corresponding edge
- Updated: assembly and file version to `1.1.0.0`

## Running
1. Run via `dotnet` or published `exe`
2. Adjust sensitivity, animation speed, and auto-start from the tray icon menu

## Project Structure
- `src/HiddenWindow` — main program source
- `README.md` — English documentation
- `README_zh.md` — Chinese documentation
- `LICENSE` — license

## Source Files
- `src/HiddenWindow/Program.cs` — entry point
- `src/HiddenWindow/MainForm.cs` — tray icon, config menu, auto-start
- `src/HiddenWindow/DockManager.cs` — snap, hide, show, animation logic
- `src/HiddenWindow/WinApi.cs` — Win32 API P/Invoke wrappers
- `src/HiddenWindow/Settings.cs` — config read/write and defaults

## Build
Requires .NET 8 SDK.

```powershell
dotnet build .\src\HiddenWindow\HiddenWindow.csproj -c Release
```

## Publish (generate exe)
```powershell
dotnet publish .\src\HiddenWindow\HiddenWindow.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

发布产物在：
`src/HiddenWindow/bin/Release/net8.0-windows/win-x64/publish/HiddenWindow.exe`

## 说明
- 首次运行将创建配置文件：`%AppData%\HiddenWindow\settings.json`
- 关闭程序可在托盘菜单中选择“退出”
