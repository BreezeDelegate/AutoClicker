using AutoClicker.Core;
using Xunit;

namespace AutoClicker.Tests.Core;

public sealed class ClickIntervalCalculatorTests
{
    [Fact]
    public void ConvertsIntervalPartsToMilliseconds()
    {
        Assert.Equal(3_723_004, ClickIntervalCalculator.ToMilliseconds(1, 2, 3, 4));
    }

    [Fact]
    public void AcceptsExactlyOneDay()
    {
        Assert.Equal(86_400_000, ClickIntervalCalculator.ToMilliseconds(24, 0, 0, 0));
    }

    [Fact]
    public void RejectsIntervalsAboveOneDay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClickIntervalCalculator.ToMilliseconds(24, 0, 0, 1));
    }

    [Fact]
    public void RejectsNegativeComponents()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClickIntervalCalculator.ToMilliseconds(0, 0, -1, 0));
    }
}
