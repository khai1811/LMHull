using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Hydrostatics;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Hydrostatics.Calculations;
using LMHull.Rhino.Features.Hydrostatics.Models;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

/// <summary>
/// Đường cong thủy tĩnh theo dải mớn nước: bảng T, V, Δ, Aw, TPC, Cb, KB, LCB, LCF và (tùy chọn) đồ thị trong bản vẽ.
/// </summary>
public class LMHydroCurvesCommand : Command
{
    private static readonly CadLayer CurvesLayer = LayerManager.Results.Child("HydroCurves", Color.DarkOrange);

    public override string EnglishName => "LMHydroCurves";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn vỏ tàu và dải mớn nước
        var picked = RhinoInput.PickBrep("Chọn vỏ tàu cần dựng đường cong thủy tĩnh", out Brep hull);
        if (picked != Result.Success) return picked;

        double depth = hull.GetBoundingBox(true).Diagonal.Z;
        var got = RhinoInput.GetNumber("Mớn nước nhỏ nhất T min (m)", Math.Round(depth * 0.1, 2), 0.01, depth, out double tMin);
        if (got != Result.Success) return got;
        got = RhinoInput.GetNumber("Mớn nước lớn nhất T max (m)", Math.Round(depth * 0.8, 2), tMin, depth, out double tMax);
        if (got != Result.Success) return got;
        got = RhinoInput.GetNumber("Bước mớn nước (m)", Math.Max(0.01, Math.Round((tMax - tMin) / 10, 2)), 0.01, Math.Max(0.01, tMax - tMin), out double step);
        if (got != Result.Success) return got;

        var gotPlot = RhinoInput.GetOption("Vẽ đồ thị đường cong vào bản vẽ?", new[] { "Co_Ve", "Khong_Ve" }, out int plotOption);
        if (gotPlot != Result.Success) return gotPlot;

        // 2. Tính tại từng mớn nước
        var settings = ServiceRegistry.Settings;
        var input = new HydrostaticCurvesInput(hull, HydrostaticTable.Drafts(tMin, tMax, step), settings.SeaWaterDensity, doc.ModelAbsoluteTolerance, settings.ReferenceX);

        CalculationResult<HydrostaticTable> calcResult;
        using (var progress = new RhinoProgressReporter("Đường cong thủy tĩnh"))
        {
            calcResult = new HydrostaticCurvesCalculation().Run(input, progress);
        }

        if (!calcResult.IsSuccess || calcResult.Value == null || calcResult.Value.Rows.Count == 0)
        {
            ServiceRegistry.Logger.Error("[LMHydroCurves] " + (calcResult.Errors.Count > 0 ? calcResult.GetErrorMessage() : "Không tính được mớn nước nào."));
            return Result.Failure;
        }

        var table = calcResult.Value;

        // 3. Bảng kết quả
        var result = new ResultTable("Đường cong thủy tĩnh", "LMHydroCurves",
            "T (m)", "V (m³)", "Δ (t)", "Aw (m²)", "TPC (t/cm)", "Cb", "KB (m)", "LCB (m)", "LCF (m)");
        foreach (var r in table.Rows)
        {
            result.AddRow(F(r.Draft, 2), F(r.Volume, 2), F(r.Displacement, 2), F(r.WaterplaneArea, 2),
                F(HydrostaticTable.Tpc(r.WaterplaneArea, settings.SeaWaterDensity), 3), F(r.BlockCoefficient, 3),
                F(r.KB, 3), F(r.LCB, 3), F(r.LCF, 3));
        }
        result.Notes.Add($"Tỷ trọng nước {settings.SeaWaterDensity:F3} t/m³ – LCB/LCF đo từ X = {settings.ReferenceX:F2} m – thời gian tính {calcResult.Elapsed.TotalSeconds:F2} s");
        foreach (var log in calcResult.Logs) result.Notes.Add("Bỏ qua " + log);
        ResultsHub.Publish(result);

        // 4. Đồ thị: trục đứng là T, mỗi đường được co giãn để giá trị lớn nhất dài bằng nửa chiều dài tàu
        if (plotOption == 0) PlotCurves(RhinoCadHandle.For(doc), hull, table);

        return Result.Success;
    }

    private static void PlotCurves(ICadHandle cad, Brep hull, HydrostaticTable table)
    {
        var bbox = hull.GetBoundingBox(true);
        double originX = bbox.Max.X + bbox.Diagonal.X * 0.2;
        double width = bbox.Diagonal.X * 0.5;

        var curves = new (string Name, Func<HydrostaticRow, double> Value)[]
        {
            ("Δ (t)", r => r.Displacement),
            ("Aw (m²)", r => r.WaterplaneArea),
            ("KB (m)", r => r.KB),
            ("LCB (m)", r => r.LCB)
        };

        // Trục T
        cad.AddCurve(new LineCurve(new Point3d(originX, 0, bbox.Min.Z), new Point3d(originX, 0, bbox.Min.Z + table.Rows[^1].Draft)), CurvesLayer.Spec("Trục T"));

        foreach (var (name, value) in curves)
        {
            double max = table.Rows.Max(r => Math.Abs(value(r)));
            if (max <= 0) continue;

            var points = table.Rows.Select(r => new Point3d(originX + value(r) / max * width, 0, bbox.Min.Z + r.Draft)).ToList();
            if (points.Count < 2) continue;
            cad.AddCurve(new PolylineCurve(points), CurvesLayer.Spec($"{name} – max {max.ToString("F2", CultureInfo.InvariantCulture)}"));
        }

        cad.Redraw();
    }

    private static string F(double value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);
}
