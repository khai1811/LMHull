using System;
using System.Collections.Generic;
using System.Drawing;
using LMHull.Rhino.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Structure.Services;

public class StructureResult
{
    public List<Brep> Bulkheads { get; } = new();
    public List<Curve> FrameInnerCurves { get; } = new();
}

public static class InternalStructureGenerator
{
    /// <summary>
    /// Dựng các vách ngang kín nước bên trong thân tàu kín (Solid Brep).
    /// </summary>
    public static List<Brep> CreateBulkheads(Brep hullSolid, double[] stationXPositions, double tolerance = 0.01)
    {
        var bulkheads = new List<Brep>();
        BoundingBox bbox = hullSolid.GetBoundingBox(true);

        double width = (bbox.Max.Y - bbox.Min.Y) * 1.5;
        double height = (bbox.Max.Z - bbox.Min.Z) * 1.5;
        double midY = bbox.Center.Y;
        double midZ = bbox.Center.Z;

        foreach (double x in stationXPositions)
        {
            // Tạo một mặt phẳng cắt lớn hơn tiết diện thân tàu tại vị trí x
            var plane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
            var cutterPlaneSurface = new PlaneSurface(
                plane,
                new Interval(midY - width / 2, midY + width / 2),
                new Interval(midZ - height / 2, midZ + height / 2)
            );
            var cutterBrep = cutterPlaneSurface.ToBrep();

            // Cắt lấy phần tiết diện nằm gọn bên trong thân tàu
            Curve[]? intCurves;
            Point3d[]? intPts;
            Intersection.BrepPlane(hullSolid, plane, tolerance, out intCurves, out intPts);

            if (intCurves != null && intCurves.Length > 0)
            {
                var joinedCurves = Curve.JoinCurves(intCurves, tolerance);
                if (joinedCurves != null && joinedCurves.Length > 0)
                {
                    var planarBreps = Brep.CreatePlanarBreps(joinedCurves, tolerance);
                    if (planarBreps != null && planarBreps.Length > 0)
                    {
                        bulkheads.AddRange(planarBreps);
                    }
                }
            }
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
    public static void BakeStructure(RhinoDoc doc, StructureResult result)
    {
        int rootIndex = LayerManager.GetOrCreateLayer(doc, "LMHull_Structure", Color.DarkSlateGray);
        Guid rootId = doc.Layers[rootIndex].Id;

        int bhLayer = GetOrCreateSubLayer(doc, "01_Bulkheads", Color.Cyan, rootId);
        int frameLayer = GetOrCreateSubLayer(doc, "02_FrameRibs", Color.Orange, rootId);

        foreach (var bh in result.Bulkheads)
        {
            doc.Objects.AddBrep(bh, new ObjectAttributes { LayerIndex = bhLayer });
        }

        foreach (var rib in result.FrameInnerCurves)
        {
            doc.Objects.AddCurve(rib, new ObjectAttributes { LayerIndex = frameLayer });
        }

        doc.Views.Redraw();
    }

    private static int GetOrCreateSubLayer(RhinoDoc doc, string layerName, Color color, Guid parentId)
    {
        var existingLayer = doc.Layers.FindName(layerName);
        if (existingLayer != null) return existingLayer.Index;

        var subLayer = new Layer
        {
            Name = layerName,
            Color = color,
            ParentLayerId = parentId
        };
        return doc.Layers.Add(subLayer);
    }
}