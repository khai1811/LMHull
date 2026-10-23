using System;
using LMHull.Core.Calculation;
using Rhino;
using Rhino.UI;

namespace LMHull.Rhino.Common;

/// <summary>
/// Hiển thị tiến độ CalculationBase lên thanh trạng thái Rhino và dòng lệnh.
/// Dùng với using để thanh tiến độ tự ẩn khi xong.
/// </summary>
public sealed class RhinoProgressReporter : IProgress<CalculationProgress>, IDisposable
{
    private string _lastMessage = string.Empty;

    public RhinoProgressReporter(string label)
    {
        StatusBar.ShowProgressMeter(0, 100, label, true, true);
    }

    public void Report(CalculationProgress value)
    {
        StatusBar.UpdateProgressMeter(value.Percent, true);

        if (value.Message != _lastMessage)
        {
            _lastMessage = value.Message;
            RhinoApp.WriteLine(value.Message);
        }
    }

    public void Dispose()
    {
        StatusBar.HideProgressMeter();
    }
}
