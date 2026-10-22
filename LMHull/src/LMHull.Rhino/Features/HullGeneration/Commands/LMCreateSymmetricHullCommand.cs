using System;
using System.Drawing;
using LMHull.Core.Models;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.HullGeneration.Commands;

public class LMCreateSymmetricHullCommand : Command
{
    public override string EnglishName => "LMCreateSymmetricHull";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var ship = new ShipInput("Vỏ tàu đối xứng v0.8", length: 50.0, beam: 10.0, draft: 2.5, depth: 4.0);

        // Dựng vỏ bằng phương pháp nửa mạn + đối xứng qua Centerline
        Brep? solidHull = SymmetricHullBuilder.BuildSymmetricSolidHull(ship, stationCount: 7, doc.ModelAbsoluteTolerance);

        if (solidHull == null)
        {
            RhinoApp.WriteLine("[Lỗi]: Không thể tạo vỏ tàu đối xứng.");
            return Result.Failure;
        }

        LayerManager.EnsureStandardLayers(doc);
        int hullLayerIndex = LayerManager.GetOrCreateLayer(doc, LayerManager.HullLayerName, Color.FromArgb(200, 40, 40));

        var attr = new ObjectAttributes
        {
            LayerIndex = hullLayerIndex,
            Name = ship.Name
        };

        Guid hullGuid = doc.Objects.AddBrep(solidHull, attr);
        LMHullPlugin.Instance.LastHullGuid = hullGuid;

        doc.Views.Redraw();

        BoundingBox bbox = solidHull.GetBoundingBox(true);
        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine($" [LMCreateSymmetricHull] Đã dựng vỏ tàu đối xứng thành công!");
        RhinoApp.WriteLine($" - Mã Guid vỏ tàu       : {hullGuid}");
        RhinoApp.WriteLine($" - Trạng thái khối kín  : {(solidHull.IsSolid ? "SOLID (Kín tuyệt đối)" : "OPEN (Chưa kín)")}");
        RhinoApp.WriteLine($" - Hộp bao hình học (BoundingBox):");
        RhinoApp.WriteLine($"   + Dọc (X) : {bbox.Min.X:F2}m -> {bbox.Max.X:F2}m (L = {bbox.Max.X - bbox.Min.X:F2}m)");
        RhinoApp.WriteLine($"   + Ngang(Y): {bbox.Min.Y:F2}m -> {bbox.Max.Y:F2}m (B = {bbox.Max.Y - bbox.Min.Y:F2}m)");
        RhinoApp.WriteLine($"   + Đứng (Z): {bbox.Min.Z:F2}m -> {bbox.Max.Z:F2}m (D = {bbox.Max.Z - bbox.Min.Z:F2}m)");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}