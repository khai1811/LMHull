using Eto.Drawing;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Thông số bố cục dùng chung cho các component UI để padding/spacing đồng nhất.
/// </summary>
public static class UiMetrics
{
    public static readonly Size Spacing = new(4, 4);
    public static readonly Padding Padding = new(0, 2);
    public const int LabelWidth = 110;
    public const int UnitWidth = 28;
    public const int NumericWidth = 80;
    public static readonly Color AccentColor = Color.FromArgb(0, 90, 180);
}
