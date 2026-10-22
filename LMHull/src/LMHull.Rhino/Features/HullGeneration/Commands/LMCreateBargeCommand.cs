using System;
using System.Drawing;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMCreateBargeCommand : Command
{
    // Tên lệnh nhập trên thanh Command của Rhino
    public override string EnglishName => "LMCreateBarge";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chuẩn bị dữ liệu sà lan (L = 45m, B = 10m, T = 2.5m, D = 3.5m)
        var ship = new ShipInput("Sà lan hộp LM-01", length: 45.0, beam: 10.0, draft: 2.5, depth: 3.5);

        // 2. Đảm bảo các layer chuẩn đã được tạo trong doc.Layers
        LayerManager.EnsureStandardLayers(doc);
        int hullLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.HullLayerName, Color.Red);
        int refLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.ReferenceLayerName, Color.Blue);

        // 3. TẠO HÌNH HỌC TRONG BỘ NHỚ RAM (Rhino.Geometry)
        Brep bargeBrep = BoxBargeBuilder.Build(ship);
        Point3d apPoint = new Point3d(0, 0, 0); // Điểm gốc AP

        // 4. ĐƯA HÌNH HỌC VÀO TÀI LIỆU (doc.Objects) VÀ GÁN LAYER
        var hullAttributes = new ObjectAttributes
        {
            LayerIndex = hullLayerIndex,
            Name = ship.Name
        };

        // Ghi Brep vào bảng đối tượng doc.Objects -> Nhận về Guid
        Guid hullGuid = doc.Objects.AddBrep(bargeBrep, hullAttributes);
        if (hullGuid == Guid.Empty)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể thêm khối sà lan vào tài liệu Rhino.");
            return Result.Failure;
        }

        // Thêm điểm mốc AP vào layer Reference
        var refAttributes = new ObjectAttributes { LayerIndex = refLayerIndex, Name = "AP_Origin" };
        doc.Objects.AddPoint(apPoint, refAttributes);

        // 5. LƯU GUID VỎ TÀU VÀO PLUGIN ĐỂ DÙNG LẠI CHO CÁC BÀI SAU
        LMHullPlugin.Instance.LastHullGuid = hullGuid;

        // 6. LÀM MỚI KHUNG NHÌN 3D
        doc.Views.Redraw();

        // 7. Gọi LMHull.Core tính toán và in kết quả kiểm tra
        var calc = new ShipCalculator();
        ShipResult res = calc.Calculate(ship, WaterType.SeaWater);

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine($" [LMCreateBarge] Đã tạo thành công {ship.Name}!");
        RhinoApp.WriteLine($" - Kích thước L x B x D : {ship.Length}m x {ship.Beam}m x {ship.Depth}m");
        RhinoApp.WriteLine($" - Layer chỉ định       : {LayerManager.HullLayerName} (Màu đỏ)");
        RhinoApp.WriteLine($" - Mã Guid vỏ tàu       : {hullGuid}");
        RhinoApp.WriteLine($" - Thể tích chìm (T={ship.Draft}m): {res.BoxVolume:F2} m3");
        RhinoApp.WriteLine($" - Lượng chiếm nước     : {res.Displacement:F2} Tấn");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}