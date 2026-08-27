using System.Drawing;
using AutoClicker.Core;
using Xunit;

namespace AutoClicker.Tests.Core;

public sealed class ClickRunOptionsTests
{
    [Fact]
    public void RejectsIntervalBelowSafetyFloor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(interval: 24));
    }

    [Fact]
    public void RejectsVarianceThatCanCrossSafetyFloor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(interval: 30, variance: 6));
    }

    [Fact]
    public void RejectsIntervalAboveOneDay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(interval: 86_400_001));
    }

    [Fact]
    public void RejectsNonPositiveFiniteRepeatCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(repeatCount: 0));
    }

    [Fact]
    public void VarianceAlwaysStaysInsideConfiguredRange()
    {
        var options = Create(interval: 100, variance: 25);
        var random = new Random(1234);

        for (var i = 0; i < 500; i++)
            Assert.InRange(options.GetDelayMilliseconds(random), 75, 125);
    }

    [Fact]
    public void CapturesImmutableConfigurationValues()
    {
        var point = new Point(12, 34);
        var options = ClickRunOptions.Create(100, 10, ClickButton.Right, ClickAction.Double, 3, point);

        Assert.Equal(100, options.IntervalMilliseconds);
        Assert.Equal(10, options.VarianceMilliseconds);
        Assert.Equal(ClickButton.Right, options.Button);
        Assert.Equal(ClickAction.Double, options.Action);
        Assert.Equal(3, options.RepeatCount);
        Assert.Equal(point, options.FixedPosition);
    }

    private static ClickRunOptions Create(int interval = 100, int variance = 0, int? repeatCount = null) =>
        ClickRunOptions.Create(interval, variance, ClickButton.Left, ClickAction.Single, repeatCount, null);
}
