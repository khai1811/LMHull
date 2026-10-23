using System;
using System.Globalization;
using LMHull.Core.Calculation;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.LinesPlan.Calculations;
using LMHull.Rhino.Features.LinesPlan.Models;
using LMHull.Rhino.Features.LinesPlan.Services;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.LinesPlan.Commands;

public class LMLinesPlanCommand : Command
{
    public override string EnglishName => "LMLinesPlan";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn vỏ tàu
        var picked = RhinoInput.PickBrep("Chọn vỏ tàu Brep để trích xuất tuyến hình (Lines Plan)", out Brep hull);
        if (picked != Result.Success) return picked;

        // 2. Nhập số lượng mặt cắt sườn
        var gotNum = RhinoInput.GetInteger("Nhập số mặt cắt sườn (Stations)", 11, 3, 51, out int stations);
        if (gotNum != Result.Success) return gotNum;

        RhinoApp.WriteLine("[LMLinesPlan] Đang tính toán giao tuyến các mặt phẳng cắt...");

        // 3. Tính toán và đẩy vào hệ thống layer
        var input = new LinesPlanInput(hull, stations, WaterlineCount: 6, ButtockCount: 3, doc.ModelAbsoluteTolerance);
        CalculationResult<LinesPlanResult> calcResult;
        using (var progress = new RhinoProgressReporter("Tuyến hình"))
        {
            calcResult = new LinesPlanCalculation().Run(input, progress);
        }

        if (!calcResult.IsSuccess || calcResult.Value == null)
        {
            ServiceRegistry.Logger.Error("[LMLinesPlan] " + calcResult.GetErrorMessage());
            return Result.Failure;
        }

        var linesResult = calcResult.Value;
        LinesPlanGenerator.BakeToDocument(RhinoCadHandle.For(doc), linesResult);

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

        var table = ResultTable.KeyValue("Tuyến hình", "LMLinesPlan")
            .AddRow("Số đường sườn (Stations)", linesResult.StationCurves.Count.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Số đường nước (Waterlines)", linesResult.WaterlineCurves.Count.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Số đường mông (Buttocks)", linesResult.ButtockCurves.Count.ToString(CultureInfo.InvariantCulture), "")
            .AddRow("Trang in Layout", pageName, "")
            .AddRow("Thời gian tính", calcResult.Elapsed.TotalSeconds, "F2", "s");
        ResultsHub.Publish(table);

        return Result.Success;
    }
}