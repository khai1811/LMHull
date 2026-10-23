using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

/// <param name="Step">Bước đo chiều cao chất lỏng (m).</param>
public record TankSoundingInput(Brep Tank, double Step, double Tolerance = 0.01);
