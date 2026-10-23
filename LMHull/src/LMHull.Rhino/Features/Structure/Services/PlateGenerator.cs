using System;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class PlateGenerator
{
    /// <summary>
    /// Offset mặt Brep thành tấm tôn đặc có chiều dày danh định (mm).
    /// Hỗ trợ cả mặt đơn lẻ lẫn tách mặt từ khối Solid Polysurface.
    /// </summary>
    public static Brep? CreateSolidPlate(Brep sourceBrep, double thicknessMm, bool offsetInward = true, double tolerance = 0.01)
    {
        if (sourceBrep == null) return null;

        double thicknessM = thicknessMm / 1000.0;
        double offsetDistance = offsetInward ? -thicknessM : thicknessM;

        // Nếu đối tượng chọn là cả khối kín, lấy mặt đầu tiên (thường là mặt vỏ cong)
        Brep targetBrep = sourceBrep;
        if (sourceBrep.IsSolid && sourceBrep.Faces.Count > 0)
        {
            // Trích xuất mặt vỏ mạn cong để làm tấm tôn
            targetBrep = sourceBrep.Faces[0].DuplicateFace(false);
        }

        // Thực hiện Offset thành tấm đặc
        Brep[] plates = Brep.CreateOffsetBrep(
            targetBrep,
            offsetDistance,
            solid: true,
            extend: false,
            tolerance: tolerance,
            out _,
            out _
        );

        if (plates != null && plates.Length > 0 && plates[0].IsSolid)
        {
            return plates[0];
        }

        // Phương án dự phòng: Offset mặt hở rồi khâu nẹp mép
        Brep[] openOffsets = Brep.CreateOffsetBrep(
            targetBrep,
            offsetDistance,
            solid: false,
            extend: false,
            tolerance: tolerance,
            out _,
            out _
        );

        if (openOffsets != null && openOffsets.Length > 0)
        {
            var loftEdges = Brep.CreateFromLoft(
                new[] { targetBrep.Edges[0].EdgeCurve, openOffsets[0].Edges[0].EdgeCurve },
                Point3d.Unset, Point3d.Unset, LoftType.Normal, false
            );

            var joined = Brep.JoinBreps(new[] { targetBrep, openOffsets[0] }, tolerance);
            if (joined != null && joined.Length > 0)
            {
                return joined[0].CapPlanarHoles(tolerance) ?? joined[0];
            }
        }

        return null;
    }
}