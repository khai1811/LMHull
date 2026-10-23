using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Core.Validation;
using LMHull.Infrastructure.Persistence;
using LMHull.Rhino.Common;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Features.Structure.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.Infrastructure.Cad;
using LMHull.Rhino.UI.Common;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.UI;

public class LMStructureTreePanel : Panel
{
    private readonly StructureTreeView _treeView;
    private readonly Label _lblSelectedNode;
    private readonly LabeledNumericInput _numThickness;
    private readonly LabeledDropdown _ddlMaterial;
    private readonly ResultDisplayRow _rowSelectionWeight;
    private readonly ResultDisplayRow _rowSelectionCog;
    private readonly Button _btnApplyProperties;
    private readonly Button _btnPickFromGA;

    private StructuralNode _projectRoot;
    private StructuralNode? _activeNode;

    private bool _docEventsHooked;
    private bool _syncPending;

    public LMStructureTreePanel()
    {
        Padding = new Padding(6);

        // Khởi tạo cây khung dự án chuẩn ExpressMarine
        _projectRoot = ExpressMarineProjectBuilder.CreateExpressMarineTemplate("SuperYacht");

        // 1. Cây kết cấu
        _treeView = new StructureTreeView("Cấu trúc phần tử (ExpressMarine)");
        _treeView.NodeSelected += (_, node) => OnNodeSelected(node);

        var btnRefresh = new Button { Text = "Refresh (đồng bộ với Rhino)" };
        btnRefresh.Click += (_, _) => SyncWithDocument();

        var btnScan = new Button { Text = "Quét từ bản vẽ", ToolTip = "Dựng cây từ các đối tượng kết cấu đang có trong bản vẽ" };
        btnScan.Click += OnScanDocumentClicked;

        var btnSaveProject = new Button { Text = "Lưu dự án…" };
        btnSaveProject.Click += OnSaveProjectClicked;
        var btnOpenProject = new Button { Text = "Mở dự án…" };
        btnOpenProject.Click += OnOpenProjectClicked;

        // 2. Thuộc tính & Nút Pick on GA
        _lblSelectedNode = new Label { Text = "(Chưa chọn)", Font = SystemFonts.Bold() };

        _numThickness = new LabeledNumericInput("Thickness:", "mm", min: 2.0, max: 100.0, decimalPlaces: 1, value: 10.0)
        {
            Increment = 0.5,
            Enabled = false
        };

        _ddlMaterial = new LabeledDropdown("Material:", MaterialRegistry.AvailableMaterials.Select(m => m.Name)) { Enabled = false };

        _btnApplyProperties = new Button { Text = "Apply Properties", Enabled = false };
        _btnApplyProperties.Click += OnApplyClicked;

        _btnPickFromGA = new Button { Text = "Pick from GA (Tạo từ bản vẽ 2D)", Enabled = false };
        _btnPickFromGA.Click += OnPickFromGAClicked;

        var propLayout = new DynamicLayout { Spacing = new Size(4, 4) };
        propLayout.AddRow(new Label { Text = "Part:" }, _lblSelectedNode);
        propLayout.AddRow(_numThickness);
        propLayout.AddRow(_ddlMaterial);
        propLayout.AddRow(_btnApplyProperties);
        propLayout.AddRow(_btnPickFromGA);

        // 3. Selection Results (Giống hệt khung bên trái ảnh ExpressMarine)
        _rowSelectionWeight = new ResultDisplayRow("Weight:", "t", "0.000");
        _rowSelectionCog = new ResultDisplayRow("CoG (X, Y, Z):", "m", "(0.00, 0.00, 0.00)");

        var selLayout = new DynamicLayout { Spacing = new Size(4, 3) };
        selLayout.AddRow(_rowSelectionWeight);
        selLayout.AddRow(_rowSelectionCog);

        var layout = new DynamicLayout { Spacing = new Size(4, 6) };
        layout.AddRow(_treeView);
        layout.AddRow(btnRefresh);
        layout.AddRow(new TableLayout
        {
            Spacing = new Size(4, 4),
            Rows = { new TableRow(new TableCell(btnScan, true), new TableCell(btnSaveProject, true), new TableCell(btnOpenProject, true)) }
        });
        layout.AddRow(new GroupBox { Text = "Parameters & GA Binding", Padding = new Padding(6), Content = propLayout });
        layout.AddRow(new GroupBox
        {
            Text = "Selection Results",
            Padding = new Padding(6),
            BackgroundColor = Color.FromArgb(215, 230, 245),
            Content = selLayout
        });
        layout.AddRow(null);

        Content = layout;

        _treeView.LoadTree(_projectRoot);
    }

    private void UpdateNodes(IEnumerable<StructuralNode> changedNodes)
    {
        var reloaded = _treeView.UpdateNodes(changedNodes);
        if (_activeNode != null && reloaded.Contains(_activeNode)) ShowSelectionResults(_activeNode);
    }

    #region Đồng bộ với tài liệu Rhino

    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        HookDocumentEvents(true);
        SyncWithDocument();
    }

    protected override void OnUnLoad(EventArgs e)
    {
        HookDocumentEvents(false);
        base.OnUnLoad(e);
    }

    private void HookDocumentEvents(bool hook)
    {
        if (hook == _docEventsHooked) return;
        _docEventsHooked = hook;

        if (hook)
        {
            RhinoDoc.DeleteRhinoObject += OnDocumentObjectChanged;
            RhinoDoc.UndeleteRhinoObject += OnDocumentObjectChanged;
            RhinoDoc.EndOpenDocument += OnDocumentOpened;
            RhinoApp.Idle += OnRhinoIdle;
        }
        else
        {
            RhinoDoc.DeleteRhinoObject -= OnDocumentObjectChanged;
            RhinoDoc.UndeleteRhinoObject -= OnDocumentObjectChanged;
            RhinoDoc.EndOpenDocument -= OnDocumentOpened;
            RhinoApp.Idle -= OnRhinoIdle;
        }
    }

    // Chỉ đánh dấu; việc đồng bộ được gộp lại và chạy một lần khi Rhino rảnh
    private void OnDocumentObjectChanged(object? sender, RhinoObjectEventArgs e) => _syncPending = true;

    private void OnDocumentOpened(object? sender, DocumentOpenEventArgs e) => _syncPending = true;

    private void OnRhinoIdle(object? sender, EventArgs e)
    {
        if (!_syncPending) return;
        _syncPending = false;
        SyncWithDocument();
    }

    private void SyncWithDocument()
    {
        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        var changed = StructureTreeSync.Sync(cad, _projectRoot);
        if (changed.Count > 0) UpdateNodes(changed);
    }

    #endregion

    #region Chọn node & thuộc tính

    private void OnNodeSelected(StructuralNode node)
    {
        _activeNode = node;
        _lblSelectedNode.Text = node.Name;
        _numThickness.Value = node.ThicknessMm;
        _numThickness.Enabled = node.IsLeaf;
        _ddlMaterial.SelectedText = node.Material;
        _ddlMaterial.Enabled = node.IsLeaf;
        _btnApplyProperties.Enabled = node.IsLeaf;
        _btnPickFromGA.Enabled = node.IsPickableFromGA;

        ShowSelectionResults(node);

        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad != null) StructureSelectionService.Highlight(cad, node);
    }

    private void ShowSelectionResults(StructuralNode node)
    {
        _rowSelectionWeight.SetValue(node.TotalWeightTonnes, "F3");

        var cad = RhinoCadHandle.ForActiveDoc();
        Point3d? cog = cad == null ? null : StructureSelectionService.ComputeCenterOfGravity(cad, node);
        _rowSelectionCog.SetValue(cog is Point3d p ? $"({p.X:F2}, {p.Y:F2}, {p.Z:F2})" : "-");
    }

    private void OnApplyClicked(object? sender, EventArgs e)
    {
        if (_activeNode == null) return;

        double thick = _numThickness.Value;
        string mat = _ddlMaterial.SelectedText ?? ServiceRegistry.Settings.DefaultMaterial;

        var val = StructuralValidator.ValidatePlate(thick, mat);
        if (!val.IsValid)
        {
            MessageBox.Show("Vi phạm quy chuẩn: " + val.GetErrorMessage(), "Lỗi", MessageBoxType.Warning);
            return;
        }

        _activeNode.ThicknessMm = thick;
        _activeNode.Material = mat;

        // Cập nhật metadata và trọng lượng của hình học đã liên kết theo vật liệu mới
        var cad = RhinoCadHandle.ForActiveDoc();
        Guid geometryId = _activeNode.RhinoGeometryId;
        if (cad != null && cad.GetGeometry<Brep>(geometryId) is Brep brep)
        {
            cad.SetMeta(geometryId, StructureObjectData.Meta(_activeNode));
            _activeNode.LinkGeometry(geometryId, StructureObjectData.ComputeWeightTonnes(brep, mat));
            UpdateNodes(new[] { _activeNode });
        }

        RhinoApp.WriteLine($"[LMHull] Đã cập nhật thuộc tính cho: {_activeNode.Name}");
    }

    #endregion

    #region Quét từ bản vẽ

    private void OnScanDocumentClicked(object? sender, EventArgs e)
    {
        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        var scanned = LiveStructureScanner.BuildTreeFromDocument(cad, cad.DocumentName);
        int scannedParts = scanned.Leaves().Count();
        if (scannedParts == 0)
        {
            MessageBox.Show(this, "Không tìm thấy đối tượng kết cấu (Brep) nào trong bản vẽ.", "Quét từ bản vẽ", MessageBoxType.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            $"Tìm thấy {scannedParts} chi tiết trong bản vẽ.\n\n" +
            "Yes: thay cây hiện tại bằng cây quét được.\n" +
            "No: gộp các chi tiết chưa liên kết vào nhánh \"Từ bản vẽ\".",
            "Quét từ bản vẽ", MessageBoxButtons.YesNoCancel, MessageBoxType.Question);

        if (answer == DialogResult.Yes)
        {
            _projectRoot = scanned;
            _activeNode = null;
            _treeView.LoadTree(_projectRoot);
            RhinoApp.WriteLine($"[LMHull] Đã dựng cây từ bản vẽ: {scannedParts} chi tiết.");
        }
        else if (answer == DialogResult.No)
        {
            int added = MergeScannedParts(scanned);
            _treeView.LoadTree(_projectRoot);
            RhinoApp.WriteLine($"[LMHull] Đã gộp {added} chi tiết chưa liên kết vào nhánh \"Từ bản vẽ\".");
        }
    }

    /// <summary>
    /// Gộp các chi tiết quét được nhưng chưa thuộc node nào của cây hiện tại vào nhánh "Từ bản vẽ"
    /// (giữ nguyên nhóm phân loại của bộ quét). Trả về số chi tiết đã thêm.
    /// </summary>
    private int MergeScannedParts(StructuralNode scanned)
    {
        var linkedIds = _projectRoot.Leaves().Where(n => n.HasGeometry).Select(n => n.RhinoGeometryId).ToHashSet();

        var fromDocument = _projectRoot.Children.FirstOrDefault(c => c.Name == FromDocumentGroupName);
        if (fromDocument == null)
        {
            fromDocument = new StructuralNode(FromDocumentGroupName, StructuralNodeType.ShellGroup);
            _projectRoot.AddChild(fromDocument);
        }

        int added = 0;
        foreach (var group in scanned.Children.ToList())
        {
            var newParts = group.Children.Where(p => !linkedIds.Contains(p.RhinoGeometryId)).ToList();
            if (newParts.Count == 0) continue;

            var target = fromDocument.Children.FirstOrDefault(c => c.Name == group.Name);
            if (target == null)
            {
                target = new StructuralNode(group.Name, group.NodeType);
                fromDocument.AddChild(target);
            }

            foreach (var part in newParts)
            {
                target.AddChild(part);
                linkedIds.Add(part.RhinoGeometryId);
                added++;
            }
        }
        return added;
    }

    private const string FromDocumentGroupName = "Từ bản vẽ";

    #endregion

    #region Lưu / mở dự án

    private void OnSaveProjectClicked(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog { Title = "Lưu dự án LMHull", FileName = DefaultProjectFileName() };
        sfd.Filters.Add(new FileFilter("Dự án LMHull (*.lmhull.json)", ".json"));
        if (ProjectFolder() is Uri folder) sfd.Directory = folder;
        if (sfd.ShowDialog(this) != DialogResult.Ok) return;

        string path = EnsureProjectExtension(sfd.FileName);
        try
        {
            var data = new ProjectData(_projectRoot);
            data.Results.AddRange(ResultsHub.History);
            ServiceRegistry.Projects.Save(data, path);
            ServiceRegistry.Logger.Info($"Đã lưu dự án: {path}");
            RhinoApp.WriteLine($"[LMHull] Đã lưu dự án: {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ServiceRegistry.Logger.Error($"Không lưu được dự án '{path}': {ex.Message}", ex);
            MessageBox.Show(this, $"Không lưu được dự án:\n{ex.Message}", "LMHull", MessageBoxType.Error);
        }
    }

    private void OnOpenProjectClicked(object? sender, EventArgs e)
    {
        var ofd = new OpenFileDialog { Title = "Mở dự án LMHull" };
        ofd.Filters.Add(new FileFilter("Dự án LMHull (*.lmhull.json)", ".json"));
        if (ProjectFolder() is Uri folder) ofd.Directory = folder;
        if (ofd.ShowDialog(this) != DialogResult.Ok) return;

        try
        {
            var data = ServiceRegistry.Projects.Load(ofd.FileName);
            _projectRoot = data.Root;
            _activeNode = null;
            _treeView.LoadTree(_projectRoot);
            ResultsHub.Replace(data.Results);

            // Nối lại hình học: gỡ liên kết tới đối tượng không còn, khôi phục từ metadata LMHull.NodeId
            SyncWithDocument();
            ServiceRegistry.Logger.Info($"Đã mở dự án: {ofd.FileName}");
            RhinoApp.WriteLine($"[LMHull] Đã mở dự án '{data.Name}' (lưu lúc {data.SavedAt:dd/MM/yyyy HH:mm}).");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            ServiceRegistry.Logger.Error($"Không mở được dự án '{ofd.FileName}': {ex.Message}", ex);
            MessageBox.Show(this, $"Không mở được dự án:\n{ex.Message}", "LMHull", MessageBoxType.Error);
        }
    }

    // Mặc định đặt tệp dự án cạnh tệp .3dm đang mở
    private static Uri? ProjectFolder()
    {
        string docPath = RhinoCadHandle.ForActiveDoc()?.DocumentPath ?? string.Empty;
        string? folder = string.IsNullOrEmpty(docPath) ? null : Path.GetDirectoryName(docPath);
        return folder != null && Directory.Exists(folder) ? new Uri(folder) : ExportDirectory.Default;
    }

    private static string DefaultProjectFileName()
    {
        string docPath = RhinoCadHandle.ForActiveDoc()?.DocumentPath ?? string.Empty;
        string name = string.IsNullOrEmpty(docPath) ? "LMHull_Project" : Path.GetFileNameWithoutExtension(docPath);
        return name + JsonProjectRepository.FileExtension;
    }

    private static string EnsureProjectExtension(string path) =>
        path.EndsWith(JsonProjectRepository.FileExtension, StringComparison.OrdinalIgnoreCase)
            ? path
            : Path.ChangeExtension(path, null) + JsonProjectRepository.FileExtension;

    #endregion

    #region Pick from GA

    private void OnPickFromGAClicked(object? sender, EventArgs e)
    {
        var node = _activeNode;
        if (node == null || !node.IsPickableFromGA) return;

        var cad = RhinoCadHandle.ForActiveDoc();
        if (cad == null) return;

        RhinoApp.WriteLine($"\n>>> CHUẨN BỊ GÁN HÌNH HỌC CHO PHẦN TỬ: {node.Name} <<<");

        if (RhinoInput.PickBrep("1. Chọn khối thân tàu 3D (Brep Solid)", out Brep hull) != global::Rhino.Commands.Result.Success) return;
        if (RhinoInput.PickCurve($"2. Click vào nét vẽ 2D trên bản vẽ GA tương ứng với '{node.Name}':", out Curve gaCurve) != global::Rhino.Commands.Result.Success) return;

        Brep? generatedPlate = GAPlacementService.CreateFromGA(hull, gaCurve, node.NodeType, cad.Tolerance);
        if (generatedPlate == null)
        {
            MessageBox.Show("Nét vẽ 2D nằm ngoài phạm vi thân tàu hoặc không thể cắt gọt hình học.", "Lỗi chiếu dựng", MessageBoxType.Error);
            return;
        }

        // Thay hình học cũ (nếu có), thêm hình mới kèm metadata liên kết node
        if (node.HasGeometry) cad.Delete(node.RhinoGeometryId);
        Guid objId = GAPlacementService.Bake(cad, generatedPlate, node.NodeType, node.Name, node);

        node.LinkGeometry(objId, StructureObjectData.ComputeWeightTonnes(generatedPlate, node.Material));
        _syncPending = false; // thay đổi do chính panel tạo ra, đã cập nhật trực tiếp
        UpdateNodes(new[] { node });

        RhinoApp.WriteLine($"[Thành công]: Đã tạo thành công kết cấu 3D cho '{node.Name}' từ bản vẽ 2D!");
    }

    #endregion
}
