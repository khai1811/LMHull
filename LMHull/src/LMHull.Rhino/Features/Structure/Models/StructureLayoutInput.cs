using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Models;

public record StructureLayoutInput(Brep Hull, int BulkheadCount, int FrameStationCount = 9, double FrameDepth = 0.35, double Tolerance = 0.01);
