using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Enums;
using LMHull.Core.Models;

namespace LMHull.Core.Services;

public class ShipCalculator
{
    public static readonly Dictionary<WaterType, double> WaterDensities = new()
    {
        { WaterType.FreshWater, 1.000 },
        { WaterType.SeaWater, 1.025 }
    };

    public event Action<string>? OnCalculated;

    public ShipResult Calculate(ShipInput input, WaterType waterType = WaterType.SeaWater)
    {
        double density = WaterDensities[waterType];
        double boxVolume = input.Length * input.Beam * input.Draft;
        double displacement = boxVolume * density;

        var result = new ShipResult(input)
        {
            LBRatio = Math.Round(input.Length / input.Beam, 2),
            BTRatio = Math.Round(input.Beam / input.Draft, 2),
            BoxVolume = Math.Round(boxVolume, 2),
            Displacement = Math.Round(displacement, 2)
        };

        OnCalculated?.Invoke($"[LMHull.Core] Đã tính xong: {input.Name}");
        return result;
    }

    public List<ShipResult> FilterByLBRatio(IEnumerable<ShipResult> results, RangeFilter filter)
    {
        return results
            .Where(r => filter.IsInRange(r.LBRatio))
            .OrderByDescending(r => r.Displacement)
            .ToList();
    }
}