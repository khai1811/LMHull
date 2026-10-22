using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

/// <summary>
/// Căng các mặt NURBS Surface / Brep từ tập hợp các đường sườn bằng thuật toán Loft.
/// </summary>
public static class NurbsHullLofter
{
    /// <summary>
    /// Loft danh sách các đường sườn thành mặt vỏ tàu mở (Open Brep).
    /// </summary>
    public static Brep? CreateLoftedHull(List<Curve> stationCurves)
    {
        if (stationCurves == null || stationCurves.Count < 2)
            return null;

        // Tạo Loft chuẩn kiểu thông thường (LoftType.Normal)
        Brep[] breps = Brep.CreateFromLoft(
            curves: stationCurves,
            start: Point3d.Unset,
            end: Point3d.Unset,
            loftType: LoftType.Normal,
            closed: false
        );

        return (breps != null && breps.Length > 0) ? breps[0] : null;
    }
}