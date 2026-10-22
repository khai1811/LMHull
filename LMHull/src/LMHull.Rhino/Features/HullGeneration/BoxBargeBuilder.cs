using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

public static class BoxBargeBuilder
{
    /// <summary>
    /// Dựng khối sà lan hình hộp Brep trong bộ nhớ RAM theo quy ước tọa độ tàu thủy:
    /// - X: Từ 0 (AP) đến Length (FP)
    /// - Y: Từ -Beam/2 đến +Beam/2 (đối xứng qua tâm tàu)
    /// - Z: Từ 0 (Đáy tàu BaseLine) đến Depth (Mặt boong)
    /// </summary>
    public static Brep Build(ShipInput input)
    {
        double L = input.Length;
        double B = input.Beam;
        double D = input.Depth ?? (input.Draft * 1.4); // Nếu không nhập D, giả định D = 1.4 * T
        var minPoint = new Point3d(0, -B / 2.0, 0);
        var maxPoint = new Point3d(L, B / 2.0, D);

        var box = new BoundingBox(minPoint, maxPoint);
        return box.ToBrep();
    }
}