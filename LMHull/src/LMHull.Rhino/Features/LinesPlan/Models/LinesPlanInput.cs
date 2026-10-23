using Rhino.Geometry;

namespace LMHull.Rhino.Features.LinesPlan.Models;

public record LinesPlanInput(Brep Hull, int StationCount = 11, int WaterlineCount = 5, int ButtockCount = 3, double Tolerance = 0.01);
