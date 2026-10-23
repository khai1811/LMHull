using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LMHull.Core.Reporting;

/// <summary>
/// Bảng kết quả của một lần chạy lệnh tính toán: tiêu đề, cột, các hàng dữ liệu và ghi chú tổng kết.
/// </summary>
public class ResultTable
{
    private readonly List<string[]> _rows = new();

    public string Title { get; }
    public string Source { get; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<string[]> Rows => _rows;
    public List<string> Notes { get; } = new();

    public ResultTable(string title, string source, params string[] columns)
    {
        if (columns == null || columns.Length == 0) throw new ArgumentException("Bảng phải có ít nhất một cột.", nameof(columns));

        Title = title;
        Source = source;
        Columns = columns;
    }

    /// <summary>
    /// Bảng dạng Hạng mục | Giá trị | Đơn vị.
    /// </summary>
    public static ResultTable KeyValue(string title, string source) => new(title, source, "Hạng mục", "Giá trị", "Đơn vị");

    public ResultTable AddRow(params string[] cells)
    {
        if (cells.Length != Columns.Count)
            throw new ArgumentException($"Số ô ({cells.Length}) không khớp số cột ({Columns.Count}).", nameof(cells));

        _rows.Add(cells);
        return this;
    }

    /// <summary>
    /// Thêm hàng số cho bảng 3 cột Hạng mục | Giá trị | Đơn vị.
    /// </summary>
    public ResultTable AddRow(string name, double value, string format, string unit = "") =>
        AddRow(name, value.ToString(format, CultureInfo.InvariantCulture), unit);

    public string ToCsv() => Join(",", EscapeCsv);

    /// <summary>
    /// Dạng phân tách bằng Tab để dán thẳng vào Excel.
    /// </summary>
    public string ToTsv() => Join("\t", cell => cell.Replace('\t', ' ').Replace("\r", " ").Replace("\n", " "));

    private string Join(string separator, Func<string, string> escape)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, Columns.Select(escape)));
        foreach (var row in _rows)
        {
            sb.AppendLine(string.Join(separator, row.Select(c => escape(c ?? string.Empty))));
        }
        return sb.ToString();
    }

    private static string EscapeCsv(string cell)
    {
        bool needsQuotes = cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        return needsQuotes ? $"\"{cell.Replace("\"", "\"\"")}\"" : cell;
    }
}
