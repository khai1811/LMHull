using System.Collections.Generic;
using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

/// <summary>
/// Chịu trách nhiệm tạo các đường sườn NURBS dọc theo thân tàu.
/// </summary>
public static class NurbsCurvesBuilder
{
    /// <summary>
    /// Sinh ra các đường sườn (Stations) dạng NURBS Curve.
    /// Mỗi đường sườn đi từ mạn phải (Deck Starboard) -> Đáy (Keel) -> Mạn trái (Deck Port).
    /// </summary>
    public static List<Curve> BuildStationCurves(ShipInput input, int numberOfStations = 6)
    {
        var sectionCurves = new List<Curve>();
        double L = input.Length;
        double B = input.Beam;
        double D = input.Depth ?? (input.Draft * 1.4);
        double spacing = L / (numberOfStations - 1);

        for (int i = 0; i < numberOfStations; i++)
        {
            double x = i * spacing;
            double progress = (double)i / (numberOfStations - 1); // 0 (đuôi) -> 1 (mũi)

            // Mô phỏng độ vát thon của thân tàu theo chiều dọc:
            // Phần đuôi và mũi hẹp hơn, phần giữa tàu rộng hết cỡ (B/2)
            double widthFactor = Math.Sin(progress * Math.PI);
            if (widthFactor < 0.15) widthFactor = 0.15; // Giữ độ rộng tối thiểu ở mũi và lái

            double currentHalfB = (B / 2.0) * widthFactor;

            // 5 điểm điều khiển định hình đường sườn đối xứng dạng chữ U tròn đáy:
            // 1. Mạn phải boong (Z = D, Y = -halfB)
            // 2. Hông phải (Z = D * 0.25, Y = -halfB)
            // 3. Sống đáy tâm tàu (Z = 0, Y = 0)
            // 4. Hông trái (Z = D * 0.25, Y = +halfB)
            // 5. Mạn trái boong (Z = D, Y = +halfB)
            var controlPoints = new List<Point3d>
            {
                new Point3d(x, -currentHalfB, D),
                new Point3d(x, -currentHalfB, D * 0.25),
                new Point3d(x, 0, 0),
                new Point3d(x, currentHalfB, D * 0.25),
                new Point3d(x, currentHalfB, D)
            };

            // Dựng đường cong NURBS nội suy mượt qua các điểm điều khiển với Bậc 3 (Degree 3)
            var stationCurve = Curve.CreateInterpolatedCurve(controlPoints, 3);
            if (stationCurve != null)
            {
                sectionCurves.Add(stationCurve);
            }
        }

        return sectionCurves;
    }
}