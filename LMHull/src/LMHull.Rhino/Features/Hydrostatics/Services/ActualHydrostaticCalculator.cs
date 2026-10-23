using System;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Hydrostatics.Calculations;
using LMHull.Rhino.Features.Hydrostatics.Models;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Hydrostatics.Services;

public static class ActualHydrostaticCalculator
{
    /// <summary>
    /// Tính toán các đặc trưng thủy tĩnh thực tế của Brep vỏ tàu tại mớn nước T.
    /// </summary>
    public static DetailedHydroResult? Compute(Brep hullBrep, double draft, double waterDensity = 1.025, double tolerance = 0.01)
    {
        return new HydrostaticCalculation().Run(new HydrostaticInput(hullBrep, draft, waterDensity, tolerance)).Value;
    }
}
