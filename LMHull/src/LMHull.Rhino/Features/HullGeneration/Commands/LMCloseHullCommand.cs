using System;
using System.Drawing;
using System.Linq;
using LMHull.Core.Models;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMCloseHullCommand : Command
{
    public override string EnglishName => "LMCloseHull";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Kiểm tra xem đã có vỏ tàu được tạo từ các bài trước chưa
        Guid lastGuid = LMHullPlugin.Instance.LastHullGuid;
        RhinoObject? hullObj = doc.Objects.FindId(lastGuid);

        Brep? openHullBrep = null;

        if (hullObj != null && hullObj.Geometry is Brep b)
        {
            openHullBrep = b;
            RhinoApp.WriteLine($"[LMCloseHull] Đang xử lý vỏ tàu có sẵn (Guid: {lastGuid})...");
        }
        else
        {
            // Nếu chưa có, tự động tạo mới một vỏ NURBS mẫu
            var shipInput = new ShipInput("Vỏ tàu đóng kín v0.7", length: 50.0, beam: 10.0, draft: 2.5, depth: 4.0);
            var curves = NurbsCurvesBuilder.BuildStationCurves(shipInput, 7);
            openHullBrep = NurbsHullLofter.CreateLoftedHull(curves);
        }

        if (openHullBrep == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Không tìm thấy hoặc không thể tạo mặt vỏ để đóng kín.");
            return Result.Failure;
        }

        var ship = new ShipInput("Vỏ tàu đóng kín v0.7", length: 50.0, beam: 10.0, draft: 2.5, depth: 4.0);

        // 2. TIẾN HÀNH ĐÓNG KÍN VỎ TÀU (SolidHullStitcher)
        var (closedBrep, nakedCount) = SolidHullStitcher.CreateSolidHull(openHullBrep, ship, doc.ModelAbsoluteTolerance);

        if (closedBrep == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Quá trình đóng kín Brep thất bại.");
            return Result.Failure;
        }

        // 3. THAY THẾ ĐỐI TƯỢNG CŨ HOẶC THÊM MỚI VÀO BẢN VẼ
        LayerManager.EnsureStandardLayers(doc);
        int hullLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.HullLayerName, Color.FromArgb(200, 40, 40));

        var attr = new ObjectAttributes
        {
            LayerIndex = hullLayerIndex,
            Name = "Vỏ tàu kín (Solid Brep)"
        };

        if (hullObj != null)
        {
            doc.Objects.Replace(lastGuid, closedBrep);
        }
        else
        {
            Guid newGuid = doc.Objects.AddBrep(closedBrep, attr);
            LMHullPlugin.Instance.LastHullGuid = newGuid;
        }

        doc.Views.Redraw();

        // 4. THỐNG KÊ CẤU TRÚC TOPO CỦA BREP
        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine(" [LMCloseHull] KẾT QUẢ KIỂM TRA CẤU TRÚC TOPO BREP:");
        RhinoApp.WriteLine($" - Trạng thái khối kín (IsSolid) : {(closedBrep.IsSolid ? "KÍN TUYỆT ĐỐI (SOLID)" : "VẪN HỞ (OPEN)")}");
        RhinoApp.WriteLine($" - Số cạnh hở (Naked Edges)     : {nakedCount}");
        RhinoApp.WriteLine($" - Số lượng Mặt (Faces)          : {closedBrep.Faces.Count}");
        RhinoApp.WriteLine($" - Số lượng Cạnh (Edges)          : {closedBrep.Edges.Count}");
        RhinoApp.WriteLine($" - Số lượng Đỉnh (Vertices)       : {closedBrep.Vertices.Count}");

        // Nếu là khối kín, tính toán ngay thể tích hình học thực tế
        if (closedBrep.IsSolid)
        {
            var volumeProps = VolumeMassProperties.Compute(closedBrep);
            if (volumeProps != null)
            {
                RhinoApp.WriteLine($" - Tổng thể tích toàn bộ khối    : {volumeProps.Volume:F2} m3");
                RhinoApp.WriteLine($" - Tọa độ trọng tâm hình học (Centroid): ({volumeProps.Centroid.X:F2}, {volumeProps.Centroid.Y:F2}, {volumeProps.Centroid.Z:F2})");
            }
        }
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}