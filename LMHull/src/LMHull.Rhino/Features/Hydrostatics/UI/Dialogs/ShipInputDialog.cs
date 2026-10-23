using System;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using LMHull.Rhino.UI.Common;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;

namespace LMHull.Rhino.Features.Hydrostatics.UI.Dialogs;

/// <summary>
/// Hộp thoại Eto.Forms cho phép người dùng nhập thông số L, B, T, D qua giao diện đồ họa.
/// </summary>
public class ShipInputDialog : Dialog<bool>
{
    // Các điều khiển nhập liệu
    private readonly TextBox _txtName;
    private readonly ShipDimensionsInput _dims;

    // Các hàng hiển thị kết quả tính toán
    private readonly ResultDisplayRow _rowVolume;
    private readonly ResultDisplayRow _rowDisplacement;
    private readonly ResultDisplayRow _rowLBRatio;

    public ShipInputDialog()
    {
        Title = "LMHull - Thiết kế & Tính toán thông số tàu (v1.0)";
        ClientSize = new Size(420, 360);
        Padding = new Padding(12);
        Resizable = false;

        // Khởi tạo các điều khiển nhập liệu
        _txtName = new TextBox { Text = "Sà lan LM-01" };

        _dims = new ShipDimensionsInput(length: 45.0, beam: 10.0, draft: 2.5, depth: 3.5, minDraft: 0.5);

        // Khởi tạo các hàng kết quả
        _rowLBRatio = new ResultDisplayRow("Tỷ số L/B:");
        _rowVolume = new ResultDisplayRow("Thể tích chìm:", "m³");
        _rowDisplacement = new ResultDisplayRow("Lượng chiếm nước:", "Tấn");

        // Các nút bấm hành động
        var btnCalculate = new Button { Text = "Tính toán" };
        btnCalculate.Click += OnCalculateClicked;

        var btnCreate = new Button { Text = "Dựng hình 3D" };
        btnCreate.Click += OnCreateClicked;

        var btnClose = new Button { Text = "Đóng" };
        btnClose.Click += (sender, e) => Close(false);

        // Thiết lập bố cục giao diện bằng DynamicLayout
        var layout = new DynamicLayout { Spacing = new Size(8, 8) };

        layout.AddRow(new Label { Text = "Tên phương án tàu:" }, _txtName);
        layout.AddRow(_dims);

        layout.AddRow(new GroupBox
        {
            Text = "Kết quả sơ bộ (LMHull.Core)",
            Padding = new Padding(8),
            Content = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items = { _rowLBRatio, _rowVolume, _rowDisplacement }
            }
        });

        // Hàng nút bấm dưới cùng
        layout.AddRow(null); // Khoảng trống giãn cách
        layout.AddRow(new TableLayout
        {
            Spacing = new Size(6, 6),
            Rows = { new TableRow(btnCalculate, btnCreate, btnClose) }
        });

        Content = layout;

        // Tự động tính toán kết quả ngay khi mở hộp thoại
        UpdateCalculations();
    }

    private ShipInput GetCurrentShipInput()
    {
        return _dims.ToShipInput(_txtName.Text);
    }

    private void UpdateCalculations()
    {
        var input = GetCurrentShipInput();
        var calc = new ShipCalculator(ServiceRegistry.Settings);
        ShipResult res = calc.Calculate(input, WaterType.SeaWater);

        _rowLBRatio.SetValue(res.LBRatio, "F2");
        _rowVolume.SetValue(res.BoxVolume, "F2");
        _rowDisplacement.SetValue(res.Displacement, "F2");
    }

    private void OnCalculateClicked(object? sender, EventArgs e)
    {
        UpdateCalculations();
    }

    private void OnCreateClicked(object? sender, EventArgs e)
    {
        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        var input = GetCurrentShipInput();

        // Dựng sà lan hộp dựa trên kích thước người dùng nhập trong Dialog
        Brep bargeBrep = BoxBargeBuilder.Build(input);

        Guid id = cad.AddBrep(bargeBrep, LayerManager.Hull.Spec(input.Name));
        if (id != Guid.Empty)
        {
            LMHullPlugin.Instance.LastHullGuid = id;
            cad.Redraw();
            RhinoApp.WriteLine($"[ShipInputDialog] Đã dựng thành công '{input.Name}' (L={input.Length}m, B={input.Beam}m, D={input.Depth}m) vào Rhino.");
        }

        Close(true);
    }
}