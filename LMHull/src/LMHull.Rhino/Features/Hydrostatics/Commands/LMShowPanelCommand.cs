using LMHull.Rhino.UI;
using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

/// <summary>
/// Giữ lại để tương thích: mở panel chung LMHull ở tab Thủy tĩnh.
/// </summary>
public class LMShowPanelCommand : Command
{
    public override string EnglishName => "LMShowPanel";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        LMHullWorkspacePanel.Show(WorkspaceTab.Hydrostatics);
        return Result.Success;
    }
}
