using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.Models;

/// <param name="Drafts">Các mớn nước cần tính (m, tính từ đáy vỏ).</param>
/// <param name="ReferenceX">Gốc đo dọc cho LCB / LCF.</param>
public record HydrostaticCurvesInput(Brep Hull, IReadOnlyList<double> Drafts, double WaterDensity, double Tolerance, double ReferenceX = 0.0);
