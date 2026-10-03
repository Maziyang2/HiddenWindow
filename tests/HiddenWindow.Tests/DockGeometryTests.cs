using HiddenWindow.Core;

namespace HiddenWindow.Tests;

public class DockGeometryTests
{
    // 1920 x 1080 主显示器
    private static readonly RECT Monitor = new() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };

    private static RECT Rect(int left, int top, int right, int bottom) =>
        new() { Left = left, Top = top, Right = right, Bottom = bottom };

    [Fact]
    public void SnapToEdge_Left_AlignsToMonitorAndKeepsVerticalPosition()
    {
        var snapped = DockGeometry.SnapToEdge(Rect(12, 200, 1012, 800), Monitor, DockEdge.Left);

        Assert.Equal(0, snapped.Left);
        Assert.Equal(200, snapped.Top);
        Assert.Equal(1000, snapped.Width);
        Assert.Equal(600, snapped.Height);
    }

    [Fact]
    public void SnapToEdge_Right_AlignsRightEdgeAndKeepsSize()
    {
        var snapped = DockGeometry.SnapToEdge(Rect(900, 100, 1900, 700), Monitor, DockEdge.Right);

        Assert.Equal(920, snapped.Left);
        Assert.Equal(1920, snapped.Right);
        Assert.Equal(100, snapped.Top);
    }

    [Fact]
    public void SnapToEdge_Bottom_ClampsHorizontalPositionIntoMonitor()
    {
        // 窗口右侧超出显示器，吸附后应被夹回屏幕范围内
        var snapped = DockGeometry.SnapToEdge(Rect(1500, 100, 2500, 700), Monitor, DockEdge.Bottom);

        Assert.Equal(920, snapped.Left);
        Assert.Equal(1080, snapped.Bottom);
    }

    [Theory]
    [InlineData(DockEdge.Left, -995, 5)]
    [InlineData(DockEdge.Right, 1915, 2915)]
    public void HiddenRect_HorizontalEdges_LeaveVisibleStripOnScreen(DockEdge edge, int expectedLeft, int expectedRight)
    {
        var shown = Rect(0, 200, 1000, 800);

        var hidden = DockGeometry.HiddenRect(shown, Monitor, edge, visiblePx: 5);

        Assert.Equal(expectedLeft, hidden.Left);
        Assert.Equal(expectedRight, hidden.Right);
        Assert.Equal(200, hidden.Top);
        Assert.Equal(600, hidden.Height);
    }

    [Theory]
    [InlineData(DockEdge.Top, -595, 5)]
    [InlineData(DockEdge.Bottom, 1075, 1675)]
    public void HiddenRect_VerticalEdges_LeaveVisibleStripOnScreen(DockEdge edge, int expectedTop, int expectedBottom)
    {
        var shown = Rect(100, 0, 1100, 600);

        var hidden = DockGeometry.HiddenRect(shown, Monitor, edge, visiblePx: 5);

        Assert.Equal(expectedTop, hidden.Top);
        Assert.Equal(expectedBottom, hidden.Bottom);
        Assert.Equal(100, hidden.Left);
        Assert.Equal(1000, hidden.Width);
    }

    [Fact]
    public void TryGetDockEdge_PicksNearestEdge()
    {
        // 距离左边 30px、上边 5px：应选择上边
        var found = DockGeometry.TryGetDockEdge(Rect(30, 5, 1030, 605), Monitor, sensitivity: 50, out var edge);

        Assert.True(found);
        Assert.Equal(DockEdge.Top, edge);
    }

    [Fact]
    public void TryGetDockEdge_AcceptsWindowSlightlyOutsideMonitor()
    {
        var found = DockGeometry.TryGetDockEdge(Rect(-20, 300, 980, 900), Monitor, sensitivity: 50, out var edge);

        Assert.True(found);
        Assert.Equal(DockEdge.Left, edge);
    }

    [Fact]
    public void TryGetDockEdge_ReturnsFalseWhenFarFromEveryEdge()
    {
        var found = DockGeometry.TryGetDockEdge(Rect(500, 300, 1500, 900), Monitor, sensitivity: 50, out _);

        Assert.False(found);
    }

    [Fact]
    public void IsCursorInEdgeZone_LeftEdge_RequiresCursorWithinWindowSpan()
    {
        var windowRect = Rect(0, 200, 1000, 800);

        Assert.True(DockGeometry.IsCursorInEdgeZone(new POINT { X = 2, Y = 400 }, Monitor, DockEdge.Left, 50, windowRect));
        Assert.False(DockGeometry.IsCursorInEdgeZone(new POINT { X = 2, Y = 100 }, Monitor, DockEdge.Left, 50, windowRect));
        Assert.False(DockGeometry.IsCursorInEdgeZone(new POINT { X = 300, Y = 400 }, Monitor, DockEdge.Left, 50, windowRect));
    }

    [Fact]
    public void IsCursorInEdgeZone_TopEdge_RequiresCursorWithinWindowSpan()
    {
        var windowRect = Rect(200, 0, 800, 600);

        Assert.True(DockGeometry.IsCursorInEdgeZone(new POINT { X = 400, Y = 3 }, Monitor, DockEdge.Top, 50, windowRect));
        Assert.False(DockGeometry.IsCursorInEdgeZone(new POINT { X = 100, Y = 3 }, Monitor, DockEdge.Top, 50, windowRect));
    }

    [Fact]
    public void IsCursorInEdgeZone_ReturnsFalseOutsideMonitor()
    {
        var windowRect = Rect(0, 200, 1000, 800);

        Assert.False(DockGeometry.IsCursorInEdgeZone(new POINT { X = -10, Y = 400 }, Monitor, DockEdge.Left, 50, windowRect));
    }

    [Fact]
    public void IsFullscreen_ToleratesSmallOffsets()
    {
        Assert.True(DockGeometry.IsFullscreen(Rect(0, 0, 1920, 1080), Monitor));
        Assert.True(DockGeometry.IsFullscreen(Rect(-1, 1, 1921, 1079), Monitor));
        Assert.False(DockGeometry.IsFullscreen(Rect(0, 0, 1920, 1000), Monitor));
    }

    [Fact]
    public void PointInRect_IncludesBoundaries()
    {
        var rect = Rect(10, 20, 100, 200);

        Assert.True(DockGeometry.PointInRect(new POINT { X = 10, Y = 20 }, rect));
        Assert.True(DockGeometry.PointInRect(new POINT { X = 100, Y = 200 }, rect));
        Assert.False(DockGeometry.PointInRect(new POINT { X = 9, Y = 50 }, rect));
    }    
    [Fact]
    public void RebaseToMonitor_UpdatesBothTargetsWhenDisplayBoundsChange()
    {
        var oldMonitor = Rect(0, 0, 1920, 1080);
        var newMonitor = Rect(0, 0, 2560, 1080);
        var oldShown = Rect(920, 100, 1920, 700);

        var targets = DockGeometry.RebaseToMonitor(oldShown, newMonitor, DockEdge.Right, visiblePx: 5);

        Assert.Equal(1560, targets.ShownRect.Left);
        Assert.Equal(2560, targets.ShownRect.Right);
        Assert.Equal(2555, targets.HiddenRect.Left);
        Assert.Equal(3555, targets.HiddenRect.Right);
        Assert.NotEqual(oldMonitor.Right, targets.ShownRect.Right);
    }
}
