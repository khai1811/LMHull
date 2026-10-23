using System;
using System.Collections.Generic;
using LMHull.Core.Calculation;
using LMHull.Core.Hydrostatics;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Hydrostatics.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.Calculations;

/// <summary>
/// Đường cong thủy tĩnh: chạy HydrostaticCalculation tại từng mớn nước, báo tiến độ theo mớn và cho phép hủy.
/// </summary>
public class HydrostaticCurvesCalculation : CalculationBase<HydrostaticCurvesInput, HydrostaticTable>
{
    public override string Name => "HydrostaticCurves";

    protected override ValidationResult Validate(HydrostaticCurvesInput input)
    {
        var validation = new ValidationResult();
        if (input.Hull == null) validation.AddError("Chưa có hình học vỏ tàu (Brep).");
        if (input.Drafts == null || input.Drafts.Count == 0) validation.AddError("Chưa có mớn nước nào để tính.");
        else if (input.Drafts.Count > 200) validation.AddError("Quá nhiều mớn nước (tối đa 200) — hãy tăng bước.");
        if (input.WaterDensity <= 0) validation.AddError("Tỷ trọng nước phải lớn hơn 0.");
        return validation;
    }

    protected override HydrostaticTable Execute(HydrostaticCurvesInput input, CalculationContext context)
    {
        double baseZ = input.Hull.GetBoundingBox(true).Min.Z;
        var rows = new List<HydrostaticRow>();

        for (int i = 0; i < input.Drafts.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            double draft = input.Drafts[i];

            var single = new HydrostaticCalculation().Run(
                new HydrostaticInput(input.Hull, draft, input.WaterDensity, input.Tolerance),
                cancellationToken: context.CancellationToken);

            if (single.Status == CalculationStatus.Cancelled) throw new OperationCanceledException();
            if (single.IsSuccess && single.Value is DetailedHydroResult h)
            {
                rows.Add(new HydrostaticRow(
                    draft,
                    h.SubmergedVolume,
                    h.Displacement,
                    h.WaterplaneArea,
                    h.BlockCoefficient,
                    h.CenterOfBuoyancy.Z - baseZ,
                    h.CenterOfBuoyancy.X - input.ReferenceX,
                    h.CenterOfFlotation.X - input.ReferenceX));
            }
            else
            {
                context.Log($"T = {draft:F2} m: {single.GetErrorMessage()}");
            }

            context.ReportStep(i, input.Drafts.Count, $"Mớn nước T = {draft:F2} m ({i + 1}/{input.Drafts.Count})");
        }

        return new HydrostaticTable(rows);
    }
}
