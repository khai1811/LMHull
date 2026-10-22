using System;
using System.Collections.Generic;
using LMHull.Rhino.Features.Structure.Services;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMCreateCompartmentsCommand : Command
{
    public override string EnglishName => "LMCreateCompartments";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn thân tàu kín
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Chọn thân tàu kín (Solid Brep) để phân chia khoang rỗng");
        getObj.GeometryFilter = ObjectType.Brep | ObjectType.PolysrfFilter;
        getObj.Get();

        if (getObj.CommandResult() != Result.Success)
            return getObj.CommandResult();

        Brep? hull = getObj.Object(0).Brep();
        if (hull == null || !hull.IsSolid)
        {
            RhinoApp.WriteLine("[Lỗi]: Vui lòng chọn một khối kín (Solid Brep) để thực hiện chia khoang.");
            return Result.Failure;
        }

        // 2. Nhập số lượng khoang muốn chia
        var getNum = new GetInteger();
        getNum.SetCommandPrompt("Nhập tổng số khoang chứa cần chia");
        getNum.SetDefaultInteger(5);
        getNum.SetLowerLimit(2, true);
        getNum.SetUpperLimit(20, true);
        getNum.Get();
        if (getNum.CommandResult() != Result.Success) return getNum.CommandResult();
        int totalTanks = getNum.Number();

        // 3. Tính toán các vị trí vách phân chia
        BoundingBox bbox = hull.GetBoundingBox(true);
        double minX = bbox.Min.X;
        double maxX = bbox.Max.X;
        double dx = (maxX - minX) / totalTanks;

        var xPositions = new List<double>();
        for (int i = 1; i < totalTanks; i++)
        {
            xPositions.Add(minX + i * dx);
        }

        RhinoApp.WriteLine("[LMCompartments] Đang chia khối thân tàu thành các khoang kín...");
        var compartments = CompartmentService.SplitIntoCompartments(hull, xPositions.ToArray(), doc.ModelAbsoluteTolerance);

        if (compartments.Count == 0)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể phân chia khoang trên hình học này.");
            return Result.Failure;
        }

        CompartmentService.BakeCompartments(doc, compartments);

        // 4. In bảng thông số dung tích và trọng tâm từng khoang
        RhinoApp.WriteLine("==========================================================================");
        RhinoApp.WriteLine(" [LMCompartments] THỐNG KÊ DUNG TÍCH KHOANG CHỨA:");
        RhinoApp.WriteLine(string.Format("{0,-18} | {1,12} | {2,22}", "Tên Khoang", "Thể tích (m³)", "Trọng tâm CG (X, Y, Z)"));
        RhinoApp.WriteLine(new string('-', 60));

        double totalVol = 0;
        foreach (var c in compartments)
        {
            RhinoApp.WriteLine(string.Format("{0,-18} | {1,12:F2} | ({2:F2}, {3:F2}, {4:F2})",
                c.Name, c.Volume, c.CenterOfGravity.X, c.CenterOfGravity.Y, c.CenterOfGravity.Z));
            totalVol += c.Volume;
        }

        RhinoApp.WriteLine(new string('-', 60));
        RhinoApp.WriteLine($" Tổng dung tích toàn bộ khoang: {totalVol:F2} m³");
        RhinoApp.WriteLine(" Đã tạo cây Layer: 'LMHull_Compartments'");
        RhinoApp.WriteLine("==========================================================================");

        return Result.Success;
    }
}