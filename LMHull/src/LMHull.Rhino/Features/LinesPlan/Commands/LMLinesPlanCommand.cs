using System;
using LMHull.Rhino.Features.LinesPlan.Services;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Display;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.LinesPlan.Commands;

public class LMLinesPlanCommand : Command
{
    public override string EnglishName => "LMLinesPlan";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn vỏ tàu
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Chọn vỏ tàu Brep để trích xuất tuyến hình (Lines Plan)");
        getObj.GeometryFilter = ObjectType.Brep | ObjectType.PolysrfFilter;
        getObj.Get();

        if (getObj.CommandResult() != Result.Success)
            return getObj.CommandResult();

        Brep? hull = getObj.Object(0).Brep();
        if (hull == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể đọc hình học Brep.");
            return Result.Failure;
        }

        // 2. Nhập số lượng mặt cắt sườn
        var getNum = new GetInteger();
        getNum.SetCommandPrompt("Nhập số mặt cắt sườn (Stations)");
        getNum.SetDefaultInteger(11);
        getNum.SetLowerLimit(3, true);
        getNum.SetUpperLimit(51, true);
        getNum.Get();
        if (getNum.CommandResult() != Result.Success) return getNum.CommandResult();
        int stations = getNum.Number();

        RhinoApp.WriteLine("[LMLinesPlan] Đang tính toán giao tuyến các mặt phẳng cắt...");

        // 3. Tính toán và đẩy vào hệ thống layer
        var linesResult = LinesPlanGenerator.GenerateCurves(hull, stationCount: stations, waterlineCount: 6, buttockCount: 3, tolerance: doc.ModelAbsoluteTolerance);
        LinesPlanGenerator.BakeToDocument(doc, linesResult);

        // 4. Tạo tự động một Layout Page A3 nằm ngang (420 x 297 mm)
        string pageName = "Lines Plan A3";
        var pageView = doc.Views.Find(pageName, false) as RhinoPageView; if (pageView == null)
        {
            pageView = doc.Views.AddPageView(pageName, 420, 297);
            if (pageView != null)
            {
                // Thêm một khung nhìn Detail View vào trang in
                Point2d p0 = new Point2d(10, 10);
                Point2d p1 = new Point2d(410, 287);
                var detail = pageView.AddDetailView("MainView", p0, p1, DefinedViewportProjection.Perspective); if (detail != null)
                {
                    detail.CommitViewportChanges();
                }
            }
        }

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine(" [LMLinesPlan] TRÍCH XUẤT TUYẾN HÌNH HOÀN TẤT:");
        RhinoApp.WriteLine($" - Số đường sườn (Stations)       : {linesResult.StationCurves.Count}");
        RhinoApp.WriteLine($" - Số đường nước (Waterlines)     : {linesResult.WaterlineCurves.Count}");
        RhinoApp.WriteLine($" - Số đường mông (Buttocks)       : {linesResult.ButtockCurves.Count}");
        RhinoApp.WriteLine(" - Đã tạo trang in Layout         : 'Lines Plan A3'");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}