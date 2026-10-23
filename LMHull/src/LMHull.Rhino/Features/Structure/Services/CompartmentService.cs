using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

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
    /// Cắt thân tàu kín thành các khoang; trả về danh sách rỗng nếu không cắt được.
    /// </summary>
    public static List<CompartmentInfo> SplitIntoCompartments(Brep hullSolid, double[] stationXPositions, double tolerance = 0.01)
    {
        return new CompartmentCalculation().Run(new CompartmentInput(hullSolid, stationXPositions, tolerance)).Value ?? new List<CompartmentInfo>();
    }

    /// <summary>
    /// Ghi các khoang vào RhinoDoc theo cấu trúc Layer phân tầng.
    /// </summary>
    public static void BakeCompartments(ICadHandle cad, List<CompartmentInfo> compartments)
    {
        for (int i = 0; i < compartments.Count; i++)
        {
            var comp = compartments[i];
            var layer = StructureLayers.Compartments.Child(comp.Name, TankColors[i % TankColors.Length]);
            cad.AddBrep(comp.Geometry, layer.Spec(comp.Name));
        }

        cad.Redraw();
    }
}
