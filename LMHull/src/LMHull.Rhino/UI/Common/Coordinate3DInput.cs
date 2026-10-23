using System;
using Eto.Forms;
using Rhino.Geometry;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Cụm nhập tọa độ không gian: [Title] X [ ] Y [ ] Z [ ] [Unit].
/// </summary>
public class Coordinate3DInput : Panel
{
    private readonly NumericTextBox _numX;
    private readonly NumericTextBox _numY;
    private readonly NumericTextBox _numZ;
    private bool _suppressEvents;

    public event EventHandler<EventArgs>? ValueChanged;

    public Coordinate3DInput(string title, string unit = "m", int decimalPlaces = 3)
    {
        _numX = CreateNumberBox(decimalPlaces);
        _numY = CreateNumberBox(decimalPlaces);
        _numZ = CreateNumberBox(decimalPlaces);

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows =
            {
                new TableRow(
                    new Label { Text = title, Width = UiMetrics.LabelWidth, VerticalAlignment = VerticalAlignment.Center },
                    CenteredLabel("X"), new TableCell(_numX, true),
                    CenteredLabel("Y"), new TableCell(_numY, true),
                    CenteredLabel("Z"), new TableCell(_numZ, true),
                    new Label { Text = unit, Width = UiMetrics.UnitWidth, VerticalAlignment = VerticalAlignment.Center })
            }
        };
    }

    /// <summary>
    /// Tọa độ hiện tại. Khi gán, chỉ phát một sự kiện ValueChanged cho cả 3 trục.
    /// </summary>
    public Point3d Value
    {
        get => new(_numX.Value, _numY.Value, _numZ.Value);
        set
        {
            _suppressEvents = true;
            _numX.Value = value.X;
            _numY.Value = value.Y;
            _numZ.Value = value.Z;
            _suppressEvents = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private NumericTextBox CreateNumberBox(int decimalPlaces)
    {
        var numberBox = new NumericTextBox { DecimalPlaces = decimalPlaces, Width = UiMetrics.NumericWidth - 10 };
        numberBox.ValueChanged += (_, e) =>
        {
            if (!_suppressEvents) ValueChanged?.Invoke(this, e);
        };
        return numberBox;
    }

    private static Label CenteredLabel(string text) =>
        new() { Text = text, VerticalAlignment = VerticalAlignment.Center };
}
