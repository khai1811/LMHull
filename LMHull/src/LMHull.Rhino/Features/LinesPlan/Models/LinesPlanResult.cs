using System;
using System.Collections.Generic;
using System.Drawing;
using LMHull.Rhino.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.LinesPlan.Models;

public class LinesPlanResult
{
    public List<Curve> StationCurves { get; } = new();
    public List<Curve> WaterlineCurves { get; } = new();
    public List<Curve> ButtockCurves { get; } = new();
}
