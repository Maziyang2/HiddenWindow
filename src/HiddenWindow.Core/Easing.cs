using System;

namespace HiddenWindow.Core;

/// <summary>动画曲线与插值。</summary>
public static class Easing
{
    /// <summary>ease-in-out 缓动曲线: t =&gt; t&lt;0.5 ? 2t² : 1-(-2t+2)²/2</summary>
    public static double EaseInOutQuad(double t)
    {
        return t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2) / 2.0;
    }

    public static int Lerp(int from, int to, double t)
    {
        return (int)Math.Round(from + (to - from) * t);
    }
}
