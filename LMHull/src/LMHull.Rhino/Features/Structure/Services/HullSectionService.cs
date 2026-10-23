using System;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Structure.Services;

public static class HullSectionService
{
    /// <summary>
    /// Cắt thân tàu kín bằng một mặt phẳng và dựng mặt phẳng tiết diện khép kín từ giao tuyến.
    /// </summary>
    public static Brep[] CreatePlanarSections(Brep hullSolid, Plane plane, double tolerance)
    {
        Intersection.BrepPlane(hullSolid, plane, tolerance, out Curve[]? intCurves, out Point3d[]? _);
        if (intCurves == null || intCurves.Length == 0) return Array.Empty<Brep>();

        var joinedCurves = Curve.JoinCurves(intCurves, tolerance);
        if (joinedCurves == null || joinedCurves.Length == 0) return Array.Empty<Brep>();

        return Brep.CreatePlanarBreps(joinedCurves, tolerance) ?? Array.Empty<Brep>();
    }

    /// <summary>
    /// Mặt phẳng cắt ngang (vách) tại tọa độ X.
    /// </summary>
    public static Plane TransversePlaneAt(double x) => new(new Point3d(x, 0, 0), Vector3d.XAxis);

    /// <summary>
    /// Mặt phẳng dọc (vách dọc) tại tọa độ Y.
    /// </summary>
    public static Plane LongitudinalPlaneAt(double y) => new(new Point3d(0, y, 0), Vector3d.YAxis);

    /// <summary>
    /// Mặt phẳng nằm ngang (boong) tại cao độ Z.
    /// </summary>
    public static Plane HorizontalPlaneAt(double z) => new(new Point3d(0, 0, z), Vector3d.ZAxis);
}
