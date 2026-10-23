using System;
using System.Collections.Generic;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class InternalStructureGenerator
{
    /// <summary>
    /// Dựng các vách ngang kín nước bên trong thân tàu kín (Solid Brep).
    /// </summary>
    public static List<Brep> CreateBulkheads(Brep hullSolid, double[] stationXPositions, double tolerance = 0.01)
    {
        var bulkheads = new List<Brep>();
        foreach (double x in stationXPositions)
        {
            bulkheads.AddRange(HullSectionService.CreatePlanarSections(hullSolid, HullSectionService.TransversePlaneAt(x), tolerance));
        }

        return bulkheads;
    }

    /// <summary>
    /// Tạo đường viền sườn gia cường bên trong (Web Frame Ribs) bằng cách offset đường sườn vào trong lòng vỏ.
    /// </summary>
    public static List<Curve> CreateFrameOffsets(List<Curve> stationCurves, double frameDepth = 0.25, double tolerance = 0.01)
    {
        var offsetCurves = new List<Curve>();

        foreach (var crv in stationCurves)
        {
            // Xác định mặt phẳng chứa đường sườn (mặt phẳng X = const)
            if (crv.TryGetPlane(out Plane plane, tolerance) || Math.Abs(crv.PointAtStart.X - crv.PointAtEnd.X) < tolerance)
            {
                var stationPlane = new Plane(crv.PointAtStart, Vector3d.XAxis);
                // Offset đường cong hướng vào tâm tàu (giảm bán kính mạn)
                var offsets = crv.Offset(stationPlane, -frameDepth, tolerance, CurveOffsetCornerStyle.Sharp);
                if (offsets != null && offsets.Length > 0)
                {
                    offsetCurves.AddRange(offsets);
                }
            }
        }

        return offsetCurves;
    }

    /// <summary>
    /// Ghi kết cấu vào RhinoDoc với các layer kỹ thuật.
    /// </summary>
    public static void BakeStructure(ICadHandle cad, StructureResult result)
    {
        foreach (var bh in result.Bulkheads)
        {
            cad.AddBrep(bh, StructureLayers.Bulkheads.Spec());
        }

        foreach (var rib in result.FrameInnerCurves)
        {
            cad.AddCurve(rib, StructureLayers.FrameRibs.Spec());
        }

        cad.Redraw();
    }
}