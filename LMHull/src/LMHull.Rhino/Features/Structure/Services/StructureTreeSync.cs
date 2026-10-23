using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

/// <summary>
/// Đồng bộ liên kết hình học của cây kết cấu với tài liệu CAD.
/// </summary>
public static class StructureTreeSync
{
    /// <summary>
    /// Gỡ liên kết các node có đối tượng đã bị xóa, và khôi phục liên kết từ metadata LMHull.NodeId
    /// (ví dụ sau khi mở lại file hoặc Undo). Trả về các node đã thay đổi.
    /// </summary>
    public static List<StructuralNode> Sync(ICadHandle cad, StructuralNode root)
    {
        var changed = new List<StructuralNode>();
        var nodesById = root.Descendants().ToDictionary(n => n.Id);

        // 1. Gỡ liên kết tới đối tượng không còn tồn tại
        foreach (var node in nodesById.Values.Where(n => n.HasGeometry && !cad.Exists(n.RhinoGeometryId)))
        {
            node.UnlinkGeometry();
            changed.Add(node);
        }

        // 2. Khôi phục liên kết từ metadata trên đối tượng
        foreach (var obj in cad.FindBreps())
        {
            Guid nodeId = StructureObjectData.ReadNodeId(obj);
            if (nodeId == Guid.Empty || !nodesById.TryGetValue(nodeId, out var node)) continue;
            if (node.RhinoGeometryId == obj.Id || obj.Geometry is not Brep brep) continue;

            node.Material = StructureObjectData.ReadMaterial(obj);
            node.ThicknessMm = StructureObjectData.ReadThicknessMm(obj);
            node.LinkGeometry(obj.Id, StructureObjectData.ComputeWeightTonnes(brep, node.Material));
            changed.Add(node);
        }

        return changed;
    }
}
