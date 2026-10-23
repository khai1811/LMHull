using LMHull.Core.Models;
using LMHull.Core.Reporting;

namespace LMHull.Core.Abstractions;

/// <summary>
/// Xuất bảng kết quả và thông số tàu ra tệp.
/// </summary>
public interface IResultExporter
{
    void ExportCsv(ResultTable table, string path);

    void ExportShip(ShipInput input, ShipResult result, string path, ExportFormat format);
}
