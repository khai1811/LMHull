using System;

namespace LMHull.Core.Models;

public class ShipInput
{
    public string Name { get; set; }
    public double Length { get; set; }
    public double Beam { get; set; }
    public double Draft { get; set; }
    public double? Depth { get; set; } // Nullable

    public ShipInput(string name, double length, double beam, double draft, double? depth = null)
    {
        if (length <= 0 || beam <= 0 || draft <= 0)
            throw new ArgumentException("Kích thước L, B, T phải lớn hơn 0.");

        Name = name;
        Length = length;
        Beam = beam;
        Draft = draft;
        Depth = depth;
    }
}