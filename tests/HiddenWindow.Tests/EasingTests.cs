using HiddenWindow.Core;

namespace HiddenWindow.Tests;

public class EasingTests
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    public void EaseInOutQuad_HitsEndpointsAndMidpoint(double t, double expected)
    {
        Assert.Equal(expected, Easing.EaseInOutQuad(t), precision: 10);
    }

    [Fact]
    public void EaseInOutQuad_IsMonotonic()
    {
        var previous = -1.0;
        for (var step = 0; step <= 20; step++)
        {
            var value = Easing.EaseInOutQuad(step / 20.0);
            Assert.True(value >= previous, $"t={step / 20.0} 时曲线回退");
            previous = value;
        }
    }

    [Fact]
    public void EaseInOutQuad_IsSymmetric()
    {
        for (var step = 0; step <= 10; step++)
        {
            var t = step / 10.0;
            Assert.Equal(1.0, Easing.EaseInOutQuad(t) + Easing.EaseInOutQuad(1 - t), precision: 10);
        }
    }

    [Theory]
    [InlineData(0, 100, 0.0, 0)]
    [InlineData(0, 100, 0.5, 50)]
    [InlineData(0, 100, 1.0, 100)]
    [InlineData(200, 100, 0.5, 150)]
    public void Lerp_InterpolatesBetweenPoints(int from, int to, double t, int expected)
    {
        Assert.Equal(expected, Easing.Lerp(from, to, t));
    }
}
