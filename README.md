# HiddenWindow v1.3

[中文版](./README_zh.md)

HiddenWindow is a Windows smart window management tool: when you drag a window to the top/left/right/bottom edge of the screen, it auto-snaps and hides off-screen, leaving only a thin edge visible. Moving the mouse to that edge slides the window back out and brings it to the foreground; moving the mouse away hides it again. Supports multi-monitor setups and excludes maximized/fullscreen windows.

## Download

Pre-built `HiddenWindow.exe` is automatically generated for each release via GitHub Actions.

👉 **[Download the latest release](https://github.com/Maziyang2/HiddenWindow/releases/latest)**

Just download and run — no installation required.

## Features
- Snap & hide to top/left/right/bottom edges
- Mouse-to-edge slide-out, leave-to-hide
- Auto bring-to-front & activate on slide-out
- Multi-monitor support
- Excludes fullscreen/maximized windows
- Global hotkey `Ctrl+Alt+H` to pause/resume docking
- Settings window with sliders: sensitivity, visible edge width, animation duration, hide delay
- Smooth ease-in-out animation
- Edge hint — shows window title when hovering near hidden edge
- Animation speed: 3 presets + custom duration
- Edge detection sensitivity: adjustable 10–100 px
- System tray with auto-start toggle
- Auto-update check on startup

## Changelog

See [Releases](https://github.com/Maziyang2/HiddenWindow/releases) for detailed changelogs.

### v1.3 (2026-05-05)
- **Added:** Settings window with sliders for all options
- **Added:** Global hotkey `Ctrl+Alt+H` to pause/resume docking
- **Added:** Edge hint — shows window title when hovering near hidden edge
- **Added:** Auto-update checker
- **Added:** Configurable animation duration with ease-in-out curve
- **Added:** Configurable hide delay (50–2000 ms)
- **Added:** Configurable visible edge width (2–15 px)
- **Fixed:** Animation now uses smooth easing instead of linear

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
3. Right-click tray icon → "设置" for full settings dialog
4. Press `Ctrl+Alt+H` to quickly pause/resume docking
5. First run creates config at: `%AppData%\HiddenWindow\settings.json`
6. To quit, select "退出" from the tray menu

## Project Structure
- `src/HiddenWindow` — main program source
- `README.md` — English documentation
- `README_zh.md` — Chinese documentation
- `LICENSE` — license
- `.github/workflows/release.yml` — auto-build workflow

## Source Files
- `src/HiddenWindow/Program.cs` — entry point
- `src/HiddenWindow/MainForm.cs` — tray icon, hotkey, update checker
- `src/HiddenWindow/DockManager.cs` — snap, hide, show, animation with easing
- `src/HiddenWindow/SettingsForm.cs` — settings dialog with sliders
- `src/HiddenWindow/EdgeHintForm.cs` — edge hover hint overlay
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
