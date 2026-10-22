using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.UI.Conduits;

/// <summary>
/// DisplayConduit giúp vẽ mặt nước trực tiếp lên viewport theo thời gian thực mà không thêm object vào RhinoDoc.
/// </summary>
public class WaterplaneConduit : DisplayConduit
{
    public double WaterZ { get; set; } = 2.5;
    public double Length { get; set; } = 60.0;
    public double Beam { get; set; } = 20.0;

    protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
    {
        base.CalculateBoundingBox(e);
        e.IncludeBoundingBox(new BoundingBox(-5, -Beam, WaterZ - 0.5, Length + 5, Beam, WaterZ + 0.5));
    }

    protected override void DrawOverlay(DrawEventArgs e)
    {
        base.DrawOverlay(e);

        // Tạo lưới mặt nước phẳng bán trong suốt
        var p0 = new Point3d(-2, -Beam / 1.5, WaterZ);
        var p1 = new Point3d(Length + 2, -Beam / 1.5, WaterZ);
        var p2 = new Point3d(Length + 2, Beam / 1.5, WaterZ);
        var p3 = new Point3d(-2, Beam / 1.5, WaterZ);

        // Màu xanh nước biển bán trong suốt (Alpha = 70)
        var waterColor = Color.FromArgb(70, 0, 150, 255);
        var edgeColor = Color.FromArgb(200, 0, 100, 220);

        var mesh = new Mesh();
        mesh.Vertices.Add(p0);
        mesh.Vertices.Add(p1);
        mesh.Vertices.Add(p2);
        mesh.Vertices.Add(p3);
        mesh.Faces.AddFace(0, 1, 2, 3);
        mesh.Normals.ComputeNormals();

        var mat = new DisplayMaterial(waterColor) { Transparency = 0.65 };
        e.Display.DrawMeshShaded(mesh, mat);
        e.Display.DrawPolyline(new Point3d[] { p0, p1, p2, p3, p0 }, edgeColor, 2);
    }
}