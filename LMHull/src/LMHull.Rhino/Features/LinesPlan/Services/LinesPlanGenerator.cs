using System;
using System.Collections.Generic;
using System.Drawing;
using LMHull.Rhino.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.LinesPlan.Services;

public class LinesPlanResult
{
    public List<Curve> StationCurves { get; } = new();
    public List<Curve> WaterlineCurves { get; } = new();
    public List<Curve> ButtockCurves { get; } = new();
}

public static class LinesPlanGenerator
{
    /// <summary>
    /// Cắt vỏ tàu 3D thành hệ thống đường sườn, đường nước và đường mông.
    /// </summary>
    public static LinesPlanResult GenerateCurves(Brep hullBrep, int stationCount = 11, int waterlineCount = 5, int buttockCount = 3, double tolerance = 0.01)
    {
        var result = new LinesPlanResult();
        BoundingBox bbox = hullBrep.GetBoundingBox(true);

        double minX = bbox.Min.X, maxX = bbox.Max.X;
        double minY = bbox.Min.Y, maxY = bbox.Max.Y;
        double minZ = bbox.Min.Z, maxZ = bbox.Max.Z;

        // 1. CẮT HỆ ĐƯỜNG SƯỜN (Stations: X = const)
        double dx = (maxX - minX) / (stationCount - 1);
        for (int i = 0; i < stationCount; i++)
        {
            double x = minX + i * dx;
            var plane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
            if (Intersection.BrepPlane(hullBrep, plane, tolerance, out Curve[]? curves, out _) && curves != null)
            {
                result.StationCurves.AddRange(curves);
            }
        }

        // 2. CẮT HỆ ĐƯỜNG NƯỚC (Waterlines: Z = const)
        double dz = (maxZ - minZ) / (waterlineCount + 1);
        for (int i = 1; i <= waterlineCount; i++)
        {
            double z = minZ + i * dz;
            var plane = new Plane(new Point3d(0, 0, z), Vector3d.ZAxis);
            if (Intersection.BrepPlane(hullBrep, plane, tolerance, out Curve[]? curves, out _) && curves != null)
            {
                result.WaterlineCurves.AddRange(curves);
            }
        }

        // 3. CẮT HỆ ĐƯỜNG MÔNG (Buttocks: Y = const)
        double halfB = Math.Max(Math.Abs(minY), Math.Abs(maxY));
        double dy = halfB / (buttockCount + 1);
        for (int i = 1; i <= buttockCount; i++)
        {
            double y = i * dy;
            // Cắt cả 2 mạn trái và phải
            var planeRight = new Plane(new Point3d(0, -y, 0), Vector3d.YAxis);
            if (Intersection.BrepPlane(hullBrep, planeRight, tolerance, out Curve[]? curvesR, out _) && curvesR != null)
                result.ButtockCurves.AddRange(curvesR);

            var planeLeft = new Plane(new Point3d(0, y, 0), Vector3d.YAxis);
            if (Intersection.BrepPlane(hullBrep, planeLeft, tolerance, out Curve[]? curvesL, out _) && curvesL != null)
                result.ButtockCurves.AddRange(curvesL);
        }

        return result;
    }

    /// <summary>
    /// Đưa toàn bộ đường cong Lines Plan vào các layer phân cấp chuyên dụng.
    /// </summary>
    public static void BakeToDocument(RhinoDoc doc, LinesPlanResult linesPlan)
    {
        // 1. Tạo layer cha
        int rootIndex = LayerManager.GetOrCreateLayer(doc, "LMHull_LinesPlan", Color.DarkGray);
        Guid rootId = doc.Layers[rootIndex].Id;

        // 2. Tạo các layer con
        int stationLayer = GetOrCreateSubLayer(doc, "01_Stations", Color.Red, rootId);
        int wlLayer = GetOrCreateSubLayer(doc, "02_Waterlines", Color.DodgerBlue, rootId);
        int buttockLayer = GetOrCreateSubLayer(doc, "03_Buttocks", Color.Green, rootId);

        // 3. Thêm các đường cong vào RhinoDoc
        foreach (var crv in linesPlan.StationCurves)
            doc.Objects.AddCurve(crv, new ObjectAttributes { LayerIndex = stationLayer });

        foreach (var crv in linesPlan.WaterlineCurves)
            doc.Objects.AddCurve(crv, new ObjectAttributes { LayerIndex = wlLayer });

        foreach (var crv in linesPlan.ButtockCurves)
            doc.Objects.AddCurve(crv, new ObjectAttributes { LayerIndex = buttockLayer });

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