using System;
using System.Linq;
using LMHull.Core.Models;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace LMHull.Rhino.Features.Hydrostatics.Services;

/// <summary>
/// Kết quả tính toán thủy tĩnh chi tiết từ hình học Brep thực tế.
/// </summary>
public class DetailedHydroResult
{
    public double SubmergedVolume { get; set; }   // V (m3)
    public double Displacement { get; set; }       // Delta (tấn)
    public double WaterplaneArea { get; set; }     // Aw (m2)
    public Point3d CenterOfBuoyancy { get; set; }  // CB (LCB, TCB, VCB / KB)
    public Point3d CenterOfFlotation { get; set; } // CF (LCF, TCF)
    public double BlockCoefficient { get; set; }   // Cb
}

public static class ActualHydrostaticCalculator
{
    /// <summary>
    /// Tính toán các đặc trưng thủy tĩnh thực tế của Brep vỏ tàu tại mớn nước T.
    /// </summary>
    public static DetailedHydroResult? Compute(Brep hullBrep, double draft, double waterDensity = 1.025, double tolerance = 0.01)
    {
        BoundingBox bbox = hullBrep.GetBoundingBox(true);
        double waterZ = bbox.Min.Z + draft;

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

        // 2. CẮT LẤY PHẦN CHÌM DƯỚI NƯỚC ĐỂ TÍNH THỂ TÍCH V VÀ TÂM NỔI B
        Brep[] trimmedParts = hullBrep.Trim(waterPlane, tolerance);
        double volume = 0.0;
        Point3d cb = Point3d.Origin;

        if (trimmedParts != null && trimmedParts.Length > 0)
        {
            foreach (var part in trimmedParts)
            {
                BoundingBox partBbox = part.GetBoundingBox(true);
                // Phần chìm nằm phía dưới cao độ mặt nước
                if (partBbox.Max.Z <= waterZ + tolerance)
                {
                    Brep cappedSubmerged = part.CapPlanarHoles(tolerance) ?? part;
                    var volProps = VolumeMassProperties.Compute(cappedSubmerged);
                    if (volProps != null && volProps.Volume > 0)
                    {
                        volume = volProps.Volume;
                        cb = volProps.Centroid;
                        break;
                    }
                }
            }
        }

        // Nếu vỏ tàu là Brep hở hoặc chưa khâu kín nắp, ước lượng theo Aw * T * hệ số lăng trụ
        if (volume <= 0.0)
        {
            double L = bbox.Max.X - bbox.Min.X;
            double B = bbox.Max.Y - bbox.Min.Y;
            volume = (aw > 0 ? aw : (L * B * 0.8)) * draft * 0.75;
            cb = new Point3d(bbox.Center.X, 0, bbox.Min.Z + draft * 0.53);
        }

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