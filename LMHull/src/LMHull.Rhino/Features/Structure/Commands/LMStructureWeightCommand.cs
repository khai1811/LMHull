using System.Globalization;
using LMHull.Core.Calculation;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.Features.Structure.Commands;

/// <summary>
/// Tổng trọng lượng và trọng tâm (CoG) kết cấu trong bản vẽ, kèm bảng chi tiết theo layer.
/// </summary>
public class LMStructureWeightCommand : Command
{
    public override string EnglishName => "LMStructureWeight";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var settings = ServiceRegistry.Settings;
        var input = new WeightInput(RhinoCadHandle.For(doc), settings.DefaultMaterial, settings.Limits.IsActive ? settings.Limits : null);

        CalculationResult<WeightResult> calcResult;
        using (var progress = new RhinoProgressReporter("Trọng lượng kết cấu"))
        {
            calcResult = new WeightEstimationCalculation().Run(input, progress);
        }

        if (!calcResult.IsSuccess || calcResult.Value == null)
        {
            ServiceRegistry.Logger.Error("[LMStructureWeight] " + calcResult.GetErrorMessage());
            return Result.Failure;
        }

        var weight = calcResult.Value;
        if (weight.ComputedPartCount == 0)
        {
            ServiceRegistry.Logger.Warn("[LMStructureWeight] Không tìm thấy chi tiết kết cấu nào (layer LMHull_Structure / SolidPlates / Stiffeners / Hull).");
            return Result.Nothing;
        }

        var summary = ResultTable.KeyValue("Trọng lượng kết cấu", "LMStructureWeight")
            .AddRow("Số chi tiết", weight.ComputedPartCount.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Tổng trọng lượng", weight.TotalWeightTonnes, "F3", "t")
            .AddRow("LCG (X)", weight.CenterOfGravity.X - settings.ReferenceX, "F3", "m")
            .AddRow("TCG (Y)", weight.CenterOfGravity.Y - settings.ReferenceY, "F3", "m")
            .AddRow("VCG (Z)", weight.CenterOfGravity.Z - settings.ReferenceZ, "F3", "m")
            .AddRow("Thời gian tính", calcResult.Elapsed.TotalSeconds, "F2", "s");
        summary.Notes.Add($"Trọng tâm đo từ gốc tham chiếu ({settings.ReferenceX:F2}, {settings.ReferenceY:F2}, {settings.ReferenceZ:F2}) m");
        string limits = input.Limits?.Describe() ?? string.Empty;
        if (limits.Length > 0) summary.Notes.Add(limits);
        ResultsHub.Publish(summary);

        var byLayer = new ResultTable("Trọng lượng theo layer", "LMStructureWeight", "Layer", "Số chi tiết", "Trọng lượng (t)", "Tỷ lệ (%)");
        foreach (var layer in weight.ByLayer)
        {
            byLayer.AddRow(layer.Layer,
                layer.Count.ToString(CultureInfo.InvariantCulture),
                layer.Tonnes.ToString("F3", CultureInfo.InvariantCulture),
                (layer.Tonnes / weight.TotalWeightTonnes * 100).ToString("F1", CultureInfo.InvariantCulture));
        }
        ResultsHub.Publish(byLayer);

        return Result.Success;
    }
}
