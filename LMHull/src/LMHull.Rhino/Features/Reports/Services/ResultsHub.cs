using System;
using System.Collections.Generic;
using LMHull.Core.Reporting;
using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.UI;
using Rhino;

namespace LMHull.Rhino.Features.Reports.Services;

/// <summary>
/// Nơi các lệnh tính toán gửi bảng kết quả; tab Kết quả của panel "LMHull" hiển thị lịch sử trong phiên làm việc.
/// </summary>
public static class ResultsHub
{
    private static readonly List<ResultTable> _history = new();

    public static event EventHandler<ResultTable>? TableAdded;
    public static event EventHandler? HistoryCleared;

    public static IReadOnlyList<ResultTable> History => _history;

    /// <summary>
    /// Lưu bảng vào lịch sử, mở panel kết quả và in một dòng tóm tắt ra dòng lệnh.
    /// </summary>
    public static void Publish(ResultTable table)
    {
        _history.Add(table);
        while (_history.Count > Math.Max(1, ServiceRegistry.Settings.MaxResultHistory)) _history.RemoveAt(0);

        TableAdded?.Invoke(null, table);
        LMHullWorkspacePanel.Show(WorkspaceTab.Results);

        RhinoApp.WriteLine($"[{table.Source}] Đã xuất kết quả '{table.Title}' vào panel 'LMHull' (tab Kết quả).");
    }

    public static void Remove(ResultTable table) => _history.Remove(table);

    public static void Clear()
    {
        _history.Clear();
        HistoryCleared?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Thay toàn bộ lịch sử (ví dụ khi mở dự án đã lưu) mà không tự chuyển sang tab Kết quả.
    /// </summary>
    public static void Replace(IEnumerable<ResultTable> tables)
    {
        Clear();
        foreach (var table in tables)
        {
            _history.Add(table);
            TableAdded?.Invoke(null, table);
        }
    }
}
