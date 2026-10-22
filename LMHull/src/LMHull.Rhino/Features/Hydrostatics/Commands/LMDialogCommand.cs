using LMHull.Rhino.Features.Hydrostatics.UI.Dialogs;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace LMHull.Rhino.Features.Hydrostatics.Commands;

public class LMDialogCommand : Command
{
    public override string EnglishName => "LMDialog";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // Khởi tạo hộp thoại Eto
        var dialog = new ShipInputDialog();

        // Hiển thị hộp thoại dạng Modal (khóa màn hình Rhino cho đến khi đóng hộp thoại)
        bool? result = dialog.ShowModal(RhinoEtoApp.MainWindow);

        return result == true ? Result.Success : Result.Nothing;
    }
}