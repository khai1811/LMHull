using System;
using System.Collections.Generic;
using System.Linq;

namespace LMHull.Core.Hydrostatics;

/// <summary>
/// Bảng thủy tĩnh theo mớn nước: dải mớn tính, TPC và nội suy tuyến tính theo T.
/// </summary>
public class HydrostaticTable
{
    public IReadOnlyList<HydrostaticRow> Rows { get; }

    public HydrostaticTable(IEnumerable<HydrostaticRow> rows)
    {
        Rows = rows.OrderBy(r => r.Draft).ToList();
    }

    /// <summary>
    /// Các mớn nước từ min tới max theo bước; luôn gồm cả max (bước cuối có thể ngắn hơn).
    /// </summary>
    public static IReadOnlyList<double> Drafts(double min, double max, double step) => Steps.Range(min, max, step);

    /// <summary>
    /// Số tấn làm chìm 1 cm: TPC = Aw · ρ / 100.
    /// </summary>
    public static double Tpc(double waterplaneArea, double density) => waterplaneArea * density / 100.0;

    /// <summary>
    /// Nội suy tuyến tính đại lượng chọn theo mớn nước; ngoài dải bảng thì lấy giá trị biên.
    /// </summary>
    public double Interpolate(double draft, Func<HydrostaticRow, double> selector)
    {
        if (Rows.Count == 0) throw new InvalidOperationException("Bảng thủy tĩnh rỗng.");
        if (draft <= Rows[0].Draft) return selector(Rows[0]);
        if (draft >= Rows[^1].Draft) return selector(Rows[^1]);

        for (int i = 1; i < Rows.Count; i++)
        {
            if (draft > Rows[i].Draft) continue;
            var a = Rows[i - 1];
            var b = Rows[i];
            double f = (draft - a.Draft) / (b.Draft - a.Draft);
            return selector(a) + f * (selector(b) - selector(a));
        }
        return selector(Rows[^1]);
    }
}
