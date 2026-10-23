using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.UI.Commands;

/// <summary>
/// Lệnh duy nhất mở toàn bộ giao diện LMHull (tab Lệnh, Thủy tĩnh, Kết cấu, Kết quả).
/// </summary>
public class LMHullCommand : Command
{
    public override string EnglishName => "LMHull";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        LMHullWorkspacePanel.Show(WorkspaceTab.Commands);
        return Result.Success;
    }
}
