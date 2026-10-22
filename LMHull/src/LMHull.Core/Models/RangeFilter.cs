namespace LMHull.Core.Models;

public struct RangeFilter
{
    public double Min { get; set; }
    public double Max { get; set; }

    public RangeFilter(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public bool IsInRange(double value) => value >= Min && value <= Max;
}