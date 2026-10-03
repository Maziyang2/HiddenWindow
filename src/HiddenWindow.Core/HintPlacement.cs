using System;

namespace HiddenWindow.Core;

/// <summary>
/// 边缘提示窗的定位：默认在光标右上方，越界时贴边；顶部放不下时改到光标下方。
/// </summary>
public static class HintPlacement
{
    private const int GapX = 12;
    private const int GapY = 8;
    private const int BelowGap = 24;

    public static POINT Compute(POINT cursor, int width, int height, RECT workArea)
    {
        var x = cursor.X + GapX;
        var y = cursor.Y - height - GapY;

        // 顶部边缘：上方没有空间，改为显示在光标下方
        if (y < workArea.Top)
        {
            y = cursor.Y + BelowGap;
        }

        // 右侧越界时改到光标左侧
        if (x + width > workArea.Right)
        {
            x = cursor.X - width - GapX;
        }

        var maxX = Math.Max(workArea.Left, workArea.Right - width);
        var maxY = Math.Max(workArea.Top, workArea.Bottom - height);

        return new POINT
        {
            X = Math.Clamp(x, workArea.Left, maxX),
            Y = Math.Clamp(y, workArea.Top, maxY)
        };
    }
}
