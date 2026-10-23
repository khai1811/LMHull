using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Structure.Models;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Calculations;

public class CompartmentCalculation : CalculationBase<CompartmentInput, List<CompartmentInfo>>
{
    public override string Name => "Compartments";

    protected override ValidationResult Validate(CompartmentInput input)
    {
        var validation = new ValidationResult();
        if (input.Hull == null || !input.Hull.IsSolid) validation.AddError("Thân tàu phải là khối kín (Solid Brep).");
        if (input.StationXPositions == null) validation.AddError("Thiếu danh sách vị trí vách.");
        return validation;
    }

    /// <summary>
    /// Cắt thân tàu kín bằng hệ thống mặt phẳng vách ngang thành các khoang độc lập.
    /// </summary>
    protected override List<CompartmentInfo> Execute(CompartmentInput input, CalculationContext context)
    {
        Brep hullSolid = input.Hull;
        double[] stationXPositions = input.StationXPositions;
        double tolerance = input.Tolerance;
        var compartments = new List<CompartmentInfo>();

        BoundingBox bbox = hullSolid.GetBoundingBox(true);
        double width = (bbox.Max.Y - bbox.Min.Y) * 2.0;
        double height = (bbox.Max.Z - bbox.Min.Z) * 2.0;

        // Tạo danh sách các mặt phẳng cắt vô hạn dạng Brep
        var cutterBreps = new List<Brep>();
        foreach (double x in stationXPositions.OrderBy(x => x))
        {
            var plane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
            var planeSrf = new PlaneSurface(
                plane,
                new Interval(bbox.Center.Y - width / 2, bbox.Center.Y + width / 2),
                new Interval(bbox.Center.Z - height / 2, bbox.Center.Z + height / 2)
            );
            cutterBreps.Add(planeSrf.ToBrep());
        }

        context.ThrowIfCancellationRequested();
        context.Report(10, "Cắt thân tàu bằng các mặt phẳng vách...");

        // Thực hiện cắt Brep thân tàu
        Brep[] splitPieces = hullSolid.Split(cutterBreps, tolerance);
        if (splitPieces == null || splitPieces.Length == 0)
        {
            splitPieces = new[] { hullSolid };
        }

        // Sắp xếp các khoang theo chiều dọc từ đuôi tàu (X nhỏ) đến mũi tàu (X lớn)
        var orderedPieces = splitPieces
            .Select(p => p.CapPlanarHoles(tolerance) ?? p)
            .OrderBy(p => p.GetBoundingBox(true).Center.X)
            .ToList();

        for (int i = 0; i < orderedPieces.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            context.ReportStep(i, orderedPieces.Count, $"Tính dung tích khoang {i + 1}/{orderedPieces.Count}", fromPercent: 30);

            Brep piece = orderedPieces[i];
            var volProps = VolumeMassProperties.Compute(piece);

            double vol = volProps?.Volume ?? 0.0;
            Point3d cg = volProps?.Centroid ?? piece.GetBoundingBox(true).Center;

            string name;
            if (i == 0)
                name = "AftPeak_Tank";
            else if (i == orderedPieces.Count - 1)
                name = "ForePeak_Tank";
            else
                name = $"Hold_Tank_{i:D2}";

            if (input.Limits != null && !input.Limits.Contains(cg.X, cg.Y, cg.Z)) continue;

            compartments.Add(new CompartmentInfo
            {
                Name = name,
                Geometry = piece,
                Volume = vol,
                CenterOfGravity = cg
            });
        }

        return compartments;
    }
}
