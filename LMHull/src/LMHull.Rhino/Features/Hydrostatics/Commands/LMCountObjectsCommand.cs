using LMHull.Rhino.Infrastructure.Cad;
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
        var cad = RhinoCadHandle.For(doc);

        RhinoApp.WriteLine($"--- [LMCountObjects] Thống kê bản vẽ '{(string.IsNullOrEmpty(cad.DocumentName) ? "Chưa đặt tên" : cad.DocumentName)}' ---");
        RhinoApp.WriteLine($" - Số lượng Layer                   : {cad.LayerCount}");
        RhinoApp.WriteLine($" - Tổng số đối tượng                : {cad.ObjectCount}");
        RhinoApp.WriteLine($"   + Khối / Mặt (Brep/Extrusion)     : {cad.CountObjects(ObjectType.Brep | ObjectType.Extrusion)}");
        RhinoApp.WriteLine($"   + Đường cong (Curve)              : {cad.CountObjects(ObjectType.Curve)}");
        RhinoApp.WriteLine($"   + Điểm (Point)                    : {cad.CountObjects(ObjectType.Point)}");

        return Result.Success;
    }
}