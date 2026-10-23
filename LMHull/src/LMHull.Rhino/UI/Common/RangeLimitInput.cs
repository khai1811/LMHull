using System;
using Eto.Forms;
using Rhino.Geometry;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Khung giới hạn khoảng cho tab Limits: [✓ X] &gt; [min] and &lt; [max] [Unit].
/// </summary>
public class RangeLimitInput : Panel
{
    private readonly CheckBox _chkEnabled;
    private readonly NumericTextBox _numMin;
    private readonly NumericTextBox _numMax;
    private bool _suppressEvents;

    public event EventHandler<EventArgs>? RangeChanged;

    public RangeLimitInput(string axisName, string unit = "m", int decimalPlaces = 3, bool enabled = true)
    {
        _chkEnabled = new CheckBox { Text = axisName, Checked = enabled, Width = UiMetrics.LabelWidth };
        _numMin = CreateNumberBox(decimalPlaces);
        _numMax = CreateNumberBox(decimalPlaces);

        _chkEnabled.CheckedChanged += (_, e) =>
        {
            UpdateEnabledState();
            RangeChanged?.Invoke(this, e);
        };
        UpdateEnabledState();

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows =
            {
                new TableRow(
                    _chkEnabled,
                    CenteredLabel(">"), new TableCell(_numMin, true),
                    CenteredLabel("and <"), new TableCell(_numMax, true),
                    new Label { Text = unit, Width = UiMetrics.UnitWidth, VerticalAlignment = VerticalAlignment.Center })
            }
        };
    }

    /// <summary>
    /// Khoảng giới hạn [min, max]. Khi gán, hai đầu được sắp tăng dần.
    /// </summary>
    public Interval Range
    {
        get => new(_numMin.Value, _numMax.Value);
        set
        {
            _suppressEvents = true;
            _numMin.Value = Math.Min(value.T0, value.T1);
            _numMax.Value = Math.Max(value.T0, value.T1);
            _suppressEvents = false;
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsLimitEnabled
    {
        get => _chkEnabled.Checked == true;
        set => _chkEnabled.Checked = value;
    }

    /// <summary>
    /// Kiểm tra giá trị có nằm trong khoảng mở (min, max); luôn đúng nếu giới hạn đang tắt.
    /// </summary>
    public bool Contains(double value) => !IsLimitEnabled || (value > _numMin.Value && value < _numMax.Value);

    private void UpdateEnabledState()
    {
        _numMin.Enabled = IsLimitEnabled;
        _numMax.Enabled = IsLimitEnabled;
    }

    private NumericTextBox CreateNumberBox(int decimalPlaces)
    {
        var numberBox = new NumericTextBox { DecimalPlaces = decimalPlaces, Width = UiMetrics.NumericWidth };
        numberBox.ValueChanged += (_, e) =>
        {
            if (!_suppressEvents) RangeChanged?.Invoke(this, e);
        };
        return numberBox;
    }

    private static Label CenteredLabel(string text) =>
        new() { Text = text, VerticalAlignment = VerticalAlignment.Center };
}
