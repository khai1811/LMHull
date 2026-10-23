using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Reporting;
using LMHull.Rhino.UI.Common;

namespace LMHull.Rhino.Features.Reports.UI;

/// <summary>
/// Hiển thị một ResultTable: tiêu đề, lưới dữ liệu (cột số căn phải) và các dòng ghi chú.
/// </summary>
public class ResultTableView : Panel
{
    public ResultTable Table { get; }

    public ResultTableView(ResultTable table)
    {
        Table = table;

        var grid = new GridView { DataStore = table.Rows.Cast<object>().ToList(), GridLines = GridLines.Both };
        for (int i = 0; i < table.Columns.Count; i++)
        {
            grid.Columns.Add(new GridColumn
            {
                HeaderText = table.Columns[i],
                DataCell = new TextBoxCell(i) { TextAlignment = IsNumericColumn(table, i) ? TextAlignment.Right : TextAlignment.Left },
                AutoSize = true,
                Editable = false
            });
        }

        var layout = new DynamicLayout { Padding = new Padding(6), Spacing = UiMetrics.Spacing };
        layout.AddRow(new Label { Text = table.Title, Font = SystemFonts.Bold() });
        layout.Add(grid, yscale: true);
        foreach (var note in table.Notes)
        {
            layout.AddRow(new Label { Text = note, TextColor = UiMetrics.AccentColor });
        }

        Content = layout;
    }

    private static bool IsNumericColumn(ResultTable table, int column) =>
        table.Rows.Count > 0 && table.Rows.All(r =>
            double.TryParse(r[column], NumberStyles.Float, CultureInfo.InvariantCulture, out _) || string.IsNullOrEmpty(r[column]));
}
