using System.Collections.Generic;

namespace LMHull.Core.Models;

/// <summary>
/// Vùng giới hạn tính toán theo X / Y / Z (tab Limits). Điểm phải thỏa mọi trục đang bật.
/// </summary>
public class CalculationLimits
{
    public AxisLimit X { get; set; } = new();
    public AxisLimit Y { get; set; } = new();
    public AxisLimit Z { get; set; } = new();

    public bool IsActive => X.Enabled || Y.Enabled || Z.Enabled;

    public bool Contains(double x, double y, double z) => X.Contains(x) && Y.Contains(y) && Z.Contains(z);

    /// <summary>
    /// Mô tả các trục đang bật, dùng ghi chú dưới bảng kết quả; rỗng nếu không giới hạn.
    /// </summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (X.Enabled) parts.Add(X.Describe("X"));
        if (Y.Enabled) parts.Add(Y.Describe("Y"));
        if (Z.Enabled) parts.Add(Z.Describe("Z"));
        return parts.Count == 0 ? string.Empty : "Giới hạn: " + string.Join("; ", parts);
    }
}
