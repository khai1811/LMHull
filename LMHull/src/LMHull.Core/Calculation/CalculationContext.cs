using System;
using System.Collections.Generic;
using System.Threading;

namespace LMHull.Core.Calculation;

/// <summary>
/// Ngữ cảnh truyền vào bước Execute: báo tiến độ, kiểm tra hủy và ghi nhật ký.
/// </summary>
public class CalculationContext
{
    private readonly Action<CalculationProgress> _report;
    private readonly List<string> _logs = new();

    public CancellationToken CancellationToken { get; }

    internal CalculationContext(Action<CalculationProgress> report, CancellationToken cancellationToken)
    {
        _report = report;
        CancellationToken = cancellationToken;
    }

    internal IReadOnlyList<string> Logs => _logs;

    public void Report(int percent, string message)
    {
        _report(new CalculationProgress(Math.Clamp(percent, 0, 100), message));
    }

    /// <summary>
    /// Báo tiến độ theo vòng lặp: bước index (0-based) trên tổng total, ánh xạ vào khoảng [fromPercent, toPercent].
    /// </summary>
    public void ReportStep(int index, int total, string message, int fromPercent = 0, int toPercent = 100)
    {
        if (total <= 0) return;
        double fraction = (double)(index + 1) / total;
        Report(fromPercent + (int)Math.Round((toPercent - fromPercent) * fraction), message);
    }

    public void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

    public void Log(string message) => _logs.Add(message);
}
