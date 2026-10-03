using System.Runtime.InteropServices;

namespace HiddenWindow.Core;

/// <summary>
/// Win32 结构体定义，同时用于几何计算与 P/Invoke。
/// 单独放在 Core 中，便于不依赖 Windows 的单元测试直接构造。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct MONITORINFO
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}
