using System;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Features.HullGeneration.Builders;
using Rhino;
using Rhino.DocObjects;
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
    private readonly NumericStepper _numLength;
    private readonly NumericStepper _numBeam;
    private readonly NumericStepper _numDraft;
    private readonly NumericStepper _numDepth;

    // Các nhãn hiển thị kết quả tính toán
    private readonly Label _lblVolume;
    private readonly Label _lblDisplacement;
    private readonly Label _lblLBRatio;

    public ShipInputDialog()
    {
        Title = "LMHull - Thiết kế & Tính toán thông số tàu (v1.0)";
        ClientSize = new Size(420, 360);
        Padding = new Padding(12);
        Resizable = false;

        // Khởi tạo các điều khiển nhập liệu
        _txtName = new TextBox { Text = "Sà lan LM-01" };

        _numLength = new NumericStepper { Value = 45.0, MinValue = 5.0, MaxValue = 300.0, DecimalPlaces = 2, Increment = 1.0 };
        _numBeam = new NumericStepper { Value = 10.0, MinValue = 2.0, MaxValue = 60.0, DecimalPlaces = 2, Increment = 0.5 };
        _numDraft = new NumericStepper { Value = 2.5, MinValue = 0.5, MaxValue = 20.0, DecimalPlaces = 2, Increment = 0.1 };
        _numDepth = new NumericStepper { Value = 3.5, MinValue = 1.0, MaxValue = 30.0, DecimalPlaces = 2, Increment = 0.2 };

        // Khởi tạo các nhãn kết quả
        var boldFont = new Eto.Drawing.Font(Eto.Drawing.SystemFont.Bold);

        _lblVolume = new Label { Text = "---", Font = boldFont };
        _lblDisplacement = new Label { Text = "---", Font = boldFont };
        _lblLBRatio = new Label { Text = "---", Font = boldFont };
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
        layout.AddRow(new Label { Text = "Chiều dài L (m):" }, _numLength);
        layout.AddRow(new Label { Text = "Chiều rộng B (m):" }, _numBeam);
        layout.AddRow(new Label { Text = "Chiều chìm T (m):" }, _numDraft);
        layout.AddRow(new Label { Text = "Chiều cao mạn D (m):" }, _numDepth);

        layout.AddRow(new GroupBox
        {
            Text = "Kết quả sơ bộ (LMHull.Core)",
            Padding = new Padding(8),
            Content = new TableLayout
            {
                Spacing = new Size(8, 4),
                Rows =
                {
                    new TableRow(new Label { Text = "Tỷ số L/B:" }, _lblLBRatio),
                    new TableRow(new Label { Text = "Thể tích chìm:" }, _lblVolume),
                    new TableRow(new Label { Text = "Lượng chiếm nước:" }, _lblDisplacement)
                }
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
        return new ShipInput(
            name: _txtName.Text,
            length: _numLength.Value,
            beam: _numBeam.Value,
            draft: _numDraft.Value,
            depth: _numDepth.Value
        );
    }

    private void UpdateCalculations()
    {
        var input = GetCurrentShipInput();
        var calc = new ShipCalculator();
        ShipResult res = calc.Calculate(input, WaterType.SeaWater);

        _lblLBRatio.Text = $"{res.LBRatio:F2}";
        _lblVolume.Text = $"{res.BoxVolume:F2} m³";
        _lblDisplacement.Text = $"{res.Displacement:F2} Tấn";
    }

    private void OnCalculateClicked(object? sender, EventArgs e)
    {
        UpdateCalculations();
    }

    private void OnCreateClicked(object? sender, EventArgs e)
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;

        var input = GetCurrentShipInput();

        // Dựng sà lan hộp dựa trên kích thước người dùng nhập trong Dialog
        Brep bargeBrep = BoxBargeBuilder.Build(input);

        var attr = new ObjectAttributes
        {
            Name = input.Name
        };

        Guid id = doc.Objects.AddBrep(bargeBrep, attr);
        if (id != Guid.Empty)
        {
            LMHullPlugin.Instance.LastHullGuid = id;
            doc.Views.Redraw();
            RhinoApp.WriteLine($"[ShipInputDialog] Đã dựng thành công '{input.Name}' (L={input.Length}m, B={input.Beam}m, D={input.Depth}m) vào Rhino.");
        }

        Close(true);
    }
}