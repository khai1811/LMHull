namespace LMHull.Core.Models;

public class ShipMaterial
{
    public string Name { get; set; }
    public double DensityTonPerM3 { get; set; } // Khối lượng riêng (Tấn/m3)
    public double YieldStressMpa { get; set; }   // Giới hạn chảy (MPa)

    public ShipMaterial(string name, double densityTonPerM3, double yieldStressMpa)
    {
        Name = name;
        DensityTonPerM3 = densityTonPerM3;
        YieldStressMpa = yieldStressMpa;
    }
}