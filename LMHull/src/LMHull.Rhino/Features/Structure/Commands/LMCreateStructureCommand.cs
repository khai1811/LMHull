using System.Globalization;
using LMHull.Core.Calculation;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMCreateStructureCommand : Command
{
    public override string EnglishName => "LMCreateStructure";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn thân tàu kín
        var picked = RhinoInput.PickBrep("Chọn thân tàu kín (Solid Brep) để bố trí vách và khung sườn", out Brep hull, requireSolid: true);
        if (picked != Result.Success) return picked;

        // 2. Nhập số lượng vách ngang
        var gotNum = RhinoInput.GetInteger("Nhập số lượng vách ngang kín nước (Bulkheads)", 4, 1, 20, out int bhCount);
        if (gotNum != Result.Success) return gotNum;

        // 3. Cắt vách và tạo bản thành khung sườn (có thanh tiến độ)
        var input = new StructureLayoutInput(hull, bhCount, FrameStationCount: 9, FrameDepth: 0.35, doc.ModelAbsoluteTolerance);
        CalculationResult<StructureResult> calcResult;
        using (var progress = new RhinoProgressReporter("Bố trí kết cấu"))
        {
            calcResult = new StructureLayoutCalculation().Run(input, progress);
        }

        if (!calcResult.IsSuccess || calcResult.Value == null)
        {
            ServiceRegistry.Logger.Error("[LMCreateStructure] " + calcResult.GetErrorMessage());
            return Result.Failure;
        }

        var structResult = calcResult.Value;
        InternalStructureGenerator.BakeStructure(RhinoCadHandle.For(doc), structResult);

        var table = ResultTable.KeyValue("Bố trí kết cấu", "LMCreateStructure")
            .AddRow("Số lượng vách ngăn (Bulkheads)", structResult.Bulkheads.Count.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Số đường sườn gia cường (Ribs)", structResult.FrameInnerCurves.Count.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Layer", "LMHull_Structure", "")
            .AddRow("Thời gian tính", calcResult.Elapsed.TotalSeconds, "F2", "s");
        ResultsHub.Publish(table);

        return Result.Success;
    }
}
