using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

/// <summary>
/// Tổng hợp và làm nổi bật hình học liên kết với một node (node nhóm: gom từ các node lá).
/// </summary>
public static class StructureSelectionService
{
    public static IEnumerable<StructuralNode> LinkedLeaves(StructuralNode node) =>
        (node.IsLeaf ? new[] { node } : node.Leaves()).Where(n => n.HasGeometry);

    /// <summary>
    /// Trọng tâm có trọng số theo trọng lượng các chi tiết đã dựng 3D; null nếu chưa có chi tiết nào.
    /// </summary>
    public static Point3d? ComputeCenterOfGravity(ICadHandle cad, StructuralNode node)
    {
        var aggregator = new WeightAggregator();

        foreach (var leaf in LinkedLeaves(node).Where(l => l.WeightTonnes > 0))
        {
            if (cad.GetGeometry<Brep>(leaf.RhinoGeometryId) is not Brep brep) continue;
            var vmp = VolumeMassProperties.Compute(brep);
            if (vmp == null) continue;

            aggregator.Add(leaf.WeightTonnes, vmp.Centroid.X, vmp.Centroid.Y, vmp.Centroid.Z);
        }

        return aggregator.CenterOfGravity is var (x, y, z) ? new Point3d(x, y, z) : null;
    }

    public static void Highlight(ICadHandle cad, StructuralNode node)
    {
        var ids = LinkedLeaves(node).Select(n => n.RhinoGeometryId).ToList();
        if (ids.Count == 0) return;

        cad.UnselectAll();
        cad.Select(ids);
        cad.Redraw();
    }
}
