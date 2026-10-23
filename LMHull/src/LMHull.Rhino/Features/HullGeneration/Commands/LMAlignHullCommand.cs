using System;
using LMHull.Rhino.Common;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMAlignHullCommand : Command
{
    public override string EnglishName => "LMAlignHull";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Cho phép người dùng chọn vỏ tàu cần đưa về hệ tọa độ chuẩn
        var picked = RhinoInput.PickBrep("Chọn vỏ tàu (Brep) cần chuẩn hóa hệ tọa độ", out Brep brep, out ObjRef objRef);
        if (picked != Result.Success) return picked;

        // 2. Tính toán ma trận biến đổi tịnh tiến về chuẩn AP, Baseline, Centerline
        Transform xform = ShipCoordinateTransformer.CreateAlignmentTransform(brep);

        // In tọa độ hộp bao cũ trước khi chuyển đổi
        BoundingBox oldBox = brep.GetBoundingBox(true);

        // 3. Áp dụng biến đổi trực tiếp lên đối tượng trong tài liệu
        var cad = RhinoCadHandle.For(doc);
        cad.Transform(objRef.ObjectId, xform);
        cad.Redraw();

        // Lấy tọa độ hộp bao mới
        BoundingBox newBox = cad.GetGeometry<GeometryBase>(objRef.ObjectId)!.GetBoundingBox(true);

        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine(" [LMAlignHull] ĐÃ CHUẨN HÓA HỆ TỌA ĐỘ VỎ TÀU:");
        RhinoApp.WriteLine($" - Tọa độ cũ : X[{oldBox.Min.X:F1}..{oldBox.Max.X:F1}], Y[{oldBox.Min.Y:F1}..{oldBox.Max.Y:F1}], Z[{oldBox.Min.Z:F1}..{oldBox.Max.Z:F1}]");
        RhinoApp.WriteLine($" - Tọa độ mới: X[{newBox.Min.X:F1}..{newBox.Max.X:F1}], Y[{newBox.Min.Y:F1}..{newBox.Max.Y:F1}], Z[{newBox.Min.Z:F1}..{newBox.Max.Z:F1}]");
        RhinoApp.WriteLine("   -> Đuôi tàu (AP) trùng X = 0");
        RhinoApp.WriteLine("   -> Mặt đối xứng trùng Centerline (Y = 0)");
        RhinoApp.WriteLine("   -> Điểm thấp nhất trùng Baseline (Z = 0)");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}