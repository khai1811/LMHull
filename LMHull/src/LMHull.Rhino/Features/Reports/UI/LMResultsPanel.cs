using System;
using System.Linq;
using Eto.Forms;
using LMHull.Core.Reporting;
using LMHull.Rhino.Features.Reports.Services;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.UI.Common;

namespace LMHull.Rhino.Features.Reports.UI;

/// <summary>
/// Tab Kết quả: mỗi lần chạy lệnh tính toán thêm một tab bảng kết quả, giữ lịch sử trong phiên.
/// </summary>
public class LMResultsPanel : Panel
{
    private readonly TabControl _tabs;
    private readonly Label _lblEmpty;
    private bool _hooked;

    public LMResultsPanel()
    {
        Padding = new Eto.Drawing.Padding(6);

        _tabs = new TabControl();
        _lblEmpty = new Label { Text = "Chưa có kết quả. Chạy LMHydro, LMCreateCompartments, LMCreateStructure, LMLinesPlan hoặc ShipInfo." };

        var btnCopy = new Button { Text = "Copy" };
        btnCopy.Click += OnCopyClicked;
        var btnExport = new Button { Text = "Xuất CSV…" };
        btnExport.Click += OnExportClicked;
        var btnCloseTab = new Button { Text = "Đóng tab" };
        btnCloseTab.Click += OnCloseTabClicked;
        var btnClearAll = new Button { Text = "Xóa tất cả" };
        btnClearAll.Click += (_, _) => ResultsHub.Clear();

        var toolbar = new TableLayout
        {
            Spacing = UiMetrics.Spacing,
            Rows = { new TableRow(btnCopy, btnExport, btnCloseTab, btnClearAll, null) }
        };

        var layout = new DynamicLayout { Spacing = UiMetrics.Spacing };
        layout.AddRow(toolbar);
        layout.AddRow(_lblEmpty);
        layout.Add(_tabs, yscale: true);
        Content = layout;

        UpdateEmptyState();
    }

    private ResultTable? SelectedTable => (_tabs.SelectedPage?.Content as ResultTableView)?.Table;

    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        if (_hooked) return;
        _hooked = true;

        // Nạp lịch sử có sẵn (ví dụ khi panel được mở lại)
        _tabs.Pages.Clear();
        foreach (var table in ResultsHub.History) AddTab(table);

        ResultsHub.TableAdded += OnTableAdded;
        ResultsHub.HistoryCleared += OnHistoryCleared;
    }

    protected override void OnUnLoad(EventArgs e)
    {
        if (_hooked)
        {
            ResultsHub.TableAdded -= OnTableAdded;
            ResultsHub.HistoryCleared -= OnHistoryCleared;
            _hooked = false;
        }
        base.OnUnLoad(e);
    }

    private void OnTableAdded(object? sender, ResultTable table) =>
        Application.Instance.AsyncInvoke(() => AddTab(table));

    private void OnHistoryCleared(object? sender, EventArgs e) =>
        Application.Instance.AsyncInvoke(() =>
        {
            _tabs.Pages.Clear();
            UpdateEmptyState();
        });

    private void AddTab(ResultTable table)
    {
        // Bảng đã có tab (ví dụ vừa nạp lịch sử) thì không thêm lần nữa
        if (_tabs.Pages.Any(p => (p.Content as ResultTableView)?.Table == table)) return;

        var page = new TabPage(new ResultTableView(table)) { Text = $"{table.Source} {table.CreatedAt:HH:mm:ss}" };
        _tabs.Pages.Add(page);
        _tabs.SelectedPage = page;
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        bool empty = _tabs.Pages.Count == 0;
        _lblEmpty.Visible = empty;
        _tabs.Visible = !empty;
    }

    private void OnCopyClicked(object? sender, EventArgs e)
    {
        if (SelectedTable is not ResultTable table) return;
        new Clipboard().Text = table.ToTsv();
    }

    private void OnExportClicked(object? sender, EventArgs e)
    {
        if (SelectedTable is not ResultTable table) return;

        var sfd = new SaveFileDialog
        {
            Title = "Xuất bảng kết quả CSV",
            FileName = $"{table.Source}_{table.CreatedAt:yyyyMMdd_HHmmss}.csv",
            Directory = ExportDirectory.Default
        };
        sfd.Filters.Add(new FileFilter("Tệp CSV (*.csv)", ".csv"));

        if (sfd.ShowDialog(this) == DialogResult.Ok)
        {
            ServiceRegistry.Exporter.ExportCsv(table, sfd.FileName);
            MessageBox.Show($"Đã xuất file CSV tại:\n{sfd.FileName}", "LMHull Results", MessageBoxType.Information);
        }
    }

    private void OnCloseTabClicked(object? sender, EventArgs e)
    {
        if (_tabs.SelectedPage is not TabPage page) return;

        if (page.Content is ResultTableView view) ResultsHub.Remove(view.Table);
        _tabs.Pages.Remove(page);
        UpdateEmptyState();
    }
}
