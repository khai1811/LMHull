using System;
using LMHull.Core.Models;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMCreateRefLinesCommand : Command
{
    public override string EnglishName => "LMCreateRefLines";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Lấy thông số sà lan mẫu (hoặc chuẩn bị từ phiên trước)
        var ship = new ShipInput("Sà lan chuẩn LM-01", length: 45.0, beam: 10.0, draft: 2.5, depth: 3.5);

        // 2. Đảm bảo layer Reference đã tồn tại
        var cad = RhinoCadHandle.For(doc);
        LayerManager.EnsureStandardLayers(cad);

        // 3. TẠO VÀ GHI CÁC ĐIỂM MỐC (AP, MP, FP)
        var (ap, mp, fp) = ReferenceSystemBuilder.CreateKeyPoints(ship);

        cad.AddPoint(ap, LayerManager.Reference.Spec("AP (Aft Perpendicular)"));
        cad.AddPoint(mp, LayerManager.Reference.Spec("MP (Midship)"));
        cad.AddPoint(fp, LayerManager.Reference.Spec("FP (Forward Perpendicular)"));

        // 4. TẠO VÀ GHI CÁC ĐƯỜNG THAM CHIẾU (Line -> LineCurve)
        var lines = ReferenceSystemBuilder.CreateReferenceLines(ship);
        foreach (var item in lines)
        {
            cad.AddCurve(new LineCurve(item.LineGeo), LayerManager.Reference.Spec(item.Name));
        }

        // 5. TẠO HỆ THỐNG MẶT PHẲNG SƯỜN (10 khoảng sườn lý thuyết)
        var stations = ReferenceSystemBuilder.CreateStationPlanes(ship, numberOfStations: 10);
        foreach (var station in stations)
        {
            // Thêm đường bao khung sườn Rectangle3d (chuyển sang NurbsCurve) để trực quan hóa mặt cắt
            cad.AddCurve(station.FrameBoundary.ToNurbsCurve(), LayerManager.Reference.Spec(station.Name));
        }

        // 6. CẬP NHẬT VIEWPORT
        cad.Redraw();

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine($" [LMCreateRefLines] Đã thiết lập hệ tham chiếu cho {ship.Name}:");
        RhinoApp.WriteLine(" - Điểm mốc : AP (X=0), MP (X=22.5m), FP (X=45m)");
        RhinoApp.WriteLine($" - Đường tim: BaseLine, CenterLine, DWL (Z={ship.Draft}m)");
        RhinoApp.WriteLine($" - Mặt sườn : 11 vị trí (Station 0 đến 10)");
        RhinoApp.WriteLine($" - Toàn bộ được đặt vào layer: '{LayerManager.ReferenceLayerName}'");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }

}