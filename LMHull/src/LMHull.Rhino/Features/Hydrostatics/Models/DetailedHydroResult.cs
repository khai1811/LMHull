using System;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Hydrostatics.Models;

/// <summary>
/// Kết quả tính toán thủy tĩnh chi tiết từ hình học Brep thực tế.
/// </summary>
public class DetailedHydroResult
{
    public double SubmergedVolume { get; set; }   // V (m3)
    public double Displacement { get; set; }       // Delta (tấn)
    public double WaterplaneArea { get; set; }     // Aw (m2)
    public Point3d CenterOfBuoyancy { get; set; }  // CB (LCB, TCB, VCB / KB)
    public Point3d CenterOfFlotation { get; set; } // CF (LCF, TCF)
    public double BlockCoefficient { get; set; }   // Cb
}
