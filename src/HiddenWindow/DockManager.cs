using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FormsTimer = System.Windows.Forms.Timer;

namespace HiddenWindow;

internal enum DockEdge
{
    Left,
    Right,
    Top,
    Bottom
}

internal sealed class DockedWindow
{
    public IntPtr Hwnd { get; }
    public DockEdge Edge { get; set; }
    public WinApi.RECT ShownRect { get; set; }
    public WinApi.RECT HiddenRect { get; set; }
    public WinApi.MONITORINFO Monitor { get; set; }
    public bool IsHidden { get; set; }
    public DateTime LastShownUtc { get; set; }
    public bool IsAnimating { get; set; }
    public bool WasCursorInTriggerZone { get; set; }

    public DockedWindow(IntPtr hwnd)
    {
        Hwnd = hwnd;
    }
}

internal sealed class DockManager : IDisposable
{
    private readonly Dictionary<IntPtr, DockedWindow> _docked = new();
    private readonly FormsTimer _pollTimer;
    private readonly uint _currentProcessId;
    private AppSettings _settings;
    private IntPtr _hookStart = IntPtr.Zero;
    private IntPtr _hookEnd = IntPtr.Zero;
    private WinApi.WinEventDelegate? _eventDelegate;
    private EdgeHintForm? _hintForm;

    private const int PollIntervalMs = 50;
    public bool IsPaused { get; set; }

    public DockManager(AppSettings settings, EdgeHintForm? hintForm = null)
    {
        _settings = settings;
        _hintForm = hintForm;
        _currentProcessId = (uint)Process.GetCurrentProcess().Id;

        _pollTimer = new FormsTimer { Interval = PollIntervalMs };
        _pollTimer.Tick += (_, _) => PollMouseAndWindows();

        StartHooks();
        _pollTimer.Start();
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        foreach (var docked in _docked.Values)
        {
            RecalculateRects(docked);
            if (docked.IsHidden)
            {
                MoveWindow(docked.Hwnd, docked.HiddenRect);
            }
            else
            {
                MoveWindow(docked.Hwnd, docked.ShownRect);
            }
        }
    }

    private void StartHooks()
    {
        _eventDelegate = OnWinEvent;
        _hookStart = WinApi.SetWinEventHook(
            WinApi.EVENT_SYSTEM_MOVESIZESTART,
            WinApi.EVENT_SYSTEM_MOVESIZESTART,
            IntPtr.Zero,
            _eventDelegate,
            0,
            0,
            WinApi.WINEVENT_OUTOFCONTEXT);

        _hookEnd = WinApi.SetWinEventHook(
            WinApi.EVENT_SYSTEM_MOVESIZEEND,
            WinApi.EVENT_SYSTEM_MOVESIZEEND,
            IntPtr.Zero,
            _eventDelegate,
            0,
            0,
            WinApi.WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero || idObject != 0)
        {
            return;
        }

        if (eventType == WinApi.EVENT_SYSTEM_MOVESIZESTART)
        {
            if (_docked.TryGetValue(hwnd, out var docked))
            {
                docked.IsHidden = false;
                MoveWindow(hwnd, docked.ShownRect);
            }
            return;
        }

        if (eventType != WinApi.EVENT_SYSTEM_MOVESIZEEND)
        {
            return;
        }

        if (!IsEligibleWindow(hwnd))
        {
            if (_docked.ContainsKey(hwnd))
            {
                _docked.Remove(hwnd);
            }
            return;
        }

        if (!WinApi.GetWindowRect(hwnd, out var rect))
        {
            return;
        }

        var monitor = WinApi.MonitorFromWindow(hwnd, 2);
        var mi = WinApi.GetMonitorInfoSafe(monitor);

        if (TryGetDockEdge(rect, mi.rcMonitor, _settings.EdgeSensitivityPx, out var edge))
        {
            DockWindow(hwnd, rect, mi, edge);
        }
        else
        {
            _docked.Remove(hwnd);
        }
    }

    private void DockWindow(IntPtr hwnd, WinApi.RECT rect, WinApi.MONITORINFO mi, DockEdge edge)
    {
        if (!_docked.TryGetValue(hwnd, out var docked))
        {
            docked = new DockedWindow(hwnd);
            _docked[hwnd] = docked;
        }

        docked.Edge = edge;
        docked.Monitor = mi;
        docked.ShownRect = GetSnappedRect(rect, mi.rcMonitor, edge);
        docked.HiddenRect = GetHiddenRect(docked.ShownRect, mi.rcMonitor, edge, _settings.VisibleEdgePx);
        docked.IsHidden = true;
        docked.LastShownUtc = DateTime.UtcNow;
        docked.WasCursorInTriggerZone = false;

        MoveWindow(hwnd, docked.HiddenRect);
    }

    private void RecalculateRects(DockedWindow docked)
    {
        var edge = docked.Edge;
        var mi = docked.Monitor;
        var shown = GetSnappedRect(docked.ShownRect, mi.rcMonitor, edge);
        docked.ShownRect = shown;
        docked.HiddenRect = GetHiddenRect(shown, mi.rcMonitor, edge, _settings.VisibleEdgePx);
    }

    private static WinApi.RECT GetSnappedRect(WinApi.RECT rect, WinApi.RECT monitor, DockEdge edge)
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

        return new WinApi.RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
    }

    private static WinApi.RECT GetHiddenRect(WinApi.RECT shown, WinApi.RECT monitor, DockEdge edge, int visiblePx)
    {
        if (edge == DockEdge.Left)
        {
            var x = monitor.Left - (shown.Width - visiblePx);
            return new WinApi.RECT { Left = x, Top = shown.Top, Right = x + shown.Width, Bottom = shown.Bottom };
        }

        if (edge == DockEdge.Right)
        {
            var x = monitor.Right - visiblePx;
            return new WinApi.RECT { Left = x, Top = shown.Top, Right = x + shown.Width, Bottom = shown.Bottom };
        }

        if (edge == DockEdge.Top)
        {
            var yTop = monitor.Top - (shown.Height - visiblePx);
            return new WinApi.RECT { Left = shown.Left, Top = yTop, Right = shown.Right, Bottom = yTop + shown.Height };
        }

        // Bottom: 窗口向下隐藏，保留 visiblePx 露在屏幕底部
        var yBottom = monitor.Bottom - visiblePx;
        return new WinApi.RECT { Left = shown.Left, Top = yBottom, Right = shown.Right, Bottom = yBottom + shown.Height };
    }

    private void PollMouseAndWindows()
    {
        if (IsPaused)
            return;

        if ((WinApi.GetAsyncKeyState(WinApi.VK_LBUTTON) & 0x8000) != 0)
        {
            return;
        }

        if (!WinApi.GetCursorPos(out var pt))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var anyTriggerZone = false;
        Point hintPoint = default;
        string? hintTitle = null;

        foreach (var docked in _docked.Values.ToList())
        {
            if (!WinApi.GetWindowRect(docked.Hwnd, out var currentRect))
            {
                _docked.Remove(docked.Hwnd);
                continue;
            }

            var dockedMonitor = WinApi.MonitorFromWindow(docked.Hwnd, 2);
            docked.Monitor = WinApi.GetMonitorInfoSafe(dockedMonitor);
            var isInTriggerZone = IsCursorInEdgeZone(
                pt,
                docked.Monitor.rcMonitor,
                docked.Edge,
                _settings.EdgeSensitivityPx);
            var enteredTriggerZone = isInTriggerZone && !docked.WasCursorInTriggerZone;
            docked.WasCursorInTriggerZone = isInTriggerZone;

            // 收集隐藏窗口的提示信息
            if (docked.IsHidden && isInTriggerZone && !docked.IsAnimating)
            {
                anyTriggerZone = true;
                hintPoint = new Point(pt.X, pt.Y);
                hintTitle = GetWindowTitle(docked.Hwnd);
            }

            if (docked.IsAnimating)
            {
                continue;
            }

            if (docked.IsHidden)
            {
                if (enteredTriggerZone)
                {
                    ShowDockedWindow(docked, now);
                }
            }
            else
            {
                if (PointInRect(pt, currentRect))
                {
                    continue;
                }

                if ((now - docked.LastShownUtc).TotalMilliseconds < _settings.HideDelayMs)
                {
                    continue;
                }

                if (!isInTriggerZone)
                {
                    HideDockedWindow(docked);
                }
            }
        }

        // 边缘提示：有隐藏窗口在触发区则显示标题，否则隐藏
        if (_hintForm != null)
        {
            if (anyTriggerZone && hintTitle != null)
                _hintForm.ShowHint(hintPoint, hintTitle);
            else
                _hintForm.HideHint();
        }
    }

    private void ShowDockedWindow(DockedWindow docked, DateTime now)
    {
        docked.LastShownUtc = now;
        AnimateWindow(docked, docked.HiddenRect, docked.ShownRect);
        BringWindowToFront(docked.Hwnd);
        docked.IsHidden = false;
    }

    private void HideDockedWindow(DockedWindow docked)
    {
        AnimateWindow(docked, docked.ShownRect, docked.HiddenRect);
        docked.IsHidden = true;
    }

    private void AnimateWindow(DockedWindow docked, WinApi.RECT from, WinApi.RECT to)
    {
        docked.IsAnimating = true;

        var duration = _settings.EffectiveAnimationDurationMs;

        var sw = Stopwatch.StartNew();
        var timer = new FormsTimer { Interval = 15 };
        timer.Tick += (_, _) =>
        {
            var linearT = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / duration);
            var t = EaseInOutQuad(linearT);  // 缓动曲线
            var x = Lerp(from.Left, to.Left, t);
            var y = Lerp(from.Top, to.Top, t);

            WinApi.SetWindowPos(docked.Hwnd, IntPtr.Zero, x, y, 0, 0,
                WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE | WinApi.SWP_NOSIZE);

            if (linearT >= 1.0)
            {
                timer.Stop();
                timer.Dispose();
                docked.IsAnimating = false;
            }
        };
        timer.Start();
    }

    private static void MoveWindow(IntPtr hwnd, WinApi.RECT rect)
    {
        WinApi.SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
            WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE);
    }

    private static void BringWindowToFront(IntPtr hwnd)
    {
        WinApi.SetWindowPos(
            hwnd,
            WinApi.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOACTIVATE | WinApi.SWP_SHOWWINDOW);
        WinApi.SetWindowPos(
            hwnd,
            WinApi.HWND_NOTOPMOST,
            0,
            0,
            0,
            0,
            WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOACTIVATE | WinApi.SWP_SHOWWINDOW);
        WinApi.BringWindowToTop(hwnd);
        WinApi.SetForegroundWindow(hwnd);
    }

    private bool IsEligibleWindow(IntPtr hwnd)
    {
        if (!WinApi.IsWindowVisible(hwnd) || WinApi.IsIconic(hwnd))
        {
            return false;
        }

        WinApi.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == _currentProcessId)
        {
            return false;
        }

        var exStyle = WinApi.GetWindowLong(hwnd, WinApi.GWL_EXSTYLE);
        if ((exStyle & WinApi.WS_EX_TOOLWINDOW) != 0)
        {
            return false;
        }

        if (WinApi.IsZoomed(hwnd))
        {
            return false;
        }

        if (!WinApi.GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var monitor = WinApi.MonitorFromWindow(hwnd, 2);
        var mi = WinApi.GetMonitorInfoSafe(monitor);
        if (IsFullscreen(rect, mi.rcMonitor))
        {
            return false;
        }

        return true;
    }

    private static bool IsFullscreen(WinApi.RECT rect, WinApi.RECT monitor)
    {
        const int tolerance = 2;
        return Math.Abs(rect.Left - monitor.Left) <= tolerance
            && Math.Abs(rect.Top - monitor.Top) <= tolerance
            && Math.Abs(rect.Right - monitor.Right) <= tolerance
            && Math.Abs(rect.Bottom - monitor.Bottom) <= tolerance;
    }

    private static bool TryGetDockEdge(WinApi.RECT rect, WinApi.RECT monitor, int sensitivity, out DockEdge edge)
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

    private static bool PointInRect(WinApi.POINT pt, WinApi.RECT rect)
    {
        return pt.X >= rect.Left && pt.X <= rect.Right && pt.Y >= rect.Top && pt.Y <= rect.Bottom;
    }

    private static bool IsCursorInEdgeZone(WinApi.POINT pt, WinApi.RECT monitor, DockEdge edge, int sensitivity)
    {
        var insideMonitor = pt.X >= monitor.Left && pt.X <= monitor.Right
            && pt.Y >= monitor.Top && pt.Y <= monitor.Bottom;
        if (!insideMonitor)
        {
            return false;
        }

        if (edge == DockEdge.Left)
        {
            return pt.X >= monitor.Left && pt.X <= monitor.Left + sensitivity;
        }

        if (edge == DockEdge.Right)
        {
            return pt.X <= monitor.Right && pt.X >= monitor.Right - sensitivity;
        }

        if (edge == DockEdge.Top)
        {
            return pt.Y >= monitor.Top && pt.Y <= monitor.Top + sensitivity;
        }

        // Bottom: 鼠标在屏幕底部边缘区域
        return pt.Y <= monitor.Bottom && pt.Y >= monitor.Bottom - sensitivity;
    }

    private static int Lerp(int from, int to, double t)
    {
        return (int)Math.Round(from + (to - from) * t);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    // ease-in-out 缓动曲线: t => t<0.5 ? 2*t² : 1-(-2t+2)²/2
    private static double EaseInOutQuad(double t)
    {
        return t < 0.5 ? 2.0 * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 2) / 2.0;
    }

    // 获取窗口标题
    private static string? GetWindowTitle(IntPtr hwnd)
    {
        var len = WinApi.GetWindowTextLength(hwnd);
        if (len <= 0) return null;
        var sb = new System.Text.StringBuilder(len + 1);
        WinApi.GetWindowText(hwnd, sb, sb.Capacity);
        var title = sb.ToString();
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();

        if (_hookStart != IntPtr.Zero)
        {
            WinApi.UnhookWinEvent(_hookStart);
            _hookStart = IntPtr.Zero;
        }

        if (_hookEnd != IntPtr.Zero)
        {
            WinApi.UnhookWinEvent(_hookEnd);
            _hookEnd = IntPtr.Zero;
        }
    }
}
