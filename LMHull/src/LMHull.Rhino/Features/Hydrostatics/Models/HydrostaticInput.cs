using System;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Hydrostatics.Models;

public record HydrostaticInput(Brep Hull, double Draft, double WaterDensity = 1.025, double Tolerance = 0.01);
