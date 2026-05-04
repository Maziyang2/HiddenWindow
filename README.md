# HiddenWindow v1.2

[中文版](./README_zh.md)

HiddenWindow is a Windows smart window management tool: when you drag a window to the top/left/right/bottom edge of the screen, it auto-snaps and hides off-screen, leaving only a 5px thin edge visible. Moving the mouse to that edge slides the window back out and brings it to the foreground; moving the mouse away hides it again. Supports multi-monitor setups and excludes maximized/fullscreen windows.

## Download

Pre-built `HiddenWindow.exe` is automatically generated for each release via GitHub Actions.

👉 **[Download the latest release](https://github.com/Maziyang2/HiddenWindow/releases/latest)**

Just download and run — no installation required.

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

See [Releases](https://github.com/Maziyang2/HiddenWindow/releases) for detailed changelogs.

### v1.2 (2026-05-04)
- **Added:** Bottom edge docking support
- **Added:** GitHub Actions auto-build & release workflow

### v1.1 (2026-03-07)
- **Added:** Auto bring-to-front and focus activation on slide-out
- **Fixed:** Reduced unintended focus stealing by only triggering on corresponding edge
- **Changed:** Assembly version updated to `1.1.0.0`

## Usage
1. Download `HiddenWindow.exe` from [Releases](https://github.com/Maziyang2/HiddenWindow/releases)
2. Run it — the app lives in the system tray
3. Adjust sensitivity, animation speed, and auto-start from the tray icon menu
4. First run creates config at: `%AppData%\HiddenWindow\settings.json`
5. To quit, select "退出" from the tray menu

## Project Structure
- `src/HiddenWindow` — main program source
- `README.md` — English documentation
- `README_zh.md` — Chinese documentation
- `LICENSE` — license
- `.github/workflows/release.yml` — auto-build workflow

## Source Files
- `src/HiddenWindow/Program.cs` — entry point
- `src/HiddenWindow/MainForm.cs` — tray icon, config menu, auto-start
- `src/HiddenWindow/DockManager.cs` — snap, hide, show, animation logic
- `src/HiddenWindow/WinApi.cs` — Win32 API P/Invoke wrappers
- `src/HiddenWindow/Settings.cs` — config read/write and defaults

## Build from Source
Requires .NET 8 SDK.

```powershell
dotnet build .\src\HiddenWindow\HiddenWindow.csproj -c Release
```

```powershell
dotnet publish .\src\HiddenWindow\HiddenWindow.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `src/HiddenWindow/bin/Release/net8.0-windows/win-x64/publish/HiddenWindow.exe`
