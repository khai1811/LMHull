using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
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
        var calculator = new ShipCalculator();
        ShipResult result = calculator.Calculate(defaultBarge, WaterType.SeaWater);

        // 3. In kết quả ra thanh Command History của Rhino bằng RhinoApp.WriteLine
        RhinoApp.WriteLine("==================================================");
        RhinoApp.WriteLine($" [LMHull.Rhino v0.3] THÔNG SỐ: {result.Input.Name}");
        RhinoApp.WriteLine($" - Kích thước : L = {defaultBarge.Length}m | B = {defaultBarge.Beam}m | T = {defaultBarge.Draft}m");
        RhinoApp.WriteLine($" - Tỷ số      : L/B = {result.LBRatio:F2} | B/T = {result.BTRatio:F2}");
        RhinoApp.WriteLine($" - Thể tích V : {result.BoxVolume:F2} m3");
        RhinoApp.WriteLine($" - Lượng ch.n : {result.Displacement:F2} Tấn (Nước biển)");
        RhinoApp.WriteLine("==================================================");

        return Result.Success;
    }
}