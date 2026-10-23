using System;
using System.Collections.Generic;
using Eto.Forms;

namespace LMHull.Rhino.UI.Common;

/// <summary>
/// Danh mục chọn có nhãn tiêu đề: [Title] [DropDown].
/// </summary>
public class LabeledDropdown : Panel
{
    private readonly Label _lblTitle;
    private readonly DropDown _dropDown;

    public event EventHandler<EventArgs>? SelectedIndexChanged;

    public LabeledDropdown(string title, IEnumerable<string>? items = null)
    {
        _lblTitle = new Label { Text = title, Width = UiMetrics.LabelWidth, VerticalAlignment = VerticalAlignment.Center };
        _dropDown = new DropDown();
        _dropDown.SelectedIndexChanged += (_, e) => SelectedIndexChanged?.Invoke(this, e);

        if (items != null) SetItems(items);

        Padding = UiMetrics.Padding;
        Content = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows = { new TableRow(_lblTitle, new TableCell(_dropDown, true)) }
        };
    }

    /// <summary>
    /// Nạp lại danh sách item và chọn item đầu tiên (nếu có).
    /// </summary>
    public void SetItems(IEnumerable<string> items)
    {
        _dropDown.Items.Clear();
        foreach (var item in items)
        {
            _dropDown.Items.Add(item);
        }
        _dropDown.SelectedIndex = _dropDown.Items.Count > 0 ? 0 : -1;
    }

    public int SelectedIndex
    {
        get => _dropDown.SelectedIndex;
        set => _dropDown.SelectedIndex = value;
    }

    /// <summary>
    /// Text của item đang chọn; gán text không có trong danh sách sẽ bị bỏ qua.
    /// </summary>
    public string? SelectedText
    {
        get => SelectedIndex >= 0 ? _dropDown.Items[SelectedIndex].Text : null;
        set
        {
            for (int i = 0; i < _dropDown.Items.Count; i++)
            {
                if (_dropDown.Items[i].Text == value)
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }
    }

    public string Title
    {
        get => _lblTitle.Text;
        set => _lblTitle.Text = value;
    }
}
