using System;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.HullGeneration.Builders;
using LMHull.Rhino.Features.Hydrostatics.Services;
using LMHull.Rhino.Features.Hydrostatics.UI.Conduits;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Hydrostatics.UI.Panels;

[Guid("E8C4307C-8A9F-4D2A-B9F2-C5E37F9281C3")]
public class LMHullMainPanel : Panel
{
    public static Guid PanelId => typeof(LMHullMainPanel).GUID;

    private readonly NumericStepper _numLength;
    private readonly NumericStepper _numBeam;
    private readonly NumericStepper _numDraft;
    private readonly NumericStepper _numDepth;

    private readonly Label _lblDisplacement;
    private readonly Label _lblVolume;
    private readonly Label _lblAw;
    private readonly Label _lblCb;
    private readonly Label _lblKB;
    private readonly CheckBox _chkShowWaterplane;

    private readonly WaterplaneConduit _conduit = new();

    public LMHullMainPanel()
    {
        Padding = new Padding(8);

        _numLength = new NumericStepper { Value = 50.0, MinValue = 5.0, MaxValue = 300.0, DecimalPlaces = 2, Increment = 1.0 };
        _numBeam = new NumericStepper { Value = 10.0, MinValue = 2.0, MaxValue = 60.0, DecimalPlaces = 2, Increment = 0.5 };
        _numDraft = new NumericStepper { Value = 2.5, MinValue = 0.2, MaxValue = 20.0, DecimalPlaces = 2, Increment = 0.1 };
        _numDepth = new NumericStepper { Value = 4.0, MinValue = 1.0, MaxValue = 30.0, DecimalPlaces = 2, Increment = 0.2 };

        var boldFont = new Eto.Drawing.Font(Eto.Drawing.SystemFont.Bold);

        _lblDisplacement = new Label { Text = "---", Font = boldFont, TextAlignment = TextAlignment.Right };
        _lblVolume = new Label { Text = "---", Font = boldFont, TextAlignment = TextAlignment.Right };
        _lblAw = new Label { Text = "---", Font = boldFont, TextAlignment = TextAlignment.Right };
        _lblCb = new Label { Text = "---", Font = boldFont, TextAlignment = TextAlignment.Right };
        _lblKB = new Label { Text = "---", Font = boldFont, TextAlignment = TextAlignment.Right };

        _chkShowWaterplane = new CheckBox { Text = "Hiện mặt nước động (Conduit)", Checked = true };
        _chkShowWaterplane.CheckedChanged += (s, e) => ToggleConduit();

        _numDraft.ValueChanged += (s, e) => UpdateWaterplaneAndCompute();

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

        layout.AddRow(new Label { Text = "L (Dài - m):" }, _numLength);
        layout.AddRow(new Label { Text = "B (Rộng - m):" }, _numBeam);
        layout.AddRow(new Label { Text = "T (Chìm - m):" }, _numDraft);
        layout.AddRow(new Label { Text = "D (Mạn - m):" }, _numDepth);
        layout.AddRow(_chkShowWaterplane);
        layout.AddRow(btnCalculate);

        var resultsLayout = new DynamicLayout { Spacing = new Size(4, 3) };
        resultsLayout.AddRow(new Label { Text = "V chìm thực:" }, _lblVolume);
        resultsLayout.AddRow(new Label { Text = "Lượng chiếm nước:" }, _lblDisplacement);
        resultsLayout.AddRow(new Label { Text = "Diện tích Aw:" }, _lblAw);
        resultsLayout.AddRow(new Label { Text = "Hệ số béo Cb:" }, _lblCb);
        resultsLayout.AddRow(new Label { Text = "Cao độ tâm nổi KB:" }, _lblKB);

        layout.AddRow(new GroupBox
        {
            Text = "Thủy tĩnh thực tế",
            Padding = new Padding(6),
            Content = resultsLayout
        });

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

    private void ToggleConduit()
    {
        _conduit.Enabled = _chkShowWaterplane.Checked ?? false;
        UpdateConduitGeometry();
        RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    private void UpdateConduitGeometry()
    {
        _conduit.WaterZ = _numDraft.Value;
        _conduit.Length = _numLength.Value;
        _conduit.Beam = _numBeam.Value;
    }

    private void UpdateWaterplaneAndCompute()
    {
        UpdateConduitGeometry();
        RhinoDoc.ActiveDoc?.Views.Redraw();
        ComputeRealHydrostatics();
    }

    private void ComputeRealHydrostatics()
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;

        Brep? targetBrep = null;
        Guid lastId = LMHullPlugin.Instance.LastHullGuid;
        if (lastId != Guid.Empty)
        {
            var obj = doc.Objects.FindId(lastId);
            if (obj?.Geometry is Brep b) targetBrep = b;
        }

        if (targetBrep == null)
        {
            var fallbackInput = new ShipInput("Temp", _numLength.Value, _numBeam.Value, _numDraft.Value, _numDepth.Value);
            targetBrep = BoxBargeBuilder.Build(fallbackInput);
        }

        var hydro = ActualHydrostaticCalculator.Compute(targetBrep, _numDraft.Value, 1.025, doc.ModelAbsoluteTolerance);
        if (hydro != null)
        {
            _lblVolume.Text = $"{hydro.SubmergedVolume:F1} m³";
            _lblDisplacement.Text = $"{hydro.Displacement:F1} T";
            _lblAw.Text = $"{hydro.WaterplaneArea:F1} m²";
            _lblCb.Text = $"{hydro.BlockCoefficient:F3}";
            _lblKB.Text = $"{hydro.CenterOfBuoyancy.Z:F2} m";
        }
    }

    private void GenerateHull(bool isNurbs)
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;

        var input = new ShipInput("LM-Ship", _numLength.Value, _numBeam.Value, _numDraft.Value, _numDepth.Value);
        LayerManager.EnsureStandardLayers(doc);
        int hullLayer = LayerManager.GetOrCreateLayer(doc, LayerManager.HullLayerName, System.Drawing.Color.Red);

        Brep? brep = isNurbs
            ? SymmetricHullBuilder.BuildSymmetricSolidHull(input, 7, doc.ModelAbsoluteTolerance)
            : BoxBargeBuilder.Build(input);

        if (brep == null) return;

        var attr = new ObjectAttributes { LayerIndex = hullLayer, Name = input.Name };
        Guid id = doc.Objects.AddBrep(brep, attr);

        LMHullPlugin.Instance.LastHullGuid = id;
        doc.Views.Redraw();
        ComputeRealHydrostatics();
    }

    private void OnExportCsvClicked(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Lưu báo cáo thủy tĩnh CSV",
            FileName = "BaoCao_ThuyTinh.csv"
        };
        sfd.Filters.Add(new FileFilter("Tệp CSV (*.csv)", ".csv"));

        if (sfd.ShowDialog(this) == DialogResult.Ok)
        {
            var input = new ShipInput("LM-Ship", _numLength.Value, _numBeam.Value, _numDraft.Value, _numDepth.Value);
            var result = new ShipCalculator().Calculate(input, WaterType.SeaWater);
            ShipExportService.ExportToCsv(sfd.FileName, input, result);
            MessageBox.Show($"Đã xuất file CSV thành công tại:\n{sfd.FileName}", "LMHull Export", MessageBoxType.Information);
        }
    }

    private void OnExportJsonClicked(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Lưu dữ liệu cấu trúc JSON",
            FileName = "DuLieu_Tau.json"
        };
        sfd.Filters.Add(new FileFilter("Tệp JSON (*.json)", ".json"));

        if (sfd.ShowDialog(this) == DialogResult.Ok)
        {
            var input = new ShipInput("LM-Ship", _numLength.Value, _numBeam.Value, _numDraft.Value, _numDepth.Value);
            var result = new ShipCalculator().Calculate(input, WaterType.SeaWater);
            ShipExportService.ExportToJson(sfd.FileName, input, result);
            MessageBox.Show($"Đã xuất file JSON thành công tại:\n{sfd.FileName}", "LMHull Export", MessageBoxType.Information);
        }
    }
}