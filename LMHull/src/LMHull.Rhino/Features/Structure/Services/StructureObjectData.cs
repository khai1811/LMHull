using System;
using System.Collections.Generic;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

/// <summary>
/// Metadata kết cấu gắn trên đối tượng CAD: liên kết node, vật liệu, bề dày.
/// </summary>
public static class StructureObjectData
{
    public const string NodeIdKey = "LMHull.NodeId";
    public const string MaterialKey = "Material";
    public const string ThicknessKey = "ThicknessMm";

    public const string DefaultMaterial = "Steel AH36";
    public const double DefaultThicknessMm = 10.0;

    public static IReadOnlyDictionary<string, object> Meta(StructuralNode node) => new Dictionary<string, object>
    {
        [NodeIdKey] = node.Id,
        [MaterialKey] = node.Material,
        [ThicknessKey] = node.ThicknessMm
    };

    public static Guid ReadNodeId(CadObject obj) =>
        obj.Meta.TryGetValue(NodeIdKey, out object? v) && v is Guid id ? id : Guid.Empty;

    public static string ReadMaterial(CadObject obj) =>
        obj.Meta.TryGetValue(MaterialKey, out object? v) && v is string s ? s : DefaultMaterial;

    public static double ReadThicknessMm(CadObject obj) =>
        obj.Meta.TryGetValue(ThicknessKey, out object? v) && v is double d ? d : DefaultThicknessMm;

    /// <summary>
    /// Trọng lượng (tấn) của Brep theo khối lượng riêng vật liệu; 0 nếu không tính được thể tích.
    /// </summary>
    public static double ComputeWeightTonnes(Brep brep, string materialName)
    {
        var vmp = VolumeMassProperties.Compute(brep);
        return (vmp?.Volume ?? 0.0) * MaterialRegistry.FindOrDefault(materialName).DensityTonPerM3;
    }
}
