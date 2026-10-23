using System;
using LMHull.Core.Hydrostatics;
using Xunit;

namespace LMHull.Tests.Hydrostatics;

public class HydrostaticTableTests
{
    private static HydrostaticRow Row(double t, double disp) => new(t, disp / 1.025, disp, 450, 1.0, t / 2, 22.5, 22.5);

    [Fact]
    public void Drafts_IncludesMaxEvenWhenStepDoesNotDivide()
    {
        Assert.Equal(new[] { 0.5, 1.0, 1.5, 2.0, 2.2 }, HydrostaticTable.Drafts(0.5, 2.2, 0.5));
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, HydrostaticTable.Drafts(3.0, 1.0, 1.0));
    }

    [Fact]
    public void Drafts_RejectsNonPositiveStep()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HydrostaticTable.Drafts(1, 2, 0));
    }

    [Fact]
    public void Tpc_IsAreaTimesDensityOver100()
    {
        Assert.Equal(4.6125, HydrostaticTable.Tpc(450, 1.025), 9);
    }

    [Fact]
    public void Interpolate_LinearBetweenRowsAndClampedOutside()
    {
        var table = new HydrostaticTable(new[] { Row(2.0, 200), Row(1.0, 100) });

        Assert.Equal(150, table.Interpolate(1.5, r => r.Displacement), 9);
        Assert.Equal(100, table.Interpolate(0.2, r => r.Displacement), 9);
        Assert.Equal(200, table.Interpolate(9.0, r => r.Displacement), 9);
        Assert.Equal(1.0, table.Rows[0].Draft);
    }
}
