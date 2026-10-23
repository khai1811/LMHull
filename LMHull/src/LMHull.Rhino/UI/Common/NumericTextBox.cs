using System;
using Eto.Forms;
using LMHull.Core.Validation;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Ô nhập số chặn ký tự không hợp lệ ngay khi gõ hoặc dán (thay cho NumericStepper vốn vẫn cho gõ chữ).
/// Giá trị được kẹp trong [MinValue, MaxValue] khi rời ô hoặc nhấn Enter; phím ↑/↓ tăng giảm theo Increment.
/// </summary>
public class NumericTextBox : TextBox
{
    private double _value;
    private double _minValue = double.MinValue;
    private double _maxValue = double.MaxValue;
    private int _decimalPlaces = 2;
    private bool _suppressTextEvents;

    public event EventHandler<EventArgs>? ValueChanged;

    public NumericTextBox()
    {
        TextAlignment = TextAlignment.Right;
        TextChanging += OnTextChanging;
        TextChanged += OnTextChanged;
        LostFocus += (_, _) => Commit();
        KeyDown += OnKeyDown;
        RefreshText();
    }

    public double Value
    {
        get => _value;
        set => SetValue(value, refreshText: true);
    }

    public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            SetValue(_value, refreshText: true);
        }
    }

    public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            SetValue(_value, refreshText: true);
        }
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            _decimalPlaces = Math.Max(0, value);
            SetValue(_value, refreshText: true);
        }
    }

    public double Increment { get; set; } = 1.0;

    private bool AllowNegative => _minValue < 0;

    // Hủy thay đổi nếu chuỗi mới không phải số hợp lệ (áp dụng cả khi gõ lẫn khi dán)
    private void OnTextChanging(object? sender, TextChangingEventArgs e)
    {
        if (_suppressTextEvents) return;
        if (!NumericTextRules.IsAcceptable(e.NewText, _decimalPlaces, AllowNegative)) e.Cancel = true;
    }

    // Cập nhật giá trị ngay khi đang gõ nếu chuỗi đã là số nằm trong khoảng cho phép
    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_suppressTextEvents) return;
        if (NumericTextRules.TryParse(Text, out double parsed) && parsed >= _minValue && parsed <= _maxValue)
        {
            SetValue(parsed, refreshText: false);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Keys.Up:
                Value = _value + Increment;
                e.Handled = true;
                break;
            case Keys.Down:
                Value = _value - Increment;
                e.Handled = true;
                break;
            case Keys.Enter:
                Commit();
                break;
        }
    }

    /// <summary>
    /// Chốt nội dung đang gõ: kẹp vào khoảng cho phép; ô rỗng hoặc dở dang thì trả về giá trị hợp lệ gần nhất.
    /// </summary>
    private void Commit()
    {
        double target = NumericTextRules.TryParse(Text, out double parsed) ? parsed : _value;
        SetValue(target, refreshText: true);
    }

    private void SetValue(double value, bool refreshText)
    {
        double clamped = Math.Round(Math.Clamp(value, _minValue, _maxValue), _decimalPlaces);
        bool changed = clamped != _value;
        _value = clamped;

        if (refreshText) RefreshText();
        if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshText()
    {
        _suppressTextEvents = true;
        Text = NumericTextRules.Format(_value, _decimalPlaces);
        _suppressTextEvents = false;
    }
}
