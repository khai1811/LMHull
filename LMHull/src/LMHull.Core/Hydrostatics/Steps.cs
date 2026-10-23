using System;
using System.Collections.Generic;

namespace LMHull.Core.Hydrostatics;

/// <summary>
/// Sinh dải giá trị cách đều (mớn nước, mức đo két…).
/// </summary>
public static class Steps
{
    /// <summary>
    /// Các giá trị từ min tới max theo bước; luôn gồm cả max (bước cuối có thể ngắn hơn).
    /// </summary>
    public static IReadOnlyList<double> Range(double min, double max, double step)
    {
        if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step), "Bước phải lớn hơn 0.");
        if (max < min) (min, max) = (max, min);

        var values = new List<double>();
        for (int i = 0; ; i++)
        {
            double v = Math.Round(min + i * step, 6);
            if (v >= max - 1e-9) break;
            values.Add(v);
        }
        values.Add(max);
        return values;
    }
}
