using System;
using System.Collections.Generic;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.LinesPlan.Models;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.LinesPlan.Calculations;

/// <summary>
/// Cắt vỏ tàu 3D thành hệ thống đường sườn, đường nước và đường mông; báo tiến độ theo từng mặt cắt.
/// </summary>
public class LinesPlanCalculation : CalculationBase<LinesPlanInput, LinesPlanResult>
{
    public override string Name => "LinesPlan";

    protected override ValidationResult Validate(LinesPlanInput input)
    {
        var validation = new ValidationResult();
        if (input.Hull == null) validation.AddError("Chưa có hình học vỏ tàu (Brep).");
        if (input.StationCount < 2) validation.AddError("Số mặt cắt sườn phải từ 2 trở lên.");
        if (input.WaterlineCount < 0 || input.ButtockCount < 0) validation.AddError("Số đường nước / đường mông không được âm.");
        return validation;
    }

    protected override LinesPlanResult Execute(LinesPlanInput input, CalculationContext context)
    {
        var result = new LinesPlanResult();
        Brep hullBrep = input.Hull;
        double tolerance = input.Tolerance;
        BoundingBox bbox = hullBrep.GetBoundingBox(true);

        double minX = bbox.Min.X, maxX = bbox.Max.X;
        double minY = bbox.Min.Y, maxY = bbox.Max.Y;
        double minZ = bbox.Min.Z, maxZ = bbox.Max.Z;

        // Danh sách toàn bộ mặt cắt để báo tiến độ theo một thang chung
        var cuts = new List<(Plane Plane, List<Curve> Target, string Label)>();

        // 1. Hệ đường sườn (Stations: X = const)
        double dx = (maxX - minX) / (input.StationCount - 1);
        for (int i = 0; i < input.StationCount; i++)
        {
            cuts.Add((new Plane(new Point3d(minX + i * dx, 0, 0), Vector3d.XAxis), result.StationCurves, $"Cắt sườn {i + 1}/{input.StationCount}"));
        }

        // 2. Hệ đường nước (Waterlines: Z = const)
        double dz = (maxZ - minZ) / (input.WaterlineCount + 1);
        for (int i = 1; i <= input.WaterlineCount; i++)
        {
            cuts.Add((new Plane(new Point3d(0, 0, minZ + i * dz), Vector3d.ZAxis), result.WaterlineCurves, $"Cắt đường nước {i}/{input.WaterlineCount}"));
        }

        // 3. Hệ đường mông (Buttocks: Y = const), cắt cả 2 mạn
        double halfB = Math.Max(Math.Abs(minY), Math.Abs(maxY));
        double dy = halfB / (input.ButtockCount + 1);
        for (int i = 1; i <= input.ButtockCount; i++)
        {
            double y = i * dy;
            string label = $"Cắt đường mông {i}/{input.ButtockCount}";
            cuts.Add((new Plane(new Point3d(0, -y, 0), Vector3d.YAxis), result.ButtockCurves, label));
            cuts.Add((new Plane(new Point3d(0, y, 0), Vector3d.YAxis), result.ButtockCurves, label));
        }

        for (int i = 0; i < cuts.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            var (plane, target, label) = cuts[i];

            if (Intersection.BrepPlane(hullBrep, plane, tolerance, out Curve[]? curves, out _) && curves != null)
            {
                target.AddRange(curves);
            }
            context.ReportStep(i, cuts.Count, label);
        }

        return result;
    }
}
