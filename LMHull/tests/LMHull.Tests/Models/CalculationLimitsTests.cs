using LMHull.Core.Models;
using Xunit;

namespace LMHull.Tests.Models;

public class CalculationLimitsTests
{
    [Fact]
    public void Inactive_ContainsEverything()
    {
        var limits = new CalculationLimits();

        Assert.False(limits.IsActive);
        Assert.True(limits.Contains(-1e6, 1e6, 0));
        Assert.Equal(string.Empty, limits.Describe());
    }

    [Fact]
    public void EnabledAxis_IsOpenIntervalAndAcceptsSwappedBounds()
    {
        var limits = new CalculationLimits { X = new AxisLimit { Enabled = true, Min = 20, Max = 10 } };

        Assert.True(limits.Contains(15, 0, 0));
        Assert.False(limits.Contains(10, 0, 0));
        Assert.False(limits.Contains(25, 0, 0));
        Assert.Equal("Giới hạn: X > 10.00 và < 20.00", limits.Describe());
    }

    [Fact]
    public void AllEnabledAxesMustMatch()
    {
        var limits = new CalculationLimits
        {
            X = new AxisLimit { Enabled = true, Min = 0, Max = 10 },
            Z = new AxisLimit { Enabled = true, Min = 0, Max = 2 }
        };

        Assert.True(limits.Contains(5, 999, 1));
        Assert.False(limits.Contains(5, 0, 3));
    }
}
