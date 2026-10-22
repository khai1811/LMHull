using System;
using System.Collections.Generic;
using LMHull.Rhino.Features.LinesPlan.Services;
using LMHull.Rhino.Features.Structure.Services;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMCreateStructureCommand : Command
{
    public override string EnglishName => "LMCreateStructure";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn thân tàu kín
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Chọn thân tàu kín (Solid Brep) để bố trí vách và khung sườn");
        getObj.GeometryFilter = ObjectType.Brep | ObjectType.PolysrfFilter;
        getObj.Get();

        if (getObj.CommandResult() != Result.Success)
            return getObj.CommandResult();

        Brep? hull = getObj.Object(0).Brep();
        if (hull == null || !hull.IsSolid)
        {
            RhinoApp.WriteLine("[Cảnh báo]: Đối tượng được chọn phải là khối kín (Solid Brep) để cắt vách chính xác.");
            return Result.Failure;
        }

        // 2. Nhập số lượng vách ngang
        var getNum = new GetInteger();
        getNum.SetCommandPrompt("Nhập số lượng vách ngang kín nước (Bulkheads)");
        getNum.SetDefaultInteger(4);
        getNum.SetLowerLimit(1, true);
        getNum.SetUpperLimit(20, true);
        getNum.Get();
        if (getNum.CommandResult() != Result.Success) return getNum.CommandResult();
        int bhCount = getNum.Number();

        BoundingBox bbox = hull.GetBoundingBox(true);
        double minX = bbox.Min.X;
        double maxX = bbox.Max.X;
        double step = (maxX - minX) / (bhCount + 1);

        var xPositions = new List<double>();
        for (int i = 1; i <= bhCount; i++)
        {
            xPositions.Add(minX + i * step);
        }

        RhinoApp.WriteLine("[LMStructure] Đang cắt tiết diện vách ngăn kín nước...");
        var bulkheads = InternalStructureGenerator.CreateBulkheads(hull, xPositions.ToArray(), doc.ModelAbsoluteTolerance);

        // 3. Trích xuất đường sườn và tạo bản thành khung sườn
        var linesResult = LinesPlanGenerator.GenerateCurves(hull, stationCount: 9, waterlineCount: 0, buttockCount: 0, tolerance: doc.ModelAbsoluteTolerance);
        var ribCurves = InternalStructureGenerator.CreateFrameOffsets(linesResult.StationCurves, frameDepth: 0.35, doc.ModelAbsoluteTolerance);

        var structResult = new StructureResult();
        structResult.Bulkheads.AddRange(bulkheads);
        structResult.FrameInnerCurves.AddRange(ribCurves);

        InternalStructureGenerator.BakeStructure(doc, structResult);

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine(" [LMCreateStructure] BỐ TRÍ KẾT CẤU HOÀN TẤT:");
        RhinoApp.WriteLine($" - Số lượng vách ngăn (Bulkheads)   : {bulkheads.Count}");
        RhinoApp.WriteLine($" - Số đường sườn gia cường (Ribs)   : {ribCurves.Count}");
        RhinoApp.WriteLine(" - Đã gán vào Layer                : 'LMHull_Structure'");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}