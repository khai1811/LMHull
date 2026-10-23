using System;
using System.Diagnostics;
using System.Threading;
using LMHull.Core.Validation;

namespace LMHull.Core.Calculation;

/// <summary>
/// Lớp cơ sở quản lý vòng đời một lần tính toán (Template Method):
/// Validate → Execute (báo tiến độ, cho phép hủy) → CalculationResult có trạng thái, lỗi và thời gian chạy.
/// </summary>
public abstract class CalculationBase<TInput, TResult>
{
    public abstract string Name { get; }

    public CalculationStatus Status { get; private set; } = CalculationStatus.Pending;

    public event EventHandler<CalculationProgress>? ProgressChanged;
    public event EventHandler<CalculationResult<TResult>>? Completed;

    public CalculationResult<TResult> Run(TInput input, IProgress<CalculationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Status = CalculationStatus.Running;

        var context = new CalculationContext(p =>
        {
            progress?.Report(p);
            ProgressChanged?.Invoke(this, p);
        }, cancellationToken);

        CalculationResult<TResult> result;
        try
        {
            context.Report(0, $"[{Name}] Bắt đầu tính toán...");
            cancellationToken.ThrowIfCancellationRequested();

            var validation = Validate(input);
            if (!validation.IsValid)
            {
                result = Finish(CalculationStatus.Failed, default, validation.Errors.ToArray(), context, stopwatch);
            }
            else
            {
                TResult value = Execute(input, context);
                context.Report(100, $"[{Name}] Hoàn tất.");
                result = Finish(CalculationStatus.Succeeded, value, Array.Empty<string>(), context, stopwatch);
            }
        }
        catch (OperationCanceledException)
        {
            result = Finish(CalculationStatus.Cancelled, default, new[] { $"[{Name}] Đã hủy tính toán." }, context, stopwatch);
        }
        catch (Exception ex)
        {
            result = Finish(CalculationStatus.Failed, default, new[] { ex.Message }, context, stopwatch);
        }

        Completed?.Invoke(this, result);
        return result;
    }

    /// <summary>
    /// Kiểm tra đầu vào trước khi tính; mặc định luôn hợp lệ.
    /// </summary>
    protected virtual ValidationResult Validate(TInput input) => new();

    protected abstract TResult Execute(TInput input, CalculationContext context);

    private CalculationResult<TResult> Finish(CalculationStatus status, TResult? value, string[] errors, CalculationContext context, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        Status = status;
        return new CalculationResult<TResult>
        {
            Status = status,
            Value = value,
            Errors = new(errors),
            Logs = context.Logs,
            Elapsed = stopwatch.Elapsed
        };
    }
}
