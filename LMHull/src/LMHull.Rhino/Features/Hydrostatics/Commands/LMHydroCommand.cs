using System;
using LMHull.Core.Calculation;
using LMHull.Core.Reporting;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Hydrostatics.Calculations;
using LMHull.Rhino.Features.Hydrostatics.Models;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Infrastructure;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMHydroCommand : Command
{
    // Tên lệnh người dùng sẽ gõ trên dòng lệnh Rhino
    public override string EnglishName => "LMHydro";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // ---------------------------------------------------------------------
        // BƯỚC 1: NGƯỜI DÙNG CHỌN VỎ TÀU
        // ---------------------------------------------------------------------
        var picked = RhinoInput.PickBrep("Chọn vỏ tàu cần tính toán thủy tĩnh (Brep / Polysurface)", out Brep hullBrep, out ObjRef objRef);
        if (picked != Result.Success) return picked;

        // Lấy thông số bao hình học của tàu được chọn
        BoundingBox bbox = hullBrep.GetBoundingBox(true);
        double totalLength = bbox.Max.X - bbox.Min.X;
        double totalBeam = bbox.Max.Y - bbox.Min.Y;
        double totalDepth = bbox.Max.Z - bbox.Min.Z;

        RhinoApp.WriteLine($"[LMHydro] Đã chọn: {objRef.Object().Name ?? "Vỏ tàu"} | Kích thước bao: L={totalLength:F2}m, B={totalBeam:F2}m, D={totalDepth:F2}m");

        // ---------------------------------------------------------------------
        // BƯỚC 2: NGƯỜI DÙNG NHẬP CHIỀU CHÌM / MỚN NƯỚC T
        // ---------------------------------------------------------------------
        // Mặc định bằng 60% chiều cao mạn và giới hạn trong chiều cao mạn
        double defaultDraft = Math.Round(totalDepth * 0.6, 2);
        var gotDraft = RhinoInput.GetNumber("Nhập chiều chìm thiết kế T (mớn nước)", defaultDraft, 0.1, totalDepth, out double draft);
        if (gotDraft != Result.Success) return gotDraft;

        // ---------------------------------------------------------------------
        // BƯỚC 3: TÍNH THỦY TĨNH (dùng chung HydrostaticCalculation với panel)
        // ---------------------------------------------------------------------
        double totalVolume = 0.0;
        if (hullBrep.IsSolid)
        {
            totalVolume = VolumeMassProperties.Compute(hullBrep)?.Volume ?? 0.0;
        }

        var input = new HydrostaticInput(hullBrep, draft, ServiceRegistry.Settings.SeaWaterDensity, doc.ModelAbsoluteTolerance);
        CalculationResult<DetailedHydroResult> result;
        using (var progress = new RhinoProgressReporter("Thủy tĩnh"))
        {
            result = new HydrostaticCalculation().Run(input, progress);
        }
        if (!result.IsSuccess || result.Value == null)
        {
            ServiceRegistry.Logger.Error("[LMHydro] " + result.GetErrorMessage());
            return Result.Failure;
        }

        DetailedHydroResult hydro = result.Value;
        double waterLevelZ = bbox.Min.Z + draft;

        // ---------------------------------------------------------------------
        // BƯỚC 4: XUẤT KẾT QUẢ VÀO BẢNG "LMHull Results"
        // ---------------------------------------------------------------------
        string hullName = objRef.Object().Name;
        var table = ResultTable.KeyValue($"Thủy tĩnh – {(string.IsNullOrWhiteSpace(hullName) ? "Vỏ tàu" : hullName)}", "LMHydro")
            .AddRow("Trạng thái vỏ tàu", hullBrep.IsSolid ? "SOLID (Kín)" : "OPEN (Chưa kín)", "")
            .AddRow("Chiều chìm T", draft, "F2", "m")
            .AddRow("Cao độ mặt nước Z", waterLevelZ, "F2", "m");
        if (totalVolume > 0) table.AddRow("Thể tích toàn bộ V", totalVolume, "F2", "m³");
        table.AddRow("Thể tích phần chìm V", hydro.SubmergedVolume, "F2", "m³")
            .AddRow("Lượng chiếm nước Δ (nước biển)", hydro.Displacement, "F2", "t")
            .AddRow("Diện tích đường nước Aw", hydro.WaterplaneArea, "F2", "m²")
            .AddRow("Hệ số béo Cb", hydro.BlockCoefficient, "F3")
            .AddRow("Cao độ tâm nổi KB", hydro.CenterOfBuoyancy.Z - bbox.Min.Z, "F2", "m")
            .AddRow("Tỷ số L/B", totalLength / totalBeam, "F2")
            .AddRow("Tỷ số B/T", totalBeam / draft, "F2");
        table.Notes.Add($"Kích thước bao: L = {totalLength:F2} m, B = {totalBeam:F2} m, D = {totalDepth:F2} m");
        ResultsHub.Publish(table);

        return Result.Success;
    }
}
