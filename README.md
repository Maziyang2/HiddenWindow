# HiddenWindow

HiddenWindow 是一个 Windows 智能窗口管理工具：当用户将窗口拖动到屏幕上/左/右边缘时，窗口自动吸附并隐藏到屏幕外，仅保留 5px 细边可见；鼠标移动到边缘时窗口滑出显示，鼠标离开窗口区域后再次自动隐藏。支持多显示器，排除最大化与全屏窗口。

## 功能
- 顶部/左侧/右侧边缘吸附隐藏（保留 5px）
- 鼠标到边缘自动滑出，离开后自动隐藏
- 多显示器支持
- 排除全屏/最大化窗口
- 边缘检测灵敏度三档（20/50/70 px）
- 动画速度三档（快/适中/慢），默认适中
- 托盘常驻，提供开机自启开关

## 运行方式
1. 通过 `dotnet` 或发布后的 `exe` 运行
2. 托盘图标中可调整灵敏度、动画速度与自启开关

## 目录结构
- `src/HiddenWindow` 主程序源码
- `README.md` 项目说明
- `LICENSE` 许可证

## 文件说明
- `src/HiddenWindow/Program.cs` 程序入口
- `src/HiddenWindow/MainForm.cs` 托盘与配置菜单、开机自启开关
- `src/HiddenWindow/DockManager.cs` 窗口吸附、隐藏、显示与动画逻辑
- `src/HiddenWindow/WinApi.cs` Win32 API P/Invoke 封装
- `src/HiddenWindow/Settings.cs` 配置读写与默认值

## 构建
需要安装 .NET 8 SDK。

```powershell
dotnet build .\src\HiddenWindow\HiddenWindow.csproj -c Release
```

## 发布（生成 exe）
```powershell
dotnet publish .\src\HiddenWindow\HiddenWindow.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

发布产物在：
`src/HiddenWindow/bin/Release/net8.0-windows/win-x64/publish/HiddenWindow.exe`

## 说明
- 首次运行将创建配置文件：`%AppData%\HiddenWindow\settings.json`
- 关闭程序可在托盘菜单中选择“退出”

## 发行版本说明
- 可直接下载运行已打包好的exe文件

