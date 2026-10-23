using System;
using System.Runtime.InteropServices;
using LMHull.Rhino.Infrastructure;
using Rhino.PlugIns;

// 1. Khai báo các thuộc tính ở cấp độ Assembly tại đầu file
[assembly: Guid("F9812A1B-4C61-4DC3-8869-E54A92B43D1A")]
[assembly: PlugInDescription(DescriptionType.Organization, "LasanMarine")]

namespace LMHull.Rhino;

/// <summary>
/// Điểm vào của Plugin LMHull.
/// </summary>
[Guid("F9812A1B-4C61-4DC3-8869-E54A92B43D1A")]
public class LMHullPlugin : PlugIn
{
    public static LMHullPlugin Instance { get; private set; } = null!;

    public Guid LastHullGuid { get; set; } = Guid.Empty;

    public LMHullPlugin()
    {
        Instance = this;
    }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        ServiceRegistry.Initialize();

        // Một panel chung "LMHull" gom tab Lệnh, Thủy tĩnh, Kết cấu và Kết quả (mở bằng lệnh LMHull)
        global::Rhino.UI.Panels.RegisterPanel(this, typeof(UI.LMHullWorkspacePanel), "LMHull", (System.Drawing.Icon?)null);
        return LoadReturnCode.Success;
    }
}