namespace LMHull.Core.Calculation;

/// <summary>
/// Tiến độ của một lần tính: phần trăm (0–100) và thông điệp bước hiện tại.
/// </summary>
public record CalculationProgress(int Percent, string Message);
