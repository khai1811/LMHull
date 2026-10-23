namespace LMHull.Core.Calculation;

/// <summary>
/// Cộng dồn trọng lượng và mô men để ra tổng trọng lượng và trọng tâm (CoG); bỏ qua phần tử có trọng lượng ≤ 0.
/// </summary>
public sealed class WeightAggregator
{
    private double _momentX;
    private double _momentY;
    private double _momentZ;

    public int Count { get; private set; }
    public double TotalWeight { get; private set; }

    public void Add(double weight, double x, double y, double z)
    {
        if (weight <= 0 || double.IsNaN(weight)) return;

        TotalWeight += weight;
        _momentX += weight * x;
        _momentY += weight * y;
        _momentZ += weight * z;
        Count++;
    }

    /// <summary>
    /// Trọng tâm (X, Y, Z); null khi chưa có phần tử nào.
    /// </summary>
    public (double X, double Y, double Z)? CenterOfGravity =>
        TotalWeight > 0 ? (_momentX / TotalWeight, _momentY / TotalWeight, _momentZ / TotalWeight) : null;
}
