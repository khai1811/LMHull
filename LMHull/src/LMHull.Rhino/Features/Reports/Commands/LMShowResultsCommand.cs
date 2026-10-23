using LMHull.Rhino.UI;
using Rhino;
using Rhino.Commands;

namespace LMHull.Rhino.Features.Reports.Commands;

/// <summary>
/// Giữ lại để tương thích: mở panel chung LMHull ở tab Kết quả.
/// </summary>
public class LMShowResultsCommand : Command
{
    public override string EnglishName => "LMShowResults";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        LMHullWorkspacePanel.Show(WorkspaceTab.Results);
        return Result.Success;
    }
}
