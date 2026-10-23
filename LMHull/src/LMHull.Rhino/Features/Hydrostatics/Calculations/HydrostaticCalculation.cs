using System;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Hydrostatics.Models;
using LMHull.Rhino.Features.Hydrostatics.Services;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Hydrostatics.Calculations;

public class HydrostaticCalculation : CalculationBase<HydrostaticInput, DetailedHydroResult>
{
    public override string Name => "Hydrostatics";

    protected override ValidationResult Validate(HydrostaticInput input)
    {
        var validation = new ValidationResult();
        if (input.Hull == null) validation.AddError("Chưa có hình học vỏ tàu (Brep).");
        if (input.Draft <= 0) validation.AddError("Mớn nước T phải lớn hơn 0.");
        return validation;
    }

    protected override DetailedHydroResult Execute(HydrostaticInput input, CalculationContext context)
    {
        Brep hullBrep = input.Hull;
        double draft = input.Draft;
        double waterDensity = input.WaterDensity;
        double tolerance = input.Tolerance;

        BoundingBox bbox = hullBrep.GetBoundingBox(true);
        double waterZ = bbox.Min.Z + draft;

        context.Report(10, "Cắt mặt đường nước...");

        // 1. CẮT MẶT NƯỚC TẠI Z = waterZ
        var waterPlane = new Plane(new Point3d(0, 0, waterZ), Vector3d.ZAxis);

        Curve[]? waterlineCurves;
        Point3d[]? pts;
        Intersection.BrepPlane(hullBrep, waterPlane, tolerance, out waterlineCurves, out pts);

        double aw = 0.0;
        Point3d cf = Point3d.Origin;

        if (waterlineCurves != null && waterlineCurves.Length > 0)
        {
            var joinedWaterlines = Curve.JoinCurves(waterlineCurves, tolerance);
            if (joinedWaterlines != null && joinedWaterlines.Length > 0)
            {
                var planarBreps = Brep.CreatePlanarBreps(joinedWaterlines[0], tolerance);
                if (planarBreps != null && planarBreps.Length > 0)
                {
                    var areaProps = AreaMassProperties.Compute(planarBreps[0]);
                    if (areaProps != null)
                    {
                        aw = areaProps.Area;
                        cf = areaProps.Centroid;
                    }
                }
            }
        }

        context.ThrowIfCancellationRequested();
        context.Report(30, "Tính phần chìm và tâm nổi...");

        // 2. CẮT LẤY PHẦN CHÌM DƯỚI NƯỚC ĐỂ TÍNH THỂ TÍCH V VÀ TÂM NỔI B
        double volume = 0.0;
        Point3d cb = Point3d.Origin;
        if (BrepSlicer.VolumeBelow(hullBrep, waterZ, tolerance) is var (submergedVolume, centroid))
        {
            volume = submergedVolume;
            cb = centroid;
        }

        // Nếu vỏ tàu là Brep hở hoặc chưa khâu kín nắp, ước lượng theo Aw * T * hệ số lăng trụ
        if (volume <= 0.0)
        {
            double L = bbox.Max.X - bbox.Min.X;
            double B = bbox.Max.Y - bbox.Min.Y;
            volume = (aw > 0 ? aw : (L * B * 0.8)) * draft * 0.75;
            cb = new Point3d(bbox.Center.X, 0, bbox.Min.Z + draft * 0.53);
        }

        context.Report(70, "Tính các hệ số thủy tĩnh...");

        double displacement = volume * waterDensity;
        double lengthTotal = bbox.Max.X - bbox.Min.X;
        double beamTotal = bbox.Max.Y - bbox.Min.Y;
        double cbCoeff = (lengthTotal * beamTotal * draft > 0) ? (volume / (lengthTotal * beamTotal * draft)) : 0;

        return new DetailedHydroResult
        {
            SubmergedVolume = volume,
            Displacement = displacement,
            WaterplaneArea = aw,
            CenterOfBuoyancy = cb,
            CenterOfFlotation = cf,
            BlockCoefficient = cbCoeff
        };
    }
}
