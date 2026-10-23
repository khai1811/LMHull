using System.Collections.Generic;
using LMHull.Core.Calculation;
using LMHull.Core.Hydrostatics;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Hydrostatics.Services;
using LMHull.Rhino.Features.Structure.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Calculations;

/// <summary>
/// Bảng dung tích két: cắt két kín bằng mặt phẳng ngang tại từng mức đo, lấy thể tích và VCG chất lỏng.
/// </summary>
public class TankSoundingCalculation : CalculationBase<TankSoundingInput, SoundingTable>
{
    public override string Name => "TankSounding";

    protected override ValidationResult Validate(TankSoundingInput input)
    {
        var validation = new ValidationResult();
        if (input.Tank == null || !input.Tank.IsSolid) validation.AddError("Két phải là khối kín (Solid Brep).");
        if (input.Step <= 0) validation.AddError("Bước đo phải lớn hơn 0.");
        else if (input.Tank != null && input.Tank.GetBoundingBox(true).Diagonal.Z / input.Step > 500)
            validation.AddError("Quá nhiều mức đo (tối đa 500) — hãy tăng bước đo.");
        return validation;
    }

    protected override SoundingTable Execute(TankSoundingInput input, CalculationContext context)
    {
        BoundingBox bbox = input.Tank.GetBoundingBox(true);
        double bottom = bbox.Min.Z;
        double height = bbox.Max.Z - bottom;

        var full = VolumeMassProperties.Compute(input.Tank);
        double capacity = full?.Volume ?? 0.0;
        if (capacity <= 0) throw new System.InvalidOperationException("Không tính được thể tích két.");

        var levels = Steps.Range(0, height, input.Step);
        var rows = new List<SoundingRow>();

        for (int i = 0; i < levels.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            double sounding = levels[i];

            double volume;
            double vcg;
            if (sounding <= 0)
            {
                (volume, vcg) = (0.0, 0.0);
            }
            else if (sounding >= height)
            {
                (volume, vcg) = (capacity, full!.Centroid.Z - bottom);
            }
            else if (BrepSlicer.VolumeBelow(input.Tank, bottom + sounding, input.Tolerance) is var (v, c))
            {
                (volume, vcg) = (v, c.Z - bottom);
            }
            else
            {
                context.Log($"Không cắt được két tại mức {sounding:F2} m");
                context.ReportStep(i, levels.Count, $"Mức đo {sounding:F2} m");
                continue;
            }

            rows.Add(new SoundingRow(sounding, volume, volume / capacity * 100.0, vcg));
            context.ReportStep(i, levels.Count, $"Mức đo {sounding:F2} m ({i + 1}/{levels.Count})");
        }

        return new SoundingTable(rows);
    }
}
