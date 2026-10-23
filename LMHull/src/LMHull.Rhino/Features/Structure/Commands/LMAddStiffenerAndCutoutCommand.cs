using System;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMAddStiffenerAndCutoutCommand : Command
{
    public override string EnglishName => "LMAddStructureDetails";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var gotOpt = RhinoInput.GetOption("Chọn tính năng gia công chi tiết kết cấu", new[] { "ThemNep_Stiffener", "KhoetLo_Cutout" }, out int option);
        if (gotOpt != Result.Success) return gotOpt;

        if (option == 0)
        {
            // 1. Thêm nẹp gia cường
            var pickedCrv = RhinoInput.PickCurve("Chọn đường cong dẫn (Guide Curve) trên mặt vách/boong", out Curve guide);
            if (pickedCrv != Result.Success) return pickedCrv;

            var stiffenerBrep = StiffenerAndCutoutService.CreateFlatBarStiffener(guide, webHeightMm: 160.0, webThicknessMm: 12.0, doc.ModelAbsoluteTolerance);
            if (stiffenerBrep == null)
            {
                RhinoApp.WriteLine("[Lỗi]: Không thể tạo nẹp kết cấu từ đường dẫn này.");
                return Result.Failure;
            }

            var cad = RhinoCadHandle.For(doc);
            cad.AddBrep(stiffenerBrep, StructureLayers.Stiffeners.Spec("Stiffener_FB160x12"));
            cad.Redraw();

            RhinoApp.WriteLine("[Thành công]: Đã tạo nẹp gia cường Flat Bar (FB 160x12) thành công.");
            return Result.Success;
        }
        else
        {
            // 2. Khoét lỗ công nghệ
            var pickedPlate = RhinoInput.PickBrep("Chọn tấm vách ngăn (Brep) cần khoét lỗ", out Brep plate, out ObjRef objRef);
            if (pickedPlate != Result.Success) return pickedPlate;

            var getPt = new GetPoint();
            getPt.SetCommandPrompt("Pick điểm tâm lỗ khoét trên vách");
            getPt.Get();
            if (getPt.CommandResult() != Result.Success) return getPt.CommandResult();

            Point3d pt = getPt.Point();
            RhinoApp.WriteLine("[LMCutout] Đang trừ khối khoét lỗ công nghệ...");

            var modifiedPlate = StiffenerAndCutoutService.CreateLighteningCutout(plate, pt, radiusX: 0.35, radiusY: 0.55, doc.ModelAbsoluteTolerance);
            if (modifiedPlate == null)
            {
                RhinoApp.WriteLine("[Cảnh báo]: Không thể khoét lỗ tại vị trí này (điểm nằm ngoài vách hoặc sai hướng).");
                return Result.Failure;
            }

            // Thay thế hình học cũ bằng tấm vách đã khoét lỗ
            var cad = RhinoCadHandle.For(doc);
            cad.Replace(objRef.ObjectId, modifiedPlate);
            cad.Redraw();

            RhinoApp.WriteLine("[Thành công]: Đã khoét lỗ giảm trọng lượng Oval thành công trên vách.");
            return Result.Success;
        }
    }
}