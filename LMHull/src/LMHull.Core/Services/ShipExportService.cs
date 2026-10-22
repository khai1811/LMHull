using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using LMHull.Core.Models;

namespace LMHull.Core.Services;

/// <summary>
/// Chịu trách nhiệm xuất thông số tàu và dữ liệu thủy tĩnh ra CSV và JSON.
/// </summary>
public static class ShipExportService
{
    /// <summary>
    /// Xuất thông số và kết quả thủy tĩnh ra tệp CSV.
    /// </summary>
    public static void ExportToCsv(string filePath, ShipInput input, ShipResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ThongSo,GiaTri,DonVi");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TenTau,\"{0}\",-", input.Name));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "ChieuDai_L,{0:F2},m", input.Length));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "ChieuRong_B,{0:F2},m", input.Beam));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "ChieuChim_T,{0:F2},m", input.Draft));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "ChieuCaoMan_D,{0:F2},m", input.Depth ?? 0));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TySo_L_B,{0:F2},-", result.LBRatio));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TheTichChim_V,{0:F2},m3", result.BoxVolume));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "LuongChiemNuoc_Delta,{0:F2},Tan", result.Displacement));

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Xuất thông số và kết quả thủy tĩnh ra tệp JSON định dạng đẹp (Indented).
    /// </summary>
    public static void ExportToJson(string filePath, ShipInput input, ShipResult result)
    {
        var exportData = new
        {
            Project = "LMHull Marine",
            ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Input = new
            {
                Name = input.Name,
                Length = input.Length,
                Beam = input.Beam,
                Draft = input.Draft,
                Depth = input.Depth
            },
            Hydrostatics = new
            {
                LBRatio = result.LBRatio,
                SubmergedVolume = result.BoxVolume,
                Displacement = result.Displacement
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(exportData, options);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}