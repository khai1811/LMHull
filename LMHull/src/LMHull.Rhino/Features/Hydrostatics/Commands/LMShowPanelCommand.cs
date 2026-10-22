using LMHull.Rhino.Features.Hydrostatics.UI.Panels;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMShowPanelCommand : Command
{
    public override string EnglishName => "LMShowPanel";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        Guid panelId = LMHullMainPanel.PanelId;
        bool isVisible = Panels.IsPanelVisible(panelId);

        if (!isVisible)
        {
            Panels.OpenPanel(panelId);
        }
        else
        {
            Panels.ClosePanel(panelId);
        }

        return Result.Success;
    }
}