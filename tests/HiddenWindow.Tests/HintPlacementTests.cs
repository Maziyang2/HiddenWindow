using HiddenWindow.Core;

namespace HiddenWindow.Tests;

public class HintPlacementTests
{
    private static readonly RECT WorkArea = new() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };

    [Fact]
    public void Compute_DefaultsToUpperRightOfCursor()
    {
        var location = HintPlacement.Compute(new POINT { X = 500, Y = 500 }, width: 200, height: 40, WorkArea);

        Assert.Equal(512, location.X);
        Assert.Equal(452, location.Y);
    }

    [Fact]
    public void Compute_TopEdge_PlacesHintBelowCursor()
    {
        var location = HintPlacement.Compute(new POINT { X = 500, Y = 2 }, width: 200, height: 40, WorkArea);

        Assert.Equal(512, location.X);
        Assert.Equal(26, location.Y);
    }

    [Fact]
    public void Compute_RightEdge_PlacesHintLeftOfCursor()
    {
        var location = HintPlacement.Compute(new POINT { X = 1900, Y = 500 }, width: 200, height: 40, WorkArea);

        Assert.Equal(1688, location.X);
        Assert.Equal(452, location.Y);
    }

    [Fact]
    public void Compute_ClampsIntoWorkArea()
    {
        // 窄工作区 + 光标贴近右缘：提示窗不能越出可用区域
        var smallArea = new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 };

        var location = HintPlacement.Compute(new POINT { X = 790, Y = 300 }, width: 780, height: 40, smallArea);

        Assert.Equal(0, location.X);
        Assert.Equal(252, location.Y);
    }
}
