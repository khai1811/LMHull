namespace LMHull.Core.Models;

public class ShipResult
{
    public ShipInput Input { get; set; }
    public double LBRatio { get; set; }
    public double BTRatio { get; set; }
    public double BoxVolume { get; set; }
    public double Displacement { get; set; }

    public ShipResult(ShipInput input)
    {
        Input = input;
    }
}