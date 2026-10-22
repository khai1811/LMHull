using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMCountObjectsCommand : Command
{
    // Tên lệnh thống kê đối tượng trong bản vẽ Rhino hiện tại
    public override string EnglishName => "ShipObject";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // Lấy tất cả đối tượng đang có trong tài liệu doc.Objects
        var allObjects = doc.Objects.GetObjectList(ObjectType.AnyObject).ToList();

        RhinoApp.WriteLine($"--- [LMCountObjects] Thống kê bản vẽ '{doc.Name ?? "Chưa đặt tên"}' ---");
        RhinoApp.WriteLine($" - Số lượng Layer trong doc.Layers   : {doc.Layers.Count}");
        RhinoApp.WriteLine($" - Tổng số đối tượng trong doc.Objects: {allObjects.Count}");
        RhinoApp.WriteLine($"   + Khối / Mặt (Brep/Extrusion)     : {allObjects.Count(o => o.ObjectType == ObjectType.Brep || o.ObjectType == ObjectType.Extrusion)}");
        RhinoApp.WriteLine($"   + Đường cong (Curve)              : {allObjects.Count(o => o.ObjectType == ObjectType.Curve)}");
        RhinoApp.WriteLine($"   + Điểm (Point)                    : {allObjects.Count(o => o.ObjectType == ObjectType.Point)}");

        return Result.Success;
    }
}