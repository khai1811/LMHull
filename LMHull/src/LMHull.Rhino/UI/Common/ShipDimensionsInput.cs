using System;
using Eto.Forms;
using LMHull.Core.Models;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Bộ nhập kích thước chính của tàu: L, B, T, D (m).
/// </summary>
public class ShipDimensionsInput : Panel
{
    private readonly LabeledNumericInput _length;
    private readonly LabeledNumericInput _beam;
    private readonly LabeledNumericInput _draft;
    private readonly LabeledNumericInput _depth;

    public event EventHandler<EventArgs>? ValueChanged;

    public ShipDimensionsInput(double length = 50.0, double beam = 10.0, double draft = 2.5, double depth = 4.0, double minDraft = 0.2)
    {
        _length = CreateInput("Chiều dài L:", 5.0, 300.0, 1.0, length);
        _beam = CreateInput("Chiều rộng B:", 2.0, 60.0, 0.5, beam);
        _draft = CreateInput("Chiều chìm T:", minDraft, 20.0, 0.1, draft);
        _depth = CreateInput("Chiều cao mạn D:", 1.0, 30.0, 0.2, depth);

        var layout = new DynamicLayout { Spacing = UiMetrics.Spacing };
        layout.AddRow(_length);
        layout.AddRow(_beam);
        layout.AddRow(_draft);
        layout.AddRow(_depth);
        Content = layout;
    }

    public double Length
    {
        get => _length.Value;
        set => _length.Value = value;
    }

    public double Beam
    {
        get => _beam.Value;
        set => _beam.Value = value;
    }

    public double Draft
    {
        get => _draft.Value;
        set => _draft.Value = value;
    }

    public double Depth
    {
        get => _depth.Value;
        set => _depth.Value = value;
    }

    public ShipInput ToShipInput(string name) => new(name, Length, Beam, Draft, Depth);

    private LabeledNumericInput CreateInput(string title, double min, double max, double increment, double value)
    {
        var input = new LabeledNumericInput(title, "m", min, max, decimalPlaces: 2, value: value) { Increment = increment };
        input.ValueChanged += (_, e) => ValueChanged?.Invoke(this, e);
        return input;
    }
}
