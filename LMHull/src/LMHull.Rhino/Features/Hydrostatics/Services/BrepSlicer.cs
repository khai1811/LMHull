using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.Services;

/// <summary>
/// Cắt một Brep kín bằng mặt phẳng ngang Z và lấy phần nằm dưới (phần chìm của vỏ, phần chất lỏng trong két).
/// </summary>
public static class BrepSlicer
{
    /// <summary>
    /// Thể tích và trọng tâm phần Brep nằm dưới cao độ z; null nếu không cắt được hoặc thể tích bằng 0.
    /// </summary>
    public static (double Volume, Point3d Centroid)? VolumeBelow(Brep brep, double z, double tolerance)
    {
        var plane = new Plane(new Point3d(0, 0, z), Vector3d.ZAxis);
        Brep[] parts = brep.Trim(plane, tolerance);
        if (parts == null) return null;

        foreach (var part in parts)
        {
            // Chỉ lấy phần nằm hoàn toàn dưới mặt phẳng cắt
            if (part.GetBoundingBox(true).Max.Z > z + tolerance) continue;

            Brep capped = part.CapPlanarHoles(tolerance) ?? part;
            var props = VolumeMassProperties.Compute(capped);
            if (props != null && props.Volume > 0) return (props.Volume, props.Centroid);
        }
        return null;
    }
}
