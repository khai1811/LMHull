using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class StiffenerAndCutoutService
{
    /// <summary>
    /// Sinh nẹp gia cường dạng Thép dẹt (Flat Bar) dọc theo một đường dẫn (Guide Curve).
    /// </summary>
    public static Brep? CreateFlatBarStiffener(Curve guideCurve, double webHeightMm = 150.0, double webThicknessMm = 10.0, double tolerance = 0.01)
    {
        if (guideCurve == null) return null;

        double h = webHeightMm / 1000.0;
        double t = webThicknessMm / 1000.0;

        // Xác định mặt phẳng tại điểm đầu đường cong
        Point3d startPt = guideCurve.PointAtStart;
        Vector3d tangent = guideCurve.TangentAtStart;

        Vector3d up = Vector3d.ZAxis;
        if (Math.Abs(Vector3d.Multiply(tangent, up)) > 0.9)
        {
            up = Vector3d.YAxis;
        }

        Vector3d normal = Vector3d.CrossProduct(tangent, up);
        normal.Unitize();
        up = Vector3d.CrossProduct(normal, tangent);
        up.Unitize();

        // Tạo tiết diện hình chữ nhật
        var p0 = startPt - normal * (t / 2.0);
        var p1 = startPt + normal * (t / 2.0);
        var p2 = p1 + up * h;
        var p3 = p0 + up * h;

        var profilePolyline = new Polyline(new[] { p0, p1, p2, p3, p0 });
        var profileCurve = profilePolyline.ToNurbsCurve();

        // Quét tiết diện dọc đường dẫn (Sweep 1 rail)
        var sweeps = Brep.CreateFromSweep(guideCurve, profileCurve, closed: false, tolerance: tolerance);
        if (sweeps != null && sweeps.Length > 0)
        {
            return sweeps[0].CapPlanarHoles(tolerance) ?? sweeps[0];
        }

        return null;
    }

    /// <summary>
    /// Khoét lỗ giảm trọng lượng hình elip / oval xuyên qua một tấm vách phẳng.
    /// </summary>
    public static Brep? CreateLighteningCutout(Brep targetPlate, Point3d centerPoint, double radiusX = 0.4, double radiusY = 0.6, double tolerance = 0.01)
    {
        if (targetPlate == null) return null;

        // Tạo hình trụ tròn/elip đâm xuyên vuông góc qua tấm vách
        var plane = new Plane(centerPoint, Vector3d.XAxis);
        var ellipse = new Ellipse(plane, radiusY, radiusX); // Trục đứng và ngang
        var ellipseCurve = ellipse.ToNurbsCurve();

        // Extrude tạo hình trụ cắt xuyên (Cutter)
        var cutterSurface = Surface.CreateExtrusion(ellipseCurve, new Vector3d(1.0, 0, 0));
        var cutterBrep = cutterSurface?.ToBrep()?.CapPlanarHoles(tolerance);

        if (cutterBrep == null) return null;

        // Dịch tâm khối cutter cho đâm xuyên qua 2 bên vách
        cutterBrep.Translate(new Vector3d(-0.5, 0, 0));

        // Trừ khối Boolean
        Brep[]? diff = Brep.CreateBooleanDifference(targetPlate, cutterBrep, tolerance);
        if (diff != null && diff.Length > 0)
        {
            return diff[0];
        }

        return null;
    }
}