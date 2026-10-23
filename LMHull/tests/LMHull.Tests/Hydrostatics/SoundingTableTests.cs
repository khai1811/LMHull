using LMHull.Core.Hydrostatics;
using Xunit;

namespace LMHull.Tests.Hydrostatics;

public class SoundingTableTests
{
    // Két hộp 2 x 5 x 4 m: V = 10·h
    private static SoundingTable BoxTank() => new(new[]
    {
        new SoundingRow(4, 40, 100, 2.0),
        new SoundingRow(0, 0, 0, 0),
        new SoundingRow(2, 20, 50, 1.0)
    });

    [Fact]
    public void Rows_SortedAndCapacityIsLastVolume()
    {
        var table = BoxTank();

        Assert.Equal(new[] { 0.0, 2.0, 4.0 }, new[] { table.Rows[0].Sounding, table.Rows[1].Sounding, table.Rows[2].Sounding });
        Assert.Equal(40, table.Capacity);
    }

    [Fact]
    public void VolumeAt_And_SoundingAt_AreInverseLinearInterpolations()
    {
        var table = BoxTank();

        Assert.Equal(25, table.VolumeAt(2.5), 9);
        Assert.Equal(2.5, table.SoundingAt(25), 9);
        Assert.Equal(40, table.VolumeAt(10), 9);
        Assert.Equal(0, table.SoundingAt(-5), 9);
    }

    [Fact]
    public void StepsRange_StartsAtZeroAndEndsAtHeight()
    {
        Assert.Equal(new[] { 0.0, 1.5, 3.0, 4.0 }, Steps.Range(0, 4, 1.5));
    }
}
