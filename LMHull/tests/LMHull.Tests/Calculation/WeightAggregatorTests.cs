using LMHull.Core.Calculation;
using Xunit;

namespace LMHull.Tests.Calculation;

public class WeightAggregatorTests
{
    [Fact]
    public void Add_ComputesTotalAndWeightedCenter()
    {
        var agg = new WeightAggregator();
        agg.Add(2.0, 0, 0, 0);
        agg.Add(6.0, 4, 2, 1);

        Assert.Equal(2, agg.Count);
        Assert.Equal(8.0, agg.TotalWeight, 9);
        var (x, y, z) = agg.CenterOfGravity!.Value;
        Assert.Equal(3.0, x, 9);
        Assert.Equal(1.5, y, 9);
        Assert.Equal(0.75, z, 9);
    }

    [Fact]
    public void Add_IgnoresNonPositiveWeights()
    {
        var agg = new WeightAggregator();
        agg.Add(0, 10, 10, 10);
        agg.Add(-1, 10, 10, 10);
        agg.Add(double.NaN, 10, 10, 10);

        Assert.Equal(0, agg.Count);
        Assert.Null(agg.CenterOfGravity);
    }
}
