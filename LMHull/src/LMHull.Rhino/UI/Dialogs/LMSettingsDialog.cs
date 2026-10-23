using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.UI.Common;
using Rhino.Geometry;

namespace LMHull.Rhino.UI.Dialogs;

/// <summary>
/// Hộp thoại cấu hình LMHull: tỷ trọng nước, vật liệu / bề dày mặc định, thư mục xuất, số bảng kết quả.
/// Trả về cấu hình mới khi bấm Lưu, null khi Hủy.
/// </summary>
public class LMSettingsDialog : Dialog<LMHullSettings?>
{
    private readonly LabeledNumericInput _seaDensity;
    private readonly LabeledNumericInput _freshDensity;
    private readonly LabeledDropdown _material;
    private readonly LabeledNumericInput _thickness;
    private readonly FilePickerInput _exportFolder;
    private readonly LabeledNumericInput _maxHistory;
    private readonly Coordinate3DInput _referenceOrigin;

    // Giới hạn vùng tính được chỉnh ở tab Thủy tĩnh, hộp thoại này giữ nguyên giá trị
    private readonly CalculationLimits _limits;

    public LMSettingsDialog(LMHullSettings current)
    {
        _limits = current.Limits;
        Title = "LMHull - Cấu hình";
        Padding = new Padding(12);
        Resizable = false;

        _seaDensity = new LabeledNumericInput("Nước biển:", "t/m³", 0.9, 1.1, 3, current.SeaWaterDensity) { Increment = 0.001 };
        _freshDensity = new LabeledNumericInput("Nước ngọt:", "t/m³", 0.9, 1.1, 3, current.FreshWaterDensity) { Increment = 0.001 };
        _material = new LabeledDropdown("Vật liệu:", MaterialRegistry.AvailableMaterials.Select(m => m.Name)) { SelectedText = current.DefaultMaterial };
        _thickness = new LabeledNumericInput("Bề dày tôn:", "mm", 1, 100, 1, current.DefaultThicknessMm) { Increment = 0.5 };
        _exportFolder = new FilePickerInput("Thư mục xuất:") { SelectFolder = true, FilePath = current.ExportFolder };
        _maxHistory = new LabeledNumericInput("Số bảng tối đa:", "", 1, 200, 0, current.MaxResultHistory);
        _referenceOrigin = new Coordinate3DInput("Gốc tham chiếu:", "m", 2)
        {
            Value = new Point3d(current.ReferenceX, current.ReferenceY, current.ReferenceZ)
        };

        var btnSave = new Button { Text = "Lưu" };
        btnSave.Click += (_, _) => Close(Collect());
        var btnCancel = new Button { Text = "Hủy" };
        btnCancel.Click += (_, _) => Close(null);
        DefaultButton = btnSave;
        AbortButton = btnCancel;

        var layout = new DynamicLayout { Spacing = new Size(6, 6) };
        layout.AddRow(Group("Tỷ trọng nước", _seaDensity, _freshDensity));
        layout.AddRow(Group("Kết cấu mặc định", _material, _thickness));
        layout.AddRow(Group("Xuất dữ liệu & kết quả", _exportFolder, _maxHistory));
        layout.AddRow(Group("Gốc đo mô men (LCB / LCF / LCG)", _referenceOrigin));
        layout.AddRow(null);
        layout.AddRow(new TableLayout { Spacing = new Size(6, 6), Rows = { new TableRow(null, btnSave, btnCancel) } });

        Content = layout;
        Width = 520;
    }

    private LMHullSettings Collect() => new()
    {
        SeaWaterDensity = _seaDensity.Value,
        FreshWaterDensity = _freshDensity.Value,
        DefaultMaterial = _material.SelectedText ?? new LMHullSettings().DefaultMaterial,
        DefaultThicknessMm = _thickness.Value,
        ExportFolder = _exportFolder.FilePath,
        MaxResultHistory = (int)_maxHistory.Value,
        ReferenceX = _referenceOrigin.Value.X,
        ReferenceY = _referenceOrigin.Value.Y,
        ReferenceZ = _referenceOrigin.Value.Z,
        Limits = _limits
    };

    private static GroupBox Group(string title, params Control[] rows)
    {
        var layout = new DynamicLayout { Spacing = UiMetrics.Spacing };
        foreach (var row in rows) layout.AddRow(row);
        return new GroupBox { Text = title, Padding = new Padding(6), Content = layout };
    }
}
