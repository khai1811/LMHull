using System;
using System.Globalization;

namespace LMHull.Core.Models;

/// <summary>
/// Giới hạn mở (Min, Max) trên một trục; khi tắt thì mọi giá trị đều hợp lệ.
/// </summary>
public class AxisLimit
{
    public bool Enabled { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }

    public bool Contains(double value) => !Enabled || (value > Math.Min(Min, Max) && value < Math.Max(Min, Max));

    public string Describe(string axis) =>
        string.Format(CultureInfo.InvariantCulture, "{0} > {1:F2} và < {2:F2}", axis, Math.Min(Min, Max), Math.Max(Min, Max));
}
