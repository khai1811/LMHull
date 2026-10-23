using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class GAProjectionService
{
    /// <summary>
    /// Tạo mặt vách ngăn ngang phẳng từ vị trí X của một nét vẽ trên GA (bất kể nét vẽ nằm ở đâu).
    /// </summary>
    public static Brep? CreateBulkheadFromLine(Brep hullSolid, Curve gaCurve, double tolerance)
    {
        if (hullSolid == null || gaCurve == null) return null;

        double targetX = gaCurve.GetBoundingBox(true).Center.X;
        return FirstSection(hullSolid, HullSectionService.TransversePlaneAt(targetX), tolerance);
    }

    /// <summary>
    /// Tạo mặt boong phẳng từ cao độ Z của một nét vẽ trên GA.
    /// </summary>
    public static Brep? CreateDeckFromElevationLine(Brep hullSolid, Curve gaCurve, double tolerance)
    {
        if (hullSolid == null || gaCurve == null) return null;

        double targetZ = gaCurve.GetBoundingBox(true).Center.Z;
        return FirstSection(hullSolid, HullSectionService.HorizontalPlaneAt(targetZ), tolerance);
    }

    /// <summary>
    /// Tạo mặt vách dọc phẳng từ tọa độ Y của một nét vẽ trên GA (mặt bằng).
    /// </summary>
    public static Brep? CreateLongitudinalBulkheadFromLine(Brep hullSolid, Curve gaCurve, double tolerance)
    {
        if (hullSolid == null || gaCurve == null) return null;

        double targetY = gaCurve.GetBoundingBox(true).Center.Y;
        return FirstSection(hullSolid, HullSectionService.LongitudinalPlaneAt(targetY), tolerance);
    }

    // Trả về null nếu mặt phẳng nằm ngoài phạm vi thân tàu
    private static Brep? FirstSection(Brep hullSolid, Plane plane, double tolerance)
    {
        var sections = HullSectionService.CreatePlanarSections(hullSolid, plane, tolerance);
        return sections.Length > 0 ? sections[0] : null;
    }
}
