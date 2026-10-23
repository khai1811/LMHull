using System;
using Eto.Forms;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Ô nhập số có nhãn tiêu đề và nhãn đơn vị: [Title] [NumericTextBox] [Unit].
/// </summary>
public class LabeledNumericInput : Panel
{
    private readonly Label _lblTitle;
    private readonly NumericTextBox _numberBox;
    private readonly Label _lblUnit;

    public event EventHandler<EventArgs>? ValueChanged;

    public LabeledNumericInput(string title, string unit = "", double min = double.MinValue, double max = double.MaxValue, int decimalPlaces = 2, double value = 0.0)
    {
        _lblTitle = new Label { Text = title, Width = UiMetrics.LabelWidth, VerticalAlignment = VerticalAlignment.Center };
        _numberBox = new NumericTextBox
        {
            MinValue = min,
            MaxValue = max,
            DecimalPlaces = decimalPlaces,
            Value = value,
            Width = UiMetrics.NumericWidth
        };
        _lblUnit = new Label { Text = unit, Width = UiMetrics.UnitWidth, VerticalAlignment = VerticalAlignment.Center };

        _numberBox.ValueChanged += (_, e) => ValueChanged?.Invoke(this, e);

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows = { new TableRow(_lblTitle, new TableCell(_numberBox, true), _lblUnit) }
        };
    }

    public double Value
    {
        get => _numberBox.Value;
        set => _numberBox.Value = value;
    }

    public string Title
    {
        get => _lblTitle.Text;
        set => _lblTitle.Text = value;
    }

    public string Unit
    {
        get => _lblUnit.Text;
        set => _lblUnit.Text = value;
    }

    public double MinValue
    {
        get => _numberBox.MinValue;
        set => _numberBox.MinValue = value;
    }

    public double MaxValue
    {
        get => _numberBox.MaxValue;
        set => _numberBox.MaxValue = value;
    }

    public int DecimalPlaces
    {
        get => _numberBox.DecimalPlaces;
        set => _numberBox.DecimalPlaces = value;
    }

    public double Increment
    {
        get => _numberBox.Increment;
        set => _numberBox.Increment = value;
    }
}
