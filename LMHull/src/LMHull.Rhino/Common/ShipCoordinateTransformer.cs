using Rhino.Geometry;

namespace LMHull.Rhino.Common;

/// <summary>
/// Quản lý các phép biến đổi hình học và chuẩn hóa hệ tọa độ tàu thủy chuẩn:
/// - Gốc (0,0,0) tại AP, đường cơ sở (Baseline) và mặt phẳng dọc tâm (Centerline).
/// - Trục X: Dọc tàu (AP -> FP)
/// - Trục Y: Ngang tàu (Âm sang mạn phải, Dương sang mạn trái)
/// - Trục Z: Chiều đứng (Đáy lên boong)
/// </summary>
public static class ShipCoordinateTransformer
{
    /// <summary>
    /// Tạo ma trận đối xứng qua mặt phẳng dọc tâm (Centerline Plane - Mặt phẳng XZ tại Y=0).
    /// </summary>
    public static Transform GetMirrorAcrossCenterline()
    {
        // Mặt phẳng đối xứng là mặt phẳng XZ (đi qua gốc tọa độ, pháp tuyến là trục Y: Vector3d.YAxis)
        var centerlinePlane = new Plane(Point3d.Origin, Vector3d.YAxis);
        return Transform.Mirror(centerlinePlane);
    }

    /// <summary>
    /// Đưa một hình học bất kỳ (ví dụ vỏ tàu import từ tệp khác) về đúng hệ tọa độ tàu thủy chuẩn:
    /// - Điểm thấp nhất đáy đặt tại Z = 0 (Baseline)
    /// - Điểm mút sau cùng của đuôi đặt tại X = 0 (AP)
    /// - Tâm đối xứng ngang nằm chính xác tại Y = 0 (Centerline)
    /// </summary>
    public static Transform CreateAlignmentTransform(GeometryBase geometry)
    {
        BoundingBox bbox = geometry.GetBoundingBox(true);

        double currentMinX = bbox.Min.X;
        double currentCenterY = (bbox.Min.Y + bbox.Max.Y) / 2.0;
        double currentMinZ = bbox.Min.Z;

        // Vector tịnh tiến cần thiết để đưa về gốc chuẩn
        var translationVector = new Vector3d(
            -currentMinX,      // Đưa đuôi về X = 0
            -currentCenterY,   // Đưa tâm ngang về Y = 0
            -currentMinZ       // Đưa đáy về Z = 0
        );

        return Transform.Translation(translationVector);
    }
}