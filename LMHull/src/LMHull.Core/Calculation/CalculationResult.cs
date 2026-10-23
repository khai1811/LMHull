using System;
using System.Collections.Generic;

namespace LMHull.Core.Calculation;

/// <summary>
/// Kết quả của một lần chạy CalculationBase: trạng thái, giá trị, lỗi, nhật ký và thời gian chạy.
/// </summary>
public class CalculationResult<TResult>
{
    public CalculationStatus Status { get; init; }
    public TResult? Value { get; init; }
    public List<string> Errors { get; init; } = new();
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
    public TimeSpan Elapsed { get; init; }

    public bool IsSuccess => Status == CalculationStatus.Succeeded;

    public string GetErrorMessage() => string.Join("\n• ", Errors);
}
