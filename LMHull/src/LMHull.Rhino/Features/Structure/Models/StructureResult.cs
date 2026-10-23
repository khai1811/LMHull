using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

public class StructureResult
{
    public List<Brep> Bulkheads { get; } = new();
    public List<Curve> FrameInnerCurves { get; } = new();
}
