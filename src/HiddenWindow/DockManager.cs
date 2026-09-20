using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HiddenWindow.Core;
using FormsTimer = System.Windows.Forms.Timer;

namespace HiddenWindow;

internal sealed class DockedWindow
{
    public IntPtr Hwnd { get; }
    public DockEdge Edge { get; set; }
    public RECT ShownRect { get; set; }
    public RECT HiddenRect { get; set; }
    public RECT LastKnownRect { get; set; }
    public MONITORINFO Monitor { get; set; }
    public bool IsHidden { get; set; }
    public DateTime LastShownUtc { get; set; }
    public bool IsAnimating { get; set; }
    public bool WasCursorInTriggerZone { get; set; }

    // 进行中的动画状态：允许被取消（设置变更 / 用户开始拖动 / 退出）
    public FormsTimer? AnimationTimer { get; set; }
    public RECT AnimationTarget { get; set; }
    public Action? AnimationCompleted { get; set; }

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
        foreach (var docked in _docked.Values.ToList())
        {
            // 设置变更立即生效：先结束进行中的动画（落到原目标位置），再按新设置重算
            CancelAnimation(docked, snapToTarget: true);

            docked.ShownRect = DockGeometry.SnapToEdge(docked.ShownRect, docked.Monitor.rcMonitor, docked.Edge);
            docked.HiddenRect = DockGeometry.HiddenRect(
                docked.ShownRect, docked.Monitor.rcMonitor, docked.Edge, _settings.VisibleEdgePx);

            MoveWindow(docked.Hwnd, docked.IsHidden ? docked.HiddenRect : docked.ShownRect);
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
            if (_docked.TryGetValue(hwnd, out var dragging))
            {
                // 用户开始拖动：立即结束动画，避免动画和拖动互相拉扯
                CancelAnimation(dragging, snapToTarget: false);
                dragging.IsHidden = false;
                MoveWindow(hwnd, dragging.ShownRect);
            }
            return;
        }

        if (eventType != WinApi.EVENT_SYSTEM_MOVESIZEEND)
        {
            return;
        }

        if (!IsEligibleWindow(hwnd))
        {
            if (_docked.TryGetValue(hwnd, out var ineligible))
            {
                Forget(ineligible, restoreIfHidden: true);
            }
            return;
        }

        if (!WinApi.GetWindowRect(hwnd, out var rect))
        {
            return;
        }

        var mi = WinApi.GetMonitorInfoSafe(WinApi.MonitorFromWindow(hwnd, WinApi.MONITOR_DEFAULTTONEAREST));

        if (DockGeometry.TryGetDockEdge(rect, mi.rcMonitor, _settings.EdgeSensitivityPx, out var edge))
        {
            DockWindow(hwnd, rect, mi, edge);
        }
        else if (_docked.TryGetValue(hwnd, out var undocked))
        {
            Forget(undocked, restoreIfHidden: false);
        }
    }

    private void DockWindow(IntPtr hwnd, RECT rect, MONITORINFO mi, DockEdge edge)
    {
        if (!_docked.TryGetValue(hwnd, out var docked))
        {
            docked = new DockedWindow(hwnd);
            _docked[hwnd] = docked;
        }

        CancelAnimation(docked, snapToTarget: false);

        docked.Edge = edge;
        docked.Monitor = mi;
        docked.ShownRect = DockGeometry.SnapToEdge(rect, mi.rcMonitor, edge);
        docked.HiddenRect = DockGeometry.HiddenRect(docked.ShownRect, mi.rcMonitor, edge, _settings.VisibleEdgePx);
        docked.IsHidden = true;
        docked.LastShownUtc = DateTime.UtcNow;
        docked.WasCursorInTriggerZone = false;

        if (!TryMoveWindow(hwnd, docked.HiddenRect))
        {
            // 移动失效（例如受 UIPI 保护的提权窗口）时不记录停靠，避免状态与实际不符
            _docked.Remove(hwnd);
            return;
        }

        docked.LastKnownRect = docked.HiddenRect;
    }

    /// <summary>停止跟踪某个窗口；若它正停在隐藏位置，先移回可见位置，避免窗口"找不回来"。</summary>
    private void Forget(DockedWindow docked, bool restoreIfHidden)
    {
        _docked.Remove(docked.Hwnd);
        CancelAnimation(docked, snapToTarget: false);

        if (restoreIfHidden && docked.IsHidden)
        {
            MoveWindow(docked.Hwnd, docked.ShownRect);
        }
    }

    private void PollMouseAndWindows()
    {
        if (IsPaused)
        {
            return;
        }

        if (_docked.Count == 0)
        {
            _hintForm?.HideHint();
            return;
        }

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
        POINT hintPoint = default;
        string? hintTitle = null;

        foreach (var docked in _docked.Values.ToList())
        {
            if (!WinApi.GetWindowRect(docked.Hwnd, out var currentRect))
            {
                // 窗口已经关闭
                _docked.Remove(docked.Hwnd);
                continue;
            }

            // 仅在窗口矩形变化时重新解析所在显示器
            if (RectChanged(currentRect, docked.LastKnownRect))
            {
                docked.LastKnownRect = currentRect;
                docked.Monitor = WinApi.GetMonitorInfoSafe(
                    WinApi.MonitorFromWindow(docked.Hwnd, WinApi.MONITOR_DEFAULTTONEAREST));
            }

            // 停靠之后窗口被最小化：取消跟踪并把窗口移回可见位置
            if (WinApi.IsIconic(docked.Hwnd))
            {
                Forget(docked, restoreIfHidden: true);
                continue;
            }

            // 停靠之后窗口被最大化或进入全屏：不再属于停靠窗口（窗口本身仍在屏幕上）
            if (WinApi.IsZoomed(docked.Hwnd) || DockGeometry.IsFullscreen(currentRect, docked.Monitor.rcMonitor))
            {
                Forget(docked, restoreIfHidden: false);
                continue;
            }

            var isInTriggerZone = DockGeometry.IsCursorInEdgeZone(
                pt,
                docked.Monitor.rcMonitor,
                docked.Edge,
                _settings.EdgeSensitivityPx,
                docked.ShownRect);
            var enteredTriggerZone = isInTriggerZone && !docked.WasCursorInTriggerZone;
            docked.WasCursorInTriggerZone = isInTriggerZone;

            // 收集隐藏窗口的提示信息
            if (docked.IsHidden && isInTriggerZone && !docked.IsAnimating)
            {
                anyTriggerZone = true;
                hintPoint = pt;
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
                if (DockGeometry.PointInRect(pt, currentRect))
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
        if (_hintForm == null)
        {
            return;
        }

        if (anyTriggerZone && hintTitle != null)
        {
            _hintForm.ShowHint(hintPoint, hintTitle);
        }
        else
        {
            _hintForm.HideHint();
        }
    }

    private void ShowDockedWindow(DockedWindow docked, DateTime now)
    {
        docked.LastShownUtc = now;
        docked.IsHidden = false;
        // v1.4: 动画完成后才置顶，避免窗口在隐藏位置被激活导致跳跃
        AnimateWindow(docked, docked.HiddenRect, docked.ShownRect, () =>
        {
            BringWindowToFront(docked.Hwnd);
        });
    }

    private void HideDockedWindow(DockedWindow docked)
    {
        // v1.4: 动画完成后才标记为隐藏，防止轮询中途再次触发显示
        AnimateWindow(docked, docked.ShownRect, docked.HiddenRect, () =>
        {
            docked.IsHidden = true;
        });
    }

    // v1.4: 增加 onCompleted 回调，动画完成后执行（如置顶 / 标记隐藏）
    private void AnimateWindow(DockedWindow docked, RECT from, RECT to, Action? onCompleted = null)
    {
        CancelAnimation(docked, snapToTarget: false);

        docked.IsAnimating = true;
        docked.AnimationTarget = to;
        docked.AnimationCompleted = onCompleted;

        var duration = Math.Max(1, _settings.EffectiveAnimationDurationMs);
        var sw = Stopwatch.StartNew();
        var timer = new FormsTimer { Interval = 15 };
        docked.AnimationTimer = timer;

        timer.Tick += (_, _) =>
        {
            var linearT = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / duration);
            var t = Easing.EaseInOutQuad(linearT);  // 缓动曲线
            var x = Easing.Lerp(from.Left, to.Left, t);
            var y = Easing.Lerp(from.Top, to.Top, t);

            WinApi.SetWindowPos(docked.Hwnd, IntPtr.Zero, x, y, 0, 0,
                WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE | WinApi.SWP_NOSIZE);

            if (linearT >= 1.0)
            {
                FinishAnimation(docked);
            }
        };

        timer.Start();
    }

    private static void FinishAnimation(DockedWindow docked)
    {
        var timer = docked.AnimationTimer;
        docked.AnimationTimer = null;
        if (timer != null)
        {
            timer.Stop();
            timer.Dispose();
        }

        docked.IsAnimating = false;

        var completed = docked.AnimationCompleted;
        docked.AnimationCompleted = null;
        completed?.Invoke();
    }

    /// <summary>
    /// 结束进行中的动画。snapToTarget 为 true 时先把窗口落到原动画目标位置，
    /// 保证窗口位置与停靠状态一致。
    /// </summary>
    private static void CancelAnimation(DockedWindow docked, bool snapToTarget)
    {
        var timer = docked.AnimationTimer;
        if (timer == null)
        {
            docked.IsAnimating = false;
            return;
        }

        docked.AnimationTimer = null;
        timer.Stop();
        timer.Dispose();
        docked.IsAnimating = false;

        var completed = docked.AnimationCompleted;
        docked.AnimationCompleted = null;

        if (!snapToTarget)
        {
            return;
        }

        MoveWindow(docked.Hwnd, docked.AnimationTarget);
        completed?.Invoke();
    }

    /// <summary>移动窗口但不做校验（动画 / 设置变更 / 复位使用）。</summary>
    private static void MoveWindow(IntPtr hwnd, RECT rect)
    {
        WinApi.SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
            WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE);
    }

    /// <summary>
    /// 移动窗口并回读校验：受 UIPI 保护的提权窗口或受系统限制时 SetWindowPos 会静默失败。
    /// </summary>
    private static bool TryMoveWindow(IntPtr hwnd, RECT rect)
    {
        if (!WinApi.SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
                WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE))
        {
            return false;
        }

        if (!WinApi.GetWindowRect(hwnd, out var actual))
        {
            return false;
        }

        // 允许少量误差：部分窗口会自行微调位置
        const int tolerance = 8;
        return Math.Abs(actual.Left - rect.Left) <= tolerance
            && Math.Abs(actual.Top - rect.Top) <= tolerance;
    }

    private static bool RectChanged(RECT a, RECT b)
    {
        return a.Left != b.Left || a.Top != b.Top || a.Right != b.Right || a.Bottom != b.Bottom;
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
        SetForegroundWindowWithFallback(hwnd);
    }

    /// <summary>
    /// SetForegroundWindow 在前台锁定限制下会静默失败（返回值也被忽略）。
    /// 把本线程与前台线程的输入队列临时附加到一起可以绕过该限制。
    /// </summary>
    private static void SetForegroundWindowWithFallback(IntPtr hwnd)
    {
        var foreground = WinApi.GetForegroundWindow();
        if (foreground == hwnd)
        {
            return;
        }

        var foregroundThread = WinApi.GetWindowThreadProcessId(foreground, out _);
        var currentThread = WinApi.GetCurrentThreadId();
        if (foregroundThread == 0 || foregroundThread == currentThread)
        {
            WinApi.SetForegroundWindow(hwnd);
            return;
        }

        if (!WinApi.AttachThreadInput(foregroundThread, currentThread, true))
        {
            WinApi.SetForegroundWindow(hwnd);
            return;
        }

        try
        {
            WinApi.SetForegroundWindow(hwnd);
        }
        finally
        {
            WinApi.AttachThreadInput(foregroundThread, currentThread, false);
        }
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

        var mi = WinApi.GetMonitorInfoSafe(WinApi.MonitorFromWindow(hwnd, WinApi.MONITOR_DEFAULTTONEAREST));
        if (DockGeometry.IsFullscreen(rect, mi.rcMonitor))
        {
            return false;
        }

        return true;
    }

    // 获取窗口标题
    private static string? GetWindowTitle(IntPtr hwnd)
    {
        var len = WinApi.GetWindowTextLength(hwnd);
        if (len <= 0) return null;
        var sb = new StringBuilder(len + 1);
        WinApi.GetWindowText(hwnd, sb, sb.Capacity);
        var title = sb.ToString();
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();

        foreach (var docked in _docked.Values.ToList())
        {
            CancelAnimation(docked, snapToTarget: false);
        }

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
