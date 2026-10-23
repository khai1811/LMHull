using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Abstractions;

namespace LMHull.Infrastructure.Logging;

/// <summary>
/// Chuyển mỗi bản ghi log tới nhiều đích (ví dụ: tệp + dòng lệnh Rhino).
/// </summary>
public class CompositeLogger : IAppLogger
{
    private readonly IReadOnlyList<IAppLogger> _sinks;

    public CompositeLogger(params IAppLogger[] sinks)
    {
        _sinks = sinks.ToList();
    }

    public void Info(string message)
    {
        foreach (var sink in _sinks) sink.Info(message);
    }

    public void Warn(string message)
    {
        foreach (var sink in _sinks) sink.Warn(message);
    }

    public void Error(string message, Exception? exception = null)
    {
        foreach (var sink in _sinks) sink.Error(message, exception);
    }
}
