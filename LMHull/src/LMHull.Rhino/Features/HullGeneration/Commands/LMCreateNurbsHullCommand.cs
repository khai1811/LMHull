using System;
using System.Collections.Generic;
using System.Drawing;
using LMHull.Core.Models;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMCreateNurbsHullCommand : Command
{
    public override string EnglishName => "LMCreateNurbsHull";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Dữ liệu tàu cơ bản
        var ship = new ShipInput("Vỏ tàu NURBS v0.6", length: 50.0, beam: 10.0, draft: 2.5, depth: 4.0);

        // 2. Đảm bảo Layer tiêu chuẩn tồn tại
        LayerManager.EnsureStandardLayers(doc);
        int hullLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.HullLayerName, Color.FromArgb(200, 40, 40));
        int refLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.ReferenceLayerName, Color.FromArgb(0, 120, 215));

        // 3. DỰNG CÁC ĐƯỜNG SƯỜN NURBS (NurbsCurvesBuilder)
        List<Curve> stationCurves = NurbsCurvesBuilder.BuildStationCurves(ship, numberOfStations: 7);
        for (int i = 0; i < stationCurves.Count; i++)
        {
            var attr = new ObjectAttributes
            {
                LayerIndex = refLayerIndex,
                Name = $"StationCurve_{i}"
            };
            doc.Objects.AddCurve(stationCurves[i], attr);
        }

        // 4. LOFT CÁC ĐƯỜNG SƯỜN THÀNH MẶT VỎ TÀU NURBS (NurbsHullLofter)
        Brep? hullBrep = NurbsHullLofter.CreateLoftedHull(stationCurves);
        if (hullBrep == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể tạo mặt Loft từ các đường sườn NURBS.");
            return Result.Failure;
        }

        // 5. ĐƯA MẶT VỎ VÀO DOC.OBJECTS TRONG LAYER "HULL"
        var hullAttr = new ObjectAttributes
        {
            LayerIndex = hullLayerIndex,
            Name = ship.Name
        };
        Guid hullGuid = doc.Objects.AddBrep(hullBrep, hullAttr);
        if (hullGuid == Guid.Empty)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể lưu mặt vỏ vào tài liệu Rhino.");
            return Result.Failure;
        }

        // Cập nhật Guid vỏ tàu vào Plugin để dùng cho các bài sau
        LMHullPlugin.Instance.LastHullGuid = hullGuid;

        // 6. LÀM MỚI KHUNG NHÌN
        doc.Views.Redraw();

        // 7. TRÍCH XUẤT VÀ IN THÔNG SỐ TOÁN HỌC CỦA MẶT NURBS
        // Lấy mặt NURBS đầu tiên của vỏ tàu
        NurbsSurface? nurbsSurface = hullBrep.Faces[0].ToNurbsSurface();

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine($" [LMCreateNurbsHull] Đã dựng thành công vỏ cong: {ship.Name}");
        RhinoApp.WriteLine($" - Mã Guid vỏ tàu       : {hullGuid}");
        RhinoApp.WriteLine($" - Số đường sườn tạo ra : {stationCurves.Count} sườn (Layer: {LayerManager.ReferenceLayerName})");

        if (nurbsSurface != null)
        {
            RhinoApp.WriteLine(" --- THÔNG SỐ KỸ THUẬT MẶT NURBS ---");
            RhinoApp.WriteLine($" - Bậc theo hướng U x V : {nurbsSurface.Degree(0)} x {nurbsSurface.Degree(1)}");
            RhinoApp.WriteLine($" - Lưới điểm đ.khiển (U x V): {nurbsSurface.Points.CountU} x {nurbsSurface.Points.CountV} (Tổng: {nurbsSurface.Points.CountU * nurbsSurface.Points.CountV} điểm)");
            RhinoApp.WriteLine($" - Số nút (Knots U x V) : {nurbsSurface.KnotsU.Count} x {nurbsSurface.KnotsV.Count}");
            RhinoApp.WriteLine($" - Miền tham số U       : [{nurbsSurface.Domain(0).Min:F2} - {nurbsSurface.Domain(0).Max:F2}]");
            RhinoApp.WriteLine($" - Miền tham số V       : [{nurbsSurface.Domain(1).Min:F2} - {nurbsSurface.Domain(1).Max:F2}]");
        }
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}