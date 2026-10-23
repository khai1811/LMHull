using LMHull.Rhino.Infrastructure;
using LMHull.Rhino.UI.Dialogs;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace LMHull.Rhino.UI.Commands;

/// <summary>
/// Mở hộp thoại cấu hình LMHull và lưu vào %AppData%/LMHull/settings.json.
/// </summary>
public class LMSettingsCommand : Command
{
    public override string EnglishName => "LMSettings";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var dialog = new LMSettingsDialog(ServiceRegistry.Settings);
        var updated = dialog.ShowModal(RhinoEtoApp.MainWindow);
        if (updated == null) return Result.Cancel;

        ServiceRegistry.SaveSettings(updated);
        RhinoApp.WriteLine($"[LMSettings] Đã lưu cấu hình: nước biển {updated.SeaWaterDensity:F3} t/m³, vật liệu '{updated.DefaultMaterial}', bề dày {updated.DefaultThicknessMm:F1} mm.");
        return Result.Success;
    }
}
