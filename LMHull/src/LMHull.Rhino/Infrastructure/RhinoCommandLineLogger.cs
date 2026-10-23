using System;
using LMHull.Core.Abstractions;
using Rhino;

namespace LMHull.Rhino.Infrastructure;

/// <summary>
/// Đích log trên dòng lệnh Rhino: chỉ hiện cảnh báo và lỗi để không làm rối dòng lệnh.
/// </summary>
public class RhinoCommandLineLogger : IAppLogger
{
    public void Info(string message)
    {
    }

    public void Warn(string message) => RhinoApp.WriteLine("[Cảnh báo]: " + message);

    public void Error(string message, Exception? exception = null) => RhinoApp.WriteLine("[Lỗi]: " + message);
}
