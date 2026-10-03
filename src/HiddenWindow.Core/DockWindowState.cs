namespace HiddenWindow.Core;

/// <summary>停靠窗口被取消跟踪或重排时的状态决策。</summary>
public static class DockWindowState
{
    public static bool IsHiddenAtAnimationTarget(
        bool isHidden, bool isAnimating, bool animationTargetsHidden) =>
        isAnimating ? animationTargetsHidden : isHidden;

    public static bool ShouldRestoreOnForget(
        bool restoreIfHidden, bool isHidden, bool isAnimating) =>
        restoreIfHidden && (isHidden || isAnimating);
}
