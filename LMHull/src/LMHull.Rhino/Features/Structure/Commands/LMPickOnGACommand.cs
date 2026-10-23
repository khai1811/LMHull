using LMHull.Core.Enums;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Commands;

public class LMPickOnGACommand : Command
{
    private static readonly (string Option, StructuralNodeType Kind, string CurvePrompt)[] Kinds =
    {
        ("VachNgang_Bulkhead", StructuralNodeType.TransverseBulkhead, "Pick nét vách ngang 2D trên bản vẽ GA (Đường thẳng đứng X):"),
        ("MatBoong_Deck", StructuralNodeType.DeckPlate, "Pick nét cao độ boong 2D trên bản vẽ GA (Đường ngang Z):"),
        ("VachDoc_LongBulkhead", StructuralNodeType.LongitudinalBulkhead, "Pick nét vách dọc 2D trên mặt bằng GA (Đường dọc Y):")
    };

    public override string EnglishName => "LMPickOnGA";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Chọn khối thân tàu 3D
        var picked = RhinoInput.PickBrep("Chọn khối thân tàu 3D (Solid Brep)", out Brep hullSolid, requireSolid: true);
        if (picked != Result.Success) return picked;

        // 2. Chọn loại kết cấu muốn tạo từ bản vẽ 2D
        var gotOpt = RhinoInput.GetOption("Chọn loại kết cấu cần chiếu dựng từ bản vẽ 2D", System.Array.ConvertAll(Kinds, k => k.Option), out int option);
        if (gotOpt != Result.Success) return gotOpt;
        var (_, kind, curvePrompt) = Kinds[option];

        // 3. Chọn nét vẽ 2D (Trace line) trên bản vẽ GA
        var pickedCrv = RhinoInput.PickCurve(curvePrompt, out Curve traceCurve);
        if (pickedCrv != Result.Success) return pickedCrv;

        RhinoApp.WriteLine("[LMPickOnGA] Đang tính toán hình chiếu và cắt gọt tự động...");

        Brep? generatedPlate = GAPlacementService.CreateFromGA(hullSolid, traceCurve, kind, doc.ModelAbsoluteTolerance);
        if (generatedPlate == null)
        {
            RhinoApp.WriteLine("[Cảnh báo]: Nét vẽ 2D nằm ngoài phạm vi bao của thân tàu hoặc không cắt được.");
            return Result.Failure;
        }

        Point3d at = traceCurve.PointAtStart;
        string name = kind switch
        {
            StructuralNodeType.DeckPlate => $"Deck_Z{at.Z:F1}",
            StructuralNodeType.LongitudinalBulkhead => $"LongBulkhead_Y{at.Y:F1}",
            _ => $"Bulkhead_X{at.X:F1}"
        };
        GAPlacementService.Bake(RhinoCadHandle.For(doc), generatedPlate, kind, name);

        RhinoApp.WriteLine($"[Thành công]: Đã tự động tạo kết cấu 3D từ nét vẽ 2D tại vị trí (X={at.X:F2}, Y={at.Y:F2}, Z={at.Z:F2}).");
        return Result.Success;
    }
}
