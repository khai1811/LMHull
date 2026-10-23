using System;
using System.Drawing;
using LMHull.Rhino.Features.LinesPlan.Calculations;
using LMHull.Rhino.Features.LinesPlan.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.LinesPlan.Services;

public static class LinesPlanGenerator
{
    // Cây layer chuyên dụng của Lines Plan
    public static readonly CadLayer Root = new("LMHull_LinesPlan", Color.DarkGray);
    public static readonly CadLayer Stations = Root.Child("01_Stations", Color.Red);
    public static readonly CadLayer Waterlines = Root.Child("02_Waterlines", Color.DodgerBlue);
    public static readonly CadLayer Buttocks = Root.Child("03_Buttocks", Color.Green);

    /// <summary>
    /// Cắt vỏ tàu 3D thành hệ thống đường sườn, đường nước và đường mông.
    /// </summary>
    public static LinesPlanResult GenerateCurves(Brep hullBrep, int stationCount = 11, int waterlineCount = 5, int buttockCount = 3, double tolerance = 0.01)
    {
        var input = new LinesPlanInput(hullBrep, stationCount, waterlineCount, buttockCount, tolerance);
        return new LinesPlanCalculation().Run(input).Value ?? new LinesPlanResult();
    }

    /// <summary>
    /// Đưa toàn bộ đường cong Lines Plan vào các layer phân cấp chuyên dụng.
    /// </summary>
    public static void BakeToDocument(ICadHandle cad, LinesPlanResult linesPlan)
    {
        foreach (var crv in linesPlan.StationCurves)
            cad.AddCurve(crv, Stations.Spec());

        foreach (var crv in linesPlan.WaterlineCurves)
            cad.AddCurve(crv, Waterlines.Spec());

        foreach (var crv in linesPlan.ButtockCurves)
            cad.AddCurve(crv, Buttocks.Spec());

        cad.Redraw();
    }
}
