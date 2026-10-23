using System.Linq;
using Eto.Forms;
using LMHull.Rhino.UI.Common;
using Rhino;

namespace LMHull.Rhino.UI;

/// <summary>
/// Tab "Lệnh": nút bấm cho mọi lệnh LMHull, chia theo nhóm, để không phải nhớ tên lệnh.
/// </summary>
public class CommandLauncherView : Panel
{
    private static readonly (string Group, string Label, string Command, string Tooltip)[] Commands =
    {
        ("Dựng vỏ tàu", "Sà lan hộp", "LMCreateBarge", "Dựng sà lan hình hộp theo L, B, D"),
        ("Dựng vỏ tàu", "Vỏ NURBS", "LMCreateNurbsHull", "Dựng vỏ tàu NURBS từ đường sườn"),
        ("Dựng vỏ tàu", "Vỏ đối xứng", "LMCreateSymmetricHull", "Dựng khối vỏ kín đối xứng qua mặt dọc tâm"),
        ("Dựng vỏ tàu", "Khâu kín vỏ", "LMCloseHull", "Đóng nắp và khâu kín vỏ thành khối Solid"),
        ("Dựng vỏ tàu", "Chuẩn hóa tọa độ", "LMAlignHull", "Đưa vỏ tàu về AP / Baseline / Centerline"),
        ("Dựng vỏ tàu", "Đường tham chiếu", "LMCreateRefLines", "Tạo hệ đường tham chiếu (sườn, nước, dọc)"),

        ("Thủy tĩnh & tuyến hình", "Tính thủy tĩnh", "LMHydro", "Tính V, Δ, Aw, Cb, KB tại mớn nước T"),
        ("Thủy tĩnh & tuyến hình", "Đường cong thủy tĩnh", "LMHydroCurves", "Bảng và đồ thị V, Δ, Aw, TPC, Cb, KB, LCB, LCF theo dải mớn nước"),
        ("Thủy tĩnh & tuyến hình", "Thông số tàu mẫu", "ShipInfo", "Bảng thông số sơ bộ của sà lan mẫu"),
        ("Thủy tĩnh & tuyến hình", "Xuất tuyến hình", "LMLinesPlan", "Trích xuất đường sườn, đường nước, đường dọc"),
        ("Thủy tĩnh & tuyến hình", "Hộp thoại nhập tàu", "LMDialog", "Nhập L, B, T, D và dựng sà lan"),

        ("Kết cấu", "Vách & khung sườn", "LMCreateStructure", "Bố trí vách ngang và sườn gia cường"),
        ("Kết cấu", "Chia khoang", "LMCreateCompartments", "Chia thân tàu thành các khoang, tính dung tích"),
        ("Kết cấu", "Bảng dung tích két", "LMTankSounding", "Thể tích, % đầy và VCG theo chiều cao chất lỏng"),
        ("Kết cấu", "Dựng từ GA", "LMPickOnGA", "Dựng vách / boong từ nét vẽ 2D trên GA"),
        ("Kết cấu", "Tôn đặc", "LMGeneratePlates", "Tạo tấm tôn đặc có bề dày từ mặt"),
        ("Kết cấu", "Nẹp & khoét lỗ", "LMAddStructureDetails", "Thêm nẹp gia cường hoặc khoét lỗ giảm trọng lượng"),
        ("Kết cấu", "Trọng lượng kết cấu", "LMStructureWeight", "Tổng trọng lượng, trọng tâm CoG và bảng theo layer"),

        ("Hệ thống", "Cấu hình", "LMSettings", "Tỷ trọng nước, vật liệu / bề dày mặc định, thư mục xuất")
    };

    public CommandLauncherView()
    {
        var layout = new DynamicLayout { Padding = new Eto.Drawing.Padding(6), Spacing = UiMetrics.Spacing };

        foreach (var group in Commands.GroupBy(c => c.Group))
        {
            var groupLayout = new DynamicLayout { Spacing = UiMetrics.Spacing };
            foreach (var cmd in group)
            {
                var button = new Button { Text = cmd.Label, ToolTip = $"{cmd.Tooltip}  ({cmd.Command})" };
                string commandName = cmd.Command;
                button.Click += (_, _) => RunCommand(commandName);
                groupLayout.AddRow(button);
            }

            layout.AddRow(new GroupBox { Text = group.Key, Padding = new Eto.Drawing.Padding(6), Content = groupLayout });
        }

        layout.AddRow(null);
        Content = new Scrollable { Content = layout, Border = BorderType.None };
    }

    // "!" hủy lệnh đang chạy dở; "_" dùng tên lệnh tiếng Anh bất kể ngôn ngữ giao diện Rhino
    private static void RunCommand(string commandName) => RhinoApp.RunScript("!_" + commandName, true);
}
