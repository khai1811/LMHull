namespace LMHull.Core.Models;

/// <summary>
/// Cấu hình người dùng của LMHull (lưu tại %AppData%/LMHull/settings.json).
/// </summary>
public class LMHullSettings
{
    public double SeaWaterDensity { get; set; } = 1.025;
    public double FreshWaterDensity { get; set; } = 1.000;
    public string DefaultMaterial { get; set; } = "Steel AH36";
    public double DefaultThicknessMm { get; set; } = 10.0;
    public string ExportFolder { get; set; } = string.Empty;
    public int MaxResultHistory { get; set; } = 30;
    public CalculationLimits Limits { get; set; } = new();

    // Gốc tham chiếu khi báo cáo LCB / LCF / LCG (mặc định AP / CL / BL = gốc tọa độ)
    public double ReferenceX { get; set; }
    public double ReferenceY { get; set; }
    public double ReferenceZ { get; set; }
}
