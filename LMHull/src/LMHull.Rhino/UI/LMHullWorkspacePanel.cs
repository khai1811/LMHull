using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using LMHull.Rhino.Features.Hydrostatics.UI.Panels;
using LMHull.Rhino.Features.Reports.UI;
using LMHull.Rhino.Features.Structure.UI;

namespace LMHull.Rhino.UI;

/// <summary>
/// Panel chung "LMHull": gom Lệnh, Thủy tĩnh, Kết cấu và Kết quả vào một panel, mở bằng lệnh LMHull.
/// </summary>
[Guid("A41C9E57-2B6D-4F83-9E10-5C7D2F8B3A64")]
public class LMHullWorkspacePanel : Panel
{
    public static Guid PanelId => typeof(LMHullWorkspacePanel).GUID;

    // Instance đang hiển thị; tab cần chọn được ghi nhớ nếu panel chưa kịp tạo
    private static LMHullWorkspacePanel? _current;
    private static WorkspaceTab? _pendingTab;

    private readonly TabControl _tabs;

    public LMHullWorkspacePanel()
    {
        _tabs = new TabControl();
        _tabs.Pages.Add(new TabPage(new CommandLauncherView()) { Text = "Lệnh" });
        _tabs.Pages.Add(new TabPage(new LMHullMainPanel()) { Text = "Thủy tĩnh" });
        _tabs.Pages.Add(new TabPage(new LMStructureTreePanel()) { Text = "Kết cấu" });
        _tabs.Pages.Add(new TabPage(new LMResultsPanel()) { Text = "Kết quả" });

        Content = _tabs;
    }

    /// <summary>
    /// Mở panel LMHull (nếu chưa mở) và chuyển đến tab chỉ định.
    /// </summary>
    public static void Show(WorkspaceTab tab)
    {
        _pendingTab = tab;
        global::Rhino.UI.Panels.OpenPanel(PanelId);
        _current?.SelectPendingTab();
    }

    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        _current = this;
        SelectPendingTab();
    }

    protected override void OnUnLoad(EventArgs e)
    {
        if (_current == this) _current = null;
        base.OnUnLoad(e);
    }

    private void SelectPendingTab()
    {
        if (_pendingTab is not WorkspaceTab tab) return;
        _pendingTab = null;
        _tabs.SelectedIndex = (int)tab;
    }
}
