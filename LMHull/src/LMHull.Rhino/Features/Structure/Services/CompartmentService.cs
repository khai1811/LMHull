using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LMHull.Rhino.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public class CompartmentInfo
{
    public string Name { get; set; } = string.Empty;
    public Brep Geometry { get; set; } = null!;
    public double Volume { get; set; }
    public Point3d CenterOfGravity { get; set; }
}

public static class CompartmentService
{
    private static readonly Color[] TankColors = new[]
    {
        Color.FromArgb(100, 149, 237), // Cornflower Blue
        Color.FromArgb(60, 179, 113),  // Medium Sea Green
        Color.FromArgb(255, 165, 0),   // Orange
        Color.FromArgb(186, 85, 211),  // Medium Orchid
        Color.FromArgb(240, 128, 128), // Light Coral
        Color.FromArgb(72, 209, 204)   // Medium Turquoise
    };

    /// <summary>
    /// Cắt thân tàu kín bằng hệ thống mặt phẳng vách ngang thành các khoang độc lập.
    /// </summary>
    public static List<CompartmentInfo> SplitIntoCompartments(Brep hullSolid, double[] stationXPositions, double tolerance = 0.01)
    {
        var compartments = new List<CompartmentInfo>();
        if (!hullSolid.IsSolid) return compartments;

        BoundingBox bbox = hullSolid.GetBoundingBox(true);
        double width = (bbox.Max.Y - bbox.Min.Y) * 2.0;
        double height = (bbox.Max.Z - bbox.Min.Z) * 2.0;

        // Tạo danh sách các mặt phẳng cắt vô hạn dạng Brep
        var cutterBreps = new List<Brep>();
        foreach (double x in stationXPositions.OrderBy(x => x))
        {
            var plane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
            var planeSrf = new PlaneSurface(
                plane,
                new Interval(bbox.Center.Y - width / 2, bbox.Center.Y + width / 2),
                new Interval(bbox.Center.Z - height / 2, bbox.Center.Z + height / 2)
            );
            cutterBreps.Add(planeSrf.ToBrep());
        }

        // Thực hiện cắt Brep thân tàu
        Brep[] splitPieces = hullSolid.Split(cutterBreps, tolerance);
        if (splitPieces == null || splitPieces.Length == 0)
        {
            splitPieces = new[] { hullSolid };
        }

        // Sắp xếp các khoang theo chiều dọc từ đuôi tàu (X nhỏ) đến mũi tàu (X lớn)
        var orderedPieces = splitPieces
            .Select(p => p.CapPlanarHoles(tolerance) ?? p)
            .OrderBy(p => p.GetBoundingBox(true).Center.X)
            .ToList();

        for (int i = 0; i < orderedPieces.Count; i++)
        {
            Brep piece = orderedPieces[i];
            var volProps = VolumeMassProperties.Compute(piece);

            double vol = volProps?.Volume ?? 0.0;
            Point3d cg = volProps?.Centroid ?? piece.GetBoundingBox(true).Center;

            string name;
            if (i == 0)
                name = "AftPeak_Tank";
            else if (i == orderedPieces.Count - 1)
                name = "ForePeak_Tank";
            else
                name = $"Hold_Tank_{i:D2}";

            compartments.Add(new CompartmentInfo
            {
                Name = name,
                Geometry = piece,
                Volume = vol,
                CenterOfGravity = cg
            });
        }

        return compartments;
    }

    /// <summary>
    /// Ghi các khoang vào RhinoDoc theo cấu trúc Layer phân tầng.
    /// </summary>
    public static void BakeCompartments(RhinoDoc doc, List<CompartmentInfo> compartments)
    {
        int rootIndex = LayerManager.GetOrCreateLayer(doc, "LMHull_Compartments", Color.Gray);
        Guid rootId = doc.Layers[rootIndex].Id;

        for (int i = 0; i < compartments.Count; i++)
        {
            var comp = compartments[i];
            Color color = TankColors[i % TankColors.Length];

            int layerIndex = GetOrCreateSubLayer(doc, comp.Name, color, rootId);

            var attr = new ObjectAttributes
            {
                LayerIndex = layerIndex,
                Name = comp.Name
            };

            doc.Objects.AddBrep(comp.Geometry, attr);
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