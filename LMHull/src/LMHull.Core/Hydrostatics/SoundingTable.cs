using System;
using System.Collections.Generic;
using System.Linq;

namespace LMHull.Core.Hydrostatics;

/// <summary>
/// Bảng dung tích két theo chiều cao: nội suy thể tích theo chiều cao và chiều cao theo thể tích.
/// </summary>
public class SoundingTable
{
    public IReadOnlyList<SoundingRow> Rows { get; }

    public double Capacity => Rows.Count == 0 ? 0 : Rows[^1].Volume;

    public SoundingTable(IEnumerable<SoundingRow> rows)
    {
        Rows = rows.OrderBy(r => r.Sounding).ToList();
    }

    public double VolumeAt(double sounding) => Interpolate(sounding, r => r.Sounding, r => r.Volume);

    /// <summary>
    /// Chiều cao chất lỏng ứng với thể tích (thể tích tăng đơn điệu theo chiều cao).
    /// </summary>
    public double SoundingAt(double volume) => Interpolate(volume, r => r.Volume, r => r.Sounding);

    private double Interpolate(double x, Func<SoundingRow, double> key, Func<SoundingRow, double> value)
    {
        if (Rows.Count == 0) throw new InvalidOperationException("Bảng dung tích rỗng.");
        if (x <= key(Rows[0])) return value(Rows[0]);
        if (x >= key(Rows[^1])) return value(Rows[^1]);

        for (int i = 1; i < Rows.Count; i++)
        {
            if (x > key(Rows[i])) continue;
            var a = Rows[i - 1];
            var b = Rows[i];
            double span = key(b) - key(a);
            double f = span <= 0 ? 0 : (x - key(a)) / span;
            return value(a) + f * (value(b) - value(a));
        }
        return value(Rows[^1]);
    }
}
