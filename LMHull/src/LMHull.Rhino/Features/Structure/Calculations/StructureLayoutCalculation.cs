using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.LinesPlan.Calculations;
using LMHull.Rhino.Features.LinesPlan.Models;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Features.Structure.Services;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Calculations;

/// <summary>
/// Bố trí vách ngang kín nước cách đều và sườn gia cường bên trong thân tàu kín.
/// </summary>
public class StructureLayoutCalculation : CalculationBase<StructureLayoutInput, StructureResult>
{
    public override string Name => "StructureLayout";

    protected override ValidationResult Validate(StructureLayoutInput input)
    {
        var validation = new ValidationResult();
        if (input.Hull == null || !input.Hull.IsSolid) validation.AddError("Thân tàu phải là khối kín (Solid Brep) để cắt vách chính xác.");
        if (input.BulkheadCount < 1 || input.BulkheadCount > 20) validation.AddError("Số vách ngang phải trong khoảng 1–20.");
        if (input.FrameDepth <= 0) validation.AddError("Chiều sâu bản thành sườn phải lớn hơn 0.");
        return validation;
    }

    protected override StructureResult Execute(StructureLayoutInput input, CalculationContext context)
    {
        var result = new StructureResult();
        BoundingBox bbox = input.Hull.GetBoundingBox(true);
        double step = (bbox.Max.X - bbox.Min.X) / (input.BulkheadCount + 1);

        // 1. Cắt tiết diện vách ngang tại các vị trí cách đều (0–50%)
        for (int i = 0; i < input.BulkheadCount; i++)
        {
            context.ThrowIfCancellationRequested();
            double x = bbox.Min.X + (i + 1) * step;
            result.Bulkheads.AddRange(HullSectionService.CreatePlanarSections(input.Hull, HullSectionService.TransversePlaneAt(x), input.Tolerance));
            context.ReportStep(i, input.BulkheadCount, $"Cắt vách ngang {i + 1}/{input.BulkheadCount}", 0, 50);
        }

        // 2. Trích xuất đường sườn (50–80%)
        context.ThrowIfCancellationRequested();
        var linesInput = new LinesPlanInput(input.Hull, input.FrameStationCount, 0, 0, input.Tolerance);
        var lines = new LinesPlanCalculation().Run(linesInput, cancellationToken: context.CancellationToken);
        context.ThrowIfCancellationRequested();
        if (!lines.IsSuccess || lines.Value == null) throw new System.InvalidOperationException(lines.GetErrorMessage());
        context.Report(80, "Đã trích xuất đường sườn.");

        // 3. Offset sườn vào trong lòng vỏ thành bản thành khung sườn (80–100%)
        result.FrameInnerCurves.AddRange(InternalStructureGenerator.CreateFrameOffsets(lines.Value.StationCurves, input.FrameDepth, input.Tolerance));
        return result;
    }
}
