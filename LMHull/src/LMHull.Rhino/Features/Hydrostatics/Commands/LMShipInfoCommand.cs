using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Reporting;
using LMHull.Core.Services;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Infrastructure;
using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMShipInfoCommand : Command
{
    // Tên lệnh người dùng sẽ gõ trên Command Line của Rhino
    public override string EnglishName => "ShipInfo";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // 1. Tạo dữ liệu đầu vào sà lan mẫu sử dụng lớp ShipInput từ LMHull.Core
        var defaultBarge = new ShipInput("Sà lan mẫu v0.3", length: 45.0, beam: 10.0, draft: 2.5, depth: 3.5);

        // 2. Gọi bộ tính toán ShipCalculator từ LMHull.Core.dll
        var calculator = new ShipCalculator(ServiceRegistry.Settings);
        ShipResult result = calculator.Calculate(defaultBarge, WaterType.SeaWater);

        // 3. Xuất kết quả vào bảng "LMHull Results"
        var table = ResultTable.KeyValue($"Thông số: {result.Input.Name}", "ShipInfo")
            .AddRow("Chiều dài L", defaultBarge.Length, "F2", "m")
            .AddRow("Chiều rộng B", defaultBarge.Beam, "F2", "m")
            .AddRow("Chiều chìm T", defaultBarge.Draft, "F2", "m")
            .AddRow("Tỷ số L/B", result.LBRatio, "F2")
            .AddRow("Tỷ số B/T", result.BTRatio, "F2")
            .AddRow("Thể tích V", result.BoxVolume, "F2", "m³")
            .AddRow("Lượng chiếm nước Δ (nước biển)", result.Displacement, "F2", "t");
        ResultsHub.Publish(table);

        return Result.Success;
    }
}