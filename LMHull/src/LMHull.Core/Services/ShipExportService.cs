using System.Globalization;
using System.Text;
using System.Text.Json;
using LMHull.Core.Models;

namespace LMHull.Core.Services;

/// <summary>
/// Dựng nội dung CSV / JSON cho thông số tàu và dữ liệu thủy tĩnh (việc ghi tệp do Infrastructure đảm nhận).
/// </summary>
public static class ShipExportService
{
    /// <summary>
    /// Nội dung CSV của thông số và kết quả thủy tĩnh.
    /// </summary>
    public static string BuildCsv(ShipInput input, ShipResult result)
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

        return sb.ToString();
    }

    /// <summary>
    /// Nội dung JSON định dạng đẹp (Indented) của thông số và kết quả thủy tĩnh.
    /// </summary>
    public static string BuildJson(ShipInput input, ShipResult result)
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
        return JsonSerializer.Serialize(exportData, options);
    }
}