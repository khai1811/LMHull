using System;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Abstractions;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using LMHull.Rhino.Features.Hydrostatics.Services;
using LMHull.Rhino.Features.Hydrostatics.UI.Conduits;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using LMHull.Rhino.UI.Common;
using Rhino;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.UI.Panels;

public class LMHullMainPanel : Panel
{
    private readonly ShipDimensionsInput _dims;

    private readonly ResultDisplayRow _rowDisplacement;
    private readonly ResultDisplayRow _rowVolume;
    private readonly ResultDisplayRow _rowAw;
    private readonly ResultDisplayRow _rowCb;
    private readonly ResultDisplayRow _rowKB;
    private readonly CheckBox _chkShowWaterplane;

    private readonly RangeLimitInput _limitX;
    private readonly RangeLimitInput _limitY;
    private readonly RangeLimitInput _limitZ;

    private readonly WaterplaneConduit _conduit = new();

    public LMHullMainPanel()
    {
        Padding = new Padding(8);

        _dims = new ShipDimensionsInput(length: 50.0, beam: 10.0, draft: 2.5, depth: 4.0, minDraft: 0.2);

        _rowVolume = new ResultDisplayRow("V chìm thực:", "m³");
        _rowDisplacement = new ResultDisplayRow("Lượng chiếm nước:", "t");
        _rowAw = new ResultDisplayRow("Diện tích Aw:", "m²");
        _rowCb = new ResultDisplayRow("Hệ số béo Cb:");
        _rowKB = new ResultDisplayRow("Cao độ tâm nổi KB:", "m");

        // Giới hạn vùng tính (áp dụng cho LMStructureWeight, LMCreateCompartments)
        var limits = ServiceRegistry.Settings.Limits;
        _limitX = CreateLimitInput("X (dọc)", limits.X);
        _limitY = CreateLimitInput("Y (ngang)", limits.Y);
        _limitZ = CreateLimitInput("Z (đứng)", limits.Z);
        var btnSaveLimits = new Button { Text = "Lưu giới hạn" };
        btnSaveLimits.Click += (s, e) => SaveLimits();

        _chkShowWaterplane = new CheckBox { Text = "Hiện mặt nước động (Conduit)", Checked = true };
        _chkShowWaterplane.CheckedChanged += (s, e) => ToggleConduit();

        _dims.ValueChanged += (s, e) => UpdateWaterplaneAndCompute();

        var btnCalculate = new Button { Text = "Tính toán chi tiết" };
        btnCalculate.Click += (s, e) => ComputeRealHydrostatics();

        var btnGenerateBarge = new Button { Text = "Dựng sà lan hộp" };
        btnGenerateBarge.Click += (s, e) => GenerateHull(isNurbs: false);

        var btnGenerateNurbs = new Button { Text = "Dựng vỏ NURBS" };
        btnGenerateNurbs.Click += (s, e) => GenerateHull(isNurbs: true);

        var btnGenerateLinesPlan = new Button { Text = "Xuất tuyến hình" };
        btnGenerateLinesPlan.Click += (s, e) =>
        {
            RhinoApp.RunScript("LMLinesPlan", true);
        };

        // Khai báo 2 nút xuất dữ liệu Giai đoạn 6
        var btnExportCsv = new Button { Text = "Xuất CSV" };
        btnExportCsv.Click += OnExportCsvClicked;

        var btnExportJson = new Button { Text = "Xuất JSON" };
        btnExportJson.Click += OnExportJsonClicked;

        var layout = new DynamicLayout { Spacing = new Size(4, 4) };

        layout.AddRow(_dims);
        layout.AddRow(_chkShowWaterplane);
        layout.AddRow(btnCalculate);

        var resultsLayout = new DynamicLayout { Spacing = new Size(4, 3) };
        resultsLayout.AddRow(_rowVolume);
        resultsLayout.AddRow(_rowDisplacement);
        resultsLayout.AddRow(_rowAw);
        resultsLayout.AddRow(_rowCb);
        resultsLayout.AddRow(_rowKB);

        layout.AddRow(new GroupBox
        {
            Text = "Thủy tĩnh thực tế",
            Padding = new Padding(6),
            Content = resultsLayout
        });

        var limitsLayout = new DynamicLayout { Spacing = new Size(4, 2) };
        limitsLayout.AddRow(_limitX);
        limitsLayout.AddRow(_limitY);
        limitsLayout.AddRow(_limitZ);
        limitsLayout.AddRow(btnSaveLimits);
        layout.AddRow(new GroupBox { Text = "Giới hạn vùng tính", Padding = new Padding(6), Content = limitsLayout });

        layout.AddRow(btnGenerateLinesPlan);
        layout.AddRow(new Label { Text = "Sinh mô hình CAD:" });
        layout.AddRow(btnGenerateBarge);
        layout.AddRow(btnGenerateNurbs);

        // Khối xuất báo cáo Giai đoạn 6
        layout.AddRow(new Label { Text = "Xuất dữ liệu & Báo cáo:" });
        layout.AddRow(new TableLayout
        {
            Spacing = new Size(4, 4),
            Rows = { new TableRow(new TableCell(btnExportCsv, true), new TableCell(btnExportJson, true)) }
        });

        layout.AddRow(null);

        Content = layout;

        ToggleConduit();
        ComputeRealHydrostatics();
    }

    private static RangeLimitInput CreateLimitInput(string axis, AxisLimit limit) =>
        new(axis, "m", decimalPlaces: 2, enabled: limit.Enabled) { Range = new Interval(limit.Min, limit.Max) };

    private static AxisLimit ToAxisLimit(RangeLimitInput input) => new()
    {
        Enabled = input.IsLimitEnabled,
        Min = input.Range.Min,
        Max = input.Range.Max
    };

    private void SaveLimits()
    {
        var settings = ServiceRegistry.Settings;
        settings.Limits = new CalculationLimits
        {
            X = ToAxisLimit(_limitX),
            Y = ToAxisLimit(_limitY),
            Z = ToAxisLimit(_limitZ)
        };
        ServiceRegistry.SaveSettings(settings);

        string description = settings.Limits.Describe();
        RhinoApp.WriteLine("[LMHull] " + (description.Length > 0 ? description : "Đã tắt giới hạn vùng tính."));
    }

    private void ToggleConduit()
    {
        _conduit.Enabled = _chkShowWaterplane.Checked ?? false;
        UpdateConduitGeometry();
        RhinoCadHandle.ForActiveDoc()?.Redraw();
    }

    private void UpdateConduitGeometry()
    {
        _conduit.WaterZ = _dims.Draft;
        _conduit.Length = _dims.Length;
        _conduit.Beam = _dims.Beam;
    }

    private void UpdateWaterplaneAndCompute()
    {
        UpdateConduitGeometry();
        RhinoCadHandle.ForActiveDoc()?.Redraw();
        ComputeRealHydrostatics();
    }

    private void ComputeRealHydrostatics()
    {
        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        Brep? targetBrep = cad.GetGeometry<Brep>(LMHullPlugin.Instance.LastHullGuid);

        if (targetBrep == null)
        {
            var fallbackInput = _dims.ToShipInput("Temp");
            targetBrep = BoxBargeBuilder.Build(fallbackInput);
        }

        var hydro = ActualHydrostaticCalculator.Compute(targetBrep, _dims.Draft, ServiceRegistry.Settings.SeaWaterDensity, cad.Tolerance);
        if (hydro != null)
        {
            _rowVolume.SetValue(hydro.SubmergedVolume, "F1");
            _rowDisplacement.SetValue(hydro.Displacement, "F1");
            _rowAw.SetValue(hydro.WaterplaneArea, "F1");
            _rowCb.SetValue(hydro.BlockCoefficient, "F3");
            _rowKB.SetValue(hydro.CenterOfBuoyancy.Z, "F2");
        }
    }

    private void GenerateHull(bool isNurbs)
    {
        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        var input = _dims.ToShipInput("LM-Ship");
        LayerManager.EnsureStandardLayers(cad);

        Brep? brep = isNurbs
            ? SymmetricHullBuilder.BuildSymmetricSolidHull(input, 7, cad.Tolerance)
            : BoxBargeBuilder.Build(input);

        if (brep == null) return;

        Guid id = cad.AddBrep(brep, LayerManager.Hull.Spec(input.Name));

        LMHullPlugin.Instance.LastHullGuid = id;
        cad.Redraw();
        ComputeRealHydrostatics();
    }

    private void OnExportCsvClicked(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Lưu báo cáo thủy tĩnh CSV",
            FileName = "BaoCao_ThuyTinh.csv",
            Directory = ExportDirectory.Default
        };
        sfd.Filters.Add(new FileFilter("Tệp CSV (*.csv)", ".csv"));

        if (sfd.ShowDialog(this) == DialogResult.Ok)
        {
            var input = _dims.ToShipInput("LM-Ship");
            var result = new ShipCalculator(ServiceRegistry.Settings).Calculate(input, WaterType.SeaWater);
            ServiceRegistry.Exporter.ExportShip(input, result, sfd.FileName, ExportFormat.Csv);
            MessageBox.Show($"Đã xuất file CSV thành công tại:\n{sfd.FileName}", "LMHull Export", MessageBoxType.Information);
        }
    }

    private void OnExportJsonClicked(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Lưu dữ liệu cấu trúc JSON",
            FileName = "DuLieu_Tau.json",
            Directory = ExportDirectory.Default
        };
        sfd.Filters.Add(new FileFilter("Tệp JSON (*.json)", ".json"));

        if (sfd.ShowDialog(this) == DialogResult.Ok)
        {
            var input = _dims.ToShipInput("LM-Ship");
            var result = new ShipCalculator(ServiceRegistry.Settings).Calculate(input, WaterType.SeaWater);
            ServiceRegistry.Exporter.ExportShip(input, result, sfd.FileName, ExportFormat.Json);
            MessageBox.Show($"Đã xuất file JSON thành công tại:\n{sfd.FileName}", "LMHull Export", MessageBoxType.Information);
        }
    }
}