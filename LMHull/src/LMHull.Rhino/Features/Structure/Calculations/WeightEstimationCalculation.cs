using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Services;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Features.Structure.Services;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Calculations;

public class WeightEstimationCalculation : CalculationBase<WeightInput, WeightResult>
{
    // Quét tất cả các layer kết cấu và vỏ tàu
    private static readonly string[] TargetLayerPrefixes = { "LMHull_Structure", "LMHull_SolidPlates", "LMHull_Stiffeners", "Hull" };

    public override string Name => "WeightEstimation";

    protected override ValidationResult Validate(WeightInput input)
    {
        var validation = new ValidationResult();
        if (input.Cad == null) validation.AddError("Không có tài liệu Rhino đang mở.");
        return validation;
    }

    protected override WeightResult Execute(WeightInput input, CalculationContext context)
    {
        var defaultMat = MaterialRegistry.FindOrDefault(input.DefaultMaterialName);
        var total = new WeightAggregator();
        var byLayer = new Dictionary<string, WeightAggregator>();

        var structuralObjects = input.Cad.FindBreps()
            .Where(o => TargetLayerPrefixes.Any(prefix => o.LayerPath.Contains(prefix)))
            .ToList();

        for (int i = 0; i < structuralObjects.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            var obj = structuralObjects[i];
            context.ReportStep(i, structuralObjects.Count, $"Tính khối lượng chi tiết {i + 1}/{structuralObjects.Count}");

            if (obj.Geometry is not Brep brep) continue;

            var vmp = VolumeMassProperties.Compute(brep);
            if (vmp == null || vmp.Volume <= 0.0001) continue;

            Point3d cg = vmp.Centroid;
            if (input.Limits != null && !input.Limits.Contains(cg.X, cg.Y, cg.Z)) continue;

            // Lấy vật liệu thực tế lưu trên thuộc tính vật thể nếu có
            string matName = obj.Meta.TryGetValue(StructureObjectData.MaterialKey, out object? m) && m is string name ? name : input.DefaultMaterialName;
            double partWeight = vmp.Volume * (MaterialRegistry.Find(matName) ?? defaultMat).DensityTonPerM3;

            total.Add(partWeight, cg.X, cg.Y, cg.Z);
            if (!byLayer.TryGetValue(obj.LayerPath, out var layerAgg)) byLayer[obj.LayerPath] = layerAgg = new WeightAggregator();
            layerAgg.Add(partWeight, cg.X, cg.Y, cg.Z);
        }

        var result = new WeightResult
        {
            ComputedPartCount = total.Count,
            TotalWeightTonnes = total.TotalWeight,
            ByLayer = byLayer
                .Select(kv => new LayerWeight(kv.Key, kv.Value.Count, kv.Value.TotalWeight))
                .OrderByDescending(l => l.Tonnes)
                .ToList()
        };
        if (total.CenterOfGravity is var (x, y, z)) result.CenterOfGravity = new Point3d(x, y, z);

        return result;
    }
}
