using HiddenWindow.Core;

namespace HiddenWindow.Tests;

public class DockWindowStateTests
{
    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, false, false)]
    public void IsHiddenAtAnimationTarget_UsesInFlightTarget(
        bool isHidden, bool isAnimating, bool animationTargetsHidden, bool expected)
    {
        Assert.Equal(expected, DockWindowState.IsHiddenAtAnimationTarget(
            isHidden, isAnimating, animationTargetsHidden));
    }

    [Fact]
    public void ShouldRestoreOnForget_RestoresWhenHideFlagHasNotYetBeenSet()
    {
        Assert.True(DockWindowState.ShouldRestoreOnForget(
            restoreIfHidden: true, isHidden: false, isAnimating: true));
    }

    [Fact]
    public void ShouldRestoreOnForget_DoesNotRestoreWhenRestorationWasNotRequested()
    {
        Assert.False(DockWindowState.ShouldRestoreOnForget(
            restoreIfHidden: false, isHidden: true, isAnimating: true));
    }
}
