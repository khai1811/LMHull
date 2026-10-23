using System.IO;
using System.Text;
using LMHull.Core.Abstractions;
using LMHull.Core.Models;
using LMHull.Core.Reporting;
using LMHull.Core.Services;

namespace LMHull.Infrastructure.Export;

/// <summary>
/// Ghi bảng kết quả / thông số tàu ra tệp, mã hóa UTF-8 có BOM để Excel đọc đúng tiếng Việt.
/// </summary>
public class FileResultExporter : IResultExporter
{
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

    public void ExportCsv(ResultTable table, string path) => Write(path, table.ToCsv());

    public void ExportShip(ShipInput input, ShipResult result, string path, ExportFormat format)
    {
        string content = format == ExportFormat.Json
            ? ShipExportService.BuildJson(input, result)
            : ShipExportService.BuildCsv(input, result);
        Write(path, content);
    }

    private static void Write(string path, string content)
    {
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, content, Utf8WithBom);
    }
}
