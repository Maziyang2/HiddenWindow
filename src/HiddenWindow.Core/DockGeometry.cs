using System;
using System.Collections.Generic;
using System.Linq;

namespace HiddenWindow.Core;

public enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// 停靠几何计算：吸附、隐藏位置、边缘判定。全部为纯函数，不依赖 WinForms。
/// </summary>
public static class DockGeometry
{
    /// <summary>窗口吸附到目标边缘；沿边缘方向夹在显示器范围内。</summary>
    public static RECT SnapToEdge(RECT rect, RECT monitor, DockEdge edge)
    {
        var width = rect.Width;
        var height = rect.Height;
        var x = rect.Left;
        var y = rect.Top;

        if (edge == DockEdge.Left)
        {
            x = monitor.Left;
            y = Clamp(rect.Top, monitor.Top, monitor.Bottom - height);
        }
        else if (edge == DockEdge.Right)
        {
            x = monitor.Right - width;
            y = Clamp(rect.Top, monitor.Top, monitor.Bottom - height);
        }
        else if (edge == DockEdge.Top)
        {
            y = monitor.Top;
            x = Clamp(rect.Left, monitor.Left, monitor.Right - width);
        }
        else if (edge == DockEdge.Bottom)
        {
            y = monitor.Bottom - height;
            x = Clamp(rect.Left, monitor.Left, monitor.Right - width);
        }

        return new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
    }

    /// <summary>隐藏位置：移出屏幕外，仅保留 visiblePx 宽/高留在屏幕内。</summary>
    public static RECT HiddenRect(RECT shown, RECT monitor, DockEdge edge, int visiblePx)
    {
        if (edge == DockEdge.Left)
        {
            var x = monitor.Left - (shown.Width - visiblePx);
            return new RECT { Left = x, Top = shown.Top, Right = x + shown.Width, Bottom = shown.Bottom };
        }

        if (edge == DockEdge.Right)
        {
            var x = monitor.Right - visiblePx;
            return new RECT { Left = x, Top = shown.Top, Right = x + shown.Width, Bottom = shown.Bottom };
        }

        if (edge == DockEdge.Top)
        {
            var yTop = monitor.Top - (shown.Height - visiblePx);
            return new RECT { Left = shown.Left, Top = yTop, Right = shown.Right, Bottom = yTop + shown.Height };
        }

        // Bottom: 窗口向下隐藏，保留 visiblePx 露在屏幕底部
        var yBottom = monitor.Bottom - visiblePx;
        return new RECT { Left = shown.Left, Top = yBottom, Right = shown.Right, Bottom = yBottom + shown.Height };
    }

    /// <summary>判断窗口是否靠近某条边缘，返回距离最近的一条。</summary>
    public static bool TryGetDockEdge(RECT rect, RECT monitor, int sensitivity, out DockEdge edge)
    {
        var candidates = new List<(DockEdge Edge, int Distance)>();

        var leftDist = Math.Abs(rect.Left - monitor.Left);
        if (leftDist <= sensitivity)
        {
            candidates.Add((DockEdge.Left, leftDist));
        }

        var rightDist = Math.Abs(monitor.Right - rect.Right);
        if (rightDist <= sensitivity)
        {
            candidates.Add((DockEdge.Right, rightDist));
        }

        var topDist = Math.Abs(rect.Top - monitor.Top);
        if (topDist <= sensitivity)
        {
            candidates.Add((DockEdge.Top, topDist));
        }

        var bottomDist = Math.Abs(monitor.Bottom - rect.Bottom);
        if (bottomDist <= sensitivity)
        {
            candidates.Add((DockEdge.Bottom, bottomDist));
        }

        if (candidates.Count == 0)
        {
            edge = DockEdge.Left;
            return false;
        }

        edge = candidates.OrderBy(c => c.Distance).First().Edge;
        return true;
    }

    /// <summary>光标是否位于边缘触发区；同侧多窗口时只匹配沿边缘方向范围之内的窗口。</summary>
    public static bool IsCursorInEdgeZone(POINT pt, RECT monitor, DockEdge edge, int sensitivity, RECT windowRect)
    {
        var insideMonitor = pt.X >= monitor.Left && pt.X <= monitor.Right
            && pt.Y >= monitor.Top && pt.Y <= monitor.Bottom;
        if (!insideMonitor)
        {
            return false;
        }

        if (edge == DockEdge.Left)
        {
            return pt.X >= monitor.Left && pt.X <= monitor.Left + sensitivity
                && pt.Y >= windowRect.Top && pt.Y <= windowRect.Bottom;
        }

        if (edge == DockEdge.Right)
        {
            return pt.X <= monitor.Right && pt.X >= monitor.Right - sensitivity
                && pt.Y >= windowRect.Top && pt.Y <= windowRect.Bottom;
        }

        if (edge == DockEdge.Top)
        {
            return pt.Y >= monitor.Top && pt.Y <= monitor.Top + sensitivity
                && pt.X >= windowRect.Left && pt.X <= windowRect.Right;
        }

        return pt.Y <= monitor.Bottom && pt.Y >= monitor.Bottom - sensitivity
            && pt.X >= windowRect.Left && pt.X <= windowRect.Right;
    }

    public static bool IsFullscreen(RECT rect, RECT monitor)
    {
        const int tolerance = 2;
        return Math.Abs(rect.Left - monitor.Left) <= tolerance
            && Math.Abs(rect.Top - monitor.Top) <= tolerance
            && Math.Abs(rect.Right - monitor.Right) <= tolerance
            && Math.Abs(rect.Bottom - monitor.Bottom) <= tolerance;
    }

    public static bool PointInRect(POINT pt, RECT rect)
    {
        return pt.X >= rect.Left && pt.X <= rect.Right && pt.Y >= rect.Top && pt.Y <= rect.Bottom;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
