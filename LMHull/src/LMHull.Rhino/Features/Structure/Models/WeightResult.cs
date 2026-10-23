using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

public class WeightResult
{
    public double TotalWeightTonnes { get; set; }
    public Point3d CenterOfGravity { get; set; }
    public int ComputedPartCount { get; set; }

    /// <summary>
    /// Trọng lượng gom theo layer, sắp giảm dần theo tấn.
    /// </summary>
    public List<LayerWeight> ByLayer { get; set; } = new();
}
