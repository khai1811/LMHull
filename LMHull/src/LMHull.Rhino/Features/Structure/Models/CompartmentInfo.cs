using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

public class CompartmentInfo
{
    public string Name { get; set; } = string.Empty;
    public Brep Geometry { get; set; } = null!;
    public double Volume { get; set; }
    public Point3d CenterOfGravity { get; set; }
}
