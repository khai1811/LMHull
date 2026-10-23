using Eto.Drawing;
using Eto.Forms;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Hàng hiển thị kết quả: [Tên chỉ số] [Giá trị đậm/xanh] [Unit].
/// </summary>
public class ResultDisplayRow : Panel
{
    private readonly Label _lblValue;
    private readonly Label _lblUnit;

    public ResultDisplayRow(string title, string unit = "", string initialText = "-")
    {
        _lblValue = new Label
        {
            Text = initialText,
            Font = SystemFonts.Bold(),
            TextColor = UiMetrics.AccentColor,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        _lblUnit = new Label { Text = unit, Width = UiMetrics.UnitWidth, VerticalAlignment = VerticalAlignment.Center };

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows =
            {
                new TableRow(
                    new Label { Text = title, Width = UiMetrics.LabelWidth, VerticalAlignment = VerticalAlignment.Center },
                    new TableCell(_lblValue, true),
                    _lblUnit)
            }
        };
    }

    public void SetValue(string text) => _lblValue.Text = text;

    public void SetValue(double val, string format = "F3") => _lblValue.Text = val.ToString(format);

    public string Unit
    {
        get => _lblUnit.Text;
        set => _lblUnit.Text = value;
    }
}
