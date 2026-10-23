using System;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMGeneratePlatesCommand : Command
{
    public override string EnglishName => "LMGeneratePlates";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var picked = RhinoInput.PickBrep("Chọn mặt tôn vỏ/vách phẳng để gán bề dày đặc (Solid Plate)", out Brep sourceBrep,
            filter: ObjectType.Surface | ObjectType.Brep);
        if (picked != Result.Success) return picked;

        var gotThick = RhinoInput.GetNumber("Nhập chiều dày tôn danh định (mm)", ServiceRegistry.Settings.DefaultThicknessMm, 1.0, 100.0, out double thicknessMm);
        if (gotThick != Result.Success) return gotThick;

        RhinoApp.WriteLine($"[LMPlate] Đang tạo tôn đặc dày {thicknessMm:F1} mm...");
        Brep? solidPlate = PlateGenerator.CreateSolidPlate(sourceBrep, thicknessMm, offsetInward: true, doc.ModelAbsoluteTolerance);

        if (solidPlate == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể tạo khối tấm tôn đặc từ mặt này.");
            return Result.Failure;
        }

        var cad = RhinoCadHandle.For(doc);
        cad.AddBrep(solidPlate, StructureLayers.SolidPlates.Spec($"Plate_{thicknessMm:F1}mm"));
        cad.Redraw();

        RhinoApp.WriteLine($"[Thành công]: Đã sinh tấm tôn đặc khối kín (Solid) dày {thicknessMm:F1} mm vào layer 'LMHull_SolidPlates'.");
        return Result.Success;
    }
}