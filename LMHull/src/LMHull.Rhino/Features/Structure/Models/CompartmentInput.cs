using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

/// <param name="Limits">Chỉ giữ các khoang có trọng tâm nằm trong vùng giới hạn; null = không giới hạn.</param>
public record CompartmentInput(Brep Hull, double[] StationXPositions, double Tolerance = 0.01, CalculationLimits? Limits = null);
