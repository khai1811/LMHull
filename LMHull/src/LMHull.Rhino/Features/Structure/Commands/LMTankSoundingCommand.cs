using System;
using System.Globalization;
using LMHull.Core.Calculation;
using LMHull.Core.Hydrostatics;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Infrastructure;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Commands;

/// <summary>
/// Bảng dung tích két theo chiều cao (Sounding | V | % | VCG) cho một khoang kín.
/// </summary>
public class LMTankSoundingCommand : Command
{
    public override string EnglishName => "LMTankSounding";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn két (ví dụ khoang trên layer LMHull_Compartments) và bước đo
        var picked = RhinoInput.PickBrep("Chọn két / khoang kín cần lập bảng dung tích", out Brep tank, out ObjRef objRef, requireSolid: true);
        if (picked != Result.Success) return picked;

        double height = tank.GetBoundingBox(true).Diagonal.Z;
        var got = RhinoInput.GetNumber("Bước đo chiều cao (m)", Math.Max(0.01, Math.Round(height / 20, 2)), 0.01, Math.Max(0.01, height), out double step);
        if (got != Result.Success) return got;

        // 2. Tính
        CalculationResult<SoundingTable> calcResult;
        using (var progress = new RhinoProgressReporter("Bảng dung tích két"))
        {
            calcResult = new TankSoundingCalculation().Run(new TankSoundingInput(tank, step, doc.ModelAbsoluteTolerance), progress);
        }

        if (!calcResult.IsSuccess || calcResult.Value == null || calcResult.Value.Rows.Count == 0)
        {
            ServiceRegistry.Logger.Error("[LMTankSounding] " + (calcResult.Errors.Count > 0 ? calcResult.GetErrorMessage() : "Không tính được mức đo nào."));
            return Result.Failure;
        }

        // 3. Bảng kết quả
        var sounding = calcResult.Value;
        string tankName = objRef.Object()?.Attributes.Name is string n && n.Length > 0 ? n : "Két";
        var table = new ResultTable($"Bảng dung tích – {tankName}", "LMTankSounding", "Sounding (m)", "V (m³)", "%", "VCG (m)");
        foreach (var row in sounding.Rows)
        {
            table.AddRow(F(row.Sounding, 2), F(row.Volume, 3), F(row.Percent, 1), F(row.VCG, 3));
        }
        table.Notes.Add($"Dung tích toàn két: {sounding.Capacity:F3} m³ – chiều cao {height:F2} m – thời gian tính {calcResult.Elapsed.TotalSeconds:F2} s");
        foreach (var log in calcResult.Logs) table.Notes.Add(log);
        ResultsHub.Publish(table);

        return Result.Success;
    }

    private static string F(double value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);
}
