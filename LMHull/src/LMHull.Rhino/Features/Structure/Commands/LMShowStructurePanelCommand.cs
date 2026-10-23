using LMHull.Rhino.UI;
using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.Features.Structure.Commands;

/// <summary>
/// Giữ lại để tương thích: mở panel chung LMHull ở tab Kết cấu.
/// </summary>
public class LMShowStructurePanelCommand : Command
{
    public override string EnglishName => "LMShowStructurePanel";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        LMHullWorkspacePanel.Show(WorkspaceTab.Structure);
        return Result.Success;
    }
}
