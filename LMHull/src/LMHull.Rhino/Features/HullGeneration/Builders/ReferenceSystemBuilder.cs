using System.Collections.Generic;
using LMHull.Core.Models;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Builders;

/// <summary>
/// Chịu trách nhiệm tạo các đối tượng hình học tham chiếu: Điểm mốc, Đường tim, Đường nước, Mặt phẳng sườn.
/// </summary>
public static class ReferenceSystemBuilder
{
    /// <summary>
    /// Tạo 3 điểm mốc đặc trưng của tàu: AP (X=0), MP (X=L/2), FP (X=L).
    /// </summary>
    public static (Point3d AP, Point3d MP, Point3d FP) CreateKeyPoints(ShipInput input)
    {
        double L = input.Length;
        var ap = new Point3d(0, 0, 0);          // Aft Perpendicular (Vuông góc lái)
        var mp = new Point3d(L / 2.0, 0, 0);    // Midship (Điểm giữa tàu)
        var fp = new Point3d(L, 0, 0);          // Forward Perpendicular (Vuông góc mũi)

        return (ap, mp, fp);
    }

    /// <summary>
    /// Tạo các đường tham chiếu chuẩn dạng Line:
    /// - BaseLine: Đi từ AP đến FP dọc theo đáy (Z=0, Y=0)
    /// - CenterLine: Đi từ AP đến FP tại chiều cao mạn D (Z=D, Y=0)
    /// - WaterLine: Đi từ AP đến FP tại mớn nước thiết kế T (Z=T, Y=0)
    /// </summary>
    public static List<(string Name, Line LineGeo)> CreateReferenceLines(ShipInput input)
    {
        double L = input.Length;
        double T = input.Draft;
        double D = input.Depth ?? (input.Draft * 1.4);

        var lines = new List<(string Name, Line LineGeo)>
        {
            ("BaseLine", new Line(new Point3d(0, 0, 0), new Point3d(L, 0, 0))),
            ("DesignWaterLine_T", new Line(new Point3d(0, 0, T), new Point3d(L, 0, T))),
            ("CenterLine_Deck", new Line(new Point3d(0, 0, D), new Point3d(L, 0, D)))
        };

        return lines;
    }

    /// <summary>
    /// Dựng hệ thống các mặt phẳng sườn (Station Planes) chia đều theo chiều dài L.
    /// Thông thường chia thành 10 hoặc 20 khoảng sườn lý thuyết (Station 0 đến Station N).
    /// </summary>
    public static List<(string Name, Plane PlaneGeo, Rectangle3d FrameBoundary)> CreateStationPlanes(ShipInput input, int numberOfStations = 10)
    {
        var stations = new List<(string Name, Plane PlaneGeo, Rectangle3d FrameBoundary)>();
        double L = input.Length;
        double B = input.Beam;
        double D = input.Depth ?? (input.Draft * 1.4);
        double stationSpacing = L / numberOfStations;

        // Vector pháp tuyến của mặt cắt sườn là vector dọc trục tàu (Trục X)
        // Trục X của Plane sẽ là hướng ngang tàu (Vector Y thế giới)
        // Trục Y của Plane sẽ là hướng đứng tàu (Vector Z thế giới)
        var normalX = new Vector3d(1, 0, 0);
        var yAxis = new Vector3d(0, 1, 0);
        var zAxis = new Vector3d(0, 0, 1);

        for (int i = 0; i <= numberOfStations; i++)
        {
            double xCoord = i * stationSpacing;
            var origin = new Point3d(xCoord, 0, 0);

            // Khởi tạo mặt phẳng sườn vuông góc với trục dọc tàu
            var plane = new Plane(origin, yAxis, zAxis);

            // Tạo khung hình chữ nhật thể hiện kích thước bao ngang (từ -B/2 đến B/2 và từ 0 đến D)
            var xInterval = new Interval(-B / 2.0, B / 2.0);
            var yInterval = new Interval(0, D);
            var rect = new Rectangle3d(plane, xInterval, yInterval);

            stations.Add(($"Station_{i}", plane, rect));
        }

        return stations;
    }
}