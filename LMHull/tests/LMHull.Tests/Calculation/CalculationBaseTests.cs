using System;
using System.Collections.Generic;
using System.Threading;
using LMHull.Core.Calculation;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using LMHull.Core.Validation;
using Xunit;

namespace LMHull.Tests.Calculation;

public class CalculationBaseTests
{
    private sealed class FakeCalculation : CalculationBase<int, int>
    {
        public bool Executed { get; private set; }
        public Exception? ThrowOnExecute { get; set; }

        public override string Name => "Fake";

        protected override ValidationResult Validate(int input)
        {
            var validation = new ValidationResult();
            if (input < 0) validation.AddError("Input phải >= 0");
            return validation;
        }

        protected override int Execute(int input, CalculationContext context)
        {
            Executed = true;
            if (ThrowOnExecute != null) throw ThrowOnExecute;
            for (int i = 0; i < 4; i++)
            {
                context.ThrowIfCancellationRequested();
                context.ReportStep(i, 4, $"Bước {i + 1}");
            }
            context.Log("done");
            return input * 2;
        }
    }

    private sealed class ListProgress : IProgress<CalculationProgress>
    {
        public List<CalculationProgress> Items { get; } = new();
        public void Report(CalculationProgress value) => Items.Add(value);
    }

    [Fact]
    public void Run_Success_ReturnsValueAndReportsZeroToHundred()
    {
        var calc = new FakeCalculation();
        var progress = new ListProgress();

        var result = calc.Run(21, progress);

        Assert.Equal(CalculationStatus.Succeeded, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(0, progress.Items[0].Percent);
        Assert.Equal(100, progress.Items[^1].Percent);
        Assert.Contains("done", result.Logs);
        Assert.Equal(CalculationStatus.Succeeded, calc.Status);
    }

    [Fact]
    public void Run_InvalidInput_FailsWithoutExecuting()
    {
        var calc = new FakeCalculation();

        var result = calc.Run(-1);

        Assert.Equal(CalculationStatus.Failed, result.Status);
        Assert.Contains("Input phải >= 0", result.Errors);
        Assert.False(calc.Executed);
    }

    [Fact]
    public void Run_ExecuteThrows_ReturnsFailedWithMessage()
    {
        var calc = new FakeCalculation { ThrowOnExecute = new InvalidOperationException("boom") };

        var result = calc.Run(1);

        Assert.Equal(CalculationStatus.Failed, result.Status);
        Assert.Contains("boom", result.Errors);
    }

    [Fact]
    public void Run_CancelledToken_ReturnsCancelled()
    {
        var calc = new FakeCalculation();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = calc.Run(1, cancellationToken: cts.Token);

        Assert.Equal(CalculationStatus.Cancelled, result.Status);
        Assert.False(calc.Executed);
    }

    [Fact]
    public void Run_RaisesCompletedExactlyOnce()
    {
        var calc = new FakeCalculation();
        int completedCount = 0;
        calc.Completed += (_, _) => completedCount++;

        calc.Run(1);

        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void ShipCalculator_Calculate_KeepsLegacyBehaviour()
    {
        var calc = new ShipCalculator();
        string? message = null;
        calc.OnCalculated += m => message = m;

        var result = calc.Calculate(new ShipInput("Test", 40, 10, 2), WaterType.SeaWater);

        Assert.Equal(4.0, result.LBRatio);
        Assert.Equal(800.0, result.BoxVolume);
        Assert.Equal(820.0, result.Displacement);
        Assert.Equal("[LMHull.Core] Đã tính xong: Test", message);
    }

    [Fact]
    public void ShipCalculator_InvalidDimensions_ThrowsArgumentException()
    {
        var ship = new ShipInput("Test", 40, 10, 2) { Beam = 0 };

        Assert.Throws<ArgumentException>(() => new ShipCalculator().Calculate(ship));
    }
}
