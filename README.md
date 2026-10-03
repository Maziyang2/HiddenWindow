<p align="center">
  <img src="docs/brand/hiddenwindow-mark.png" width="128" alt="HiddenWindow v2.1 icon" />
</p>

<h1 align="center">HiddenWindow</h1>

<p align="center"><strong>A refined open-source window orchestration layer for Windows.</strong></p>

<p align="center">
  Move inactive windows to the screen edge. Recall them naturally. Keep your attention at the center.
</p>

<p align="center">
  <a href="https://github.maziyang.top"><strong>Product website</strong></a>
  · <a href="https://github.com/Maziyang2/HiddenWindow/releases/latest">Latest release</a>
  · <a href="./README_zh.md">中文说明</a>
</p>

![HiddenWindow v2.1 brand cover](docs/brand/hiddenwindow-v2-social.png)

## A third place for your windows

Closing a window loses context. Minimizing it means finding it again. HiddenWindow introduces a third option: keep the window where you remember it, without keeping it in your current field of view.

Drag a window to the top, bottom, left, or right edge of any display. HiddenWindow moves it just beyond the screen, leaving a precise visible edge. Approach that edge to reveal the window; move away and it quietly retreats.

## What makes it useful

- Dock and hide on all four screen edges
- Recall windows by approaching their corresponding edge
- Match the pointer to the correct hidden window when several share one edge
- Work naturally across multi-monitor layouts
- Ignore maximized and fullscreen windows
- Pause or resume docking globally with `Ctrl + Alt + H`
- Tune edge sensitivity, visible width, animation timing, and hide delay
- Launch with Windows and check for updates automatically
- Run as a portable, single-file application

## v2.1 Minimalism system

HiddenWindow v2.1 gives the application, website, browser tab icon, and documentation one coherent visual language:

- a warm white canvas, near-black typography, and a stable gray scale;
- grid, baseline, spacing, and type hierarchy instead of decorative surfaces;
- thin, consistent lines with one restrained blue-gray focus color;
- a lighter brand mark that shows a window moving beyond the display boundary;
- a completely redesigned Settings and About experience;
- automatic interface localization based on the Windows display language;
- manual language selection: **Follow system**, **简体中文**, or **English**.

The window-docking engine remains intentionally focused. The redesign changes how HiddenWindow communicates and feels without turning a small utility into a complicated workspace product.

## Interface preview

The [Chinese-first product website](https://github.maziyang.top) includes a complete English presentation. The website and application share the same Edge Window mark, light canvas, typography hierarchy, control geometry, and product language.

## Design language

The shared v2.1 minimalism brief — colour, typography, spacing, and interaction rules — is archived in [docs/design.md](./docs/design.md). Keep its intent when evolving the Windows app, website, icon, or documentation.

## Language behavior

HiddenWindow uses the Windows UI culture on first launch:

- Simplified Chinese Windows → 简体中文
- Other display languages → English
- Manual override → Follow system / 简体中文 / English

The choice is stored locally in `%AppData%\HiddenWindow\settings.json`. No language or usage data is transmitted.

## Usage

1. Download `HiddenWindow.exe` from [Releases](https://github.com/Maziyang2/HiddenWindow/releases/latest).
2. Run it; no installer is required. A second launch exits with a notice — HiddenWindow runs as a single instance.
3. Right-click the tray icon and open **Settings**.
4. Drag any eligible window to a screen edge.
5. Move the pointer to that edge to recall the window.
6. Press `Ctrl + Alt + H` to pause or resume docking.

## Project structure

```text
src/HiddenWindow.Core/
├── DockGeometry.cs      Snap, hidden-rect, and edge-detection math
├── HintPlacement.cs     Edge hint positioning
├── Easing.cs            Animation curve and interpolation
├── AppSettings.cs       Configuration model, validation, persistence
└── InteropTypes.cs      Win32 structs shared with the app
src/HiddenWindow/
├── Program.cs           Entry point, single-instance guard, crash reporting
├── MainForm.cs          Tray lifecycle, menu, hotkey, updates
├── DockManager.cs       Docking, reveal, hide, and animation engine
├── SettingsForm.cs      v2.1 settings interface
├── AboutForm.cs         v2.1 product and website presentation
├── AppInfo.cs           Version and product constants
├── Localization.cs      Simplified Chinese and English resources
├── UiControls.cs        Shared monochrome controls and brand mark
├── EdgeHintForm.cs      Window-title hint shown at the screen edge
├── WinApi.cs            Win32 and DWM interop
└── Assets/              Windows application icon
tests/HiddenWindow.Tests/  xUnit tests for the core
scripts/
└── generate_brand_assets.py  Reproducible PNG and ICO generation
```

## Build from source

Requires the .NET 8 SDK on Windows, or a recent .NET SDK with Windows targeting enabled.

```powershell
dotnet build .\src\HiddenWindow\HiddenWindow.csproj -c Release
```

Run the core unit tests (docking geometry, hint placement, easing, settings):

```powershell
dotnet test .\tests\HiddenWindow.Tests\HiddenWindow.Tests.csproj -c Release
```

Portable single-file build:

```powershell
dotnet publish .\src\HiddenWindow\HiddenWindow.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output:

```text
src/HiddenWindow/bin/Release/net8.0-windows/win-x64/publish/HiddenWindow.exe
```

## 中文说明

HiddenWindow 是一款轻量、克制的 Windows 窗口编排工具。把暂时不用的窗口拖到屏幕边缘，它会自动收进屏幕外；鼠标靠近对应边缘时，窗口自然滑回，离开后再次隐藏。

v2.1 以极简主义重新统一软件、官网、浏览器图标和文档：亮色纸面、黑白灰秩序、细线与极少量强调色。软件默认跟随 Windows 显示语言，也可以在设置中手动选择“跟随系统 / 简体中文 / English”。完整中文文档请阅读 [README_zh.md](./README_zh.md)。

## Status

HiddenWindow v2.1.2 is the current stable release. Download the portable, self-contained `HiddenWindow.exe` from the [latest release](https://github.com/Maziyang2/HiddenWindow/releases/latest); no installer or separate .NET runtime is required.

## License

HiddenWindow is released under the [MIT License](./LICENSE).

Designed for focus. Built by [Maziyang2](https://github.com/Maziyang2).
