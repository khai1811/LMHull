using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Validation;

namespace LMHull.Core.Services;

public class ShipCalculator : CalculationBase<ShipCalcInput, ShipResult>
{
    public static readonly Dictionary<WaterType, double> WaterDensities = new()
    {
        { WaterType.FreshWater, 1.000 },
        { WaterType.SeaWater, 1.025 }
    };

    private readonly IReadOnlyDictionary<WaterType, double> _densities;

    public event Action<string>? OnCalculated;

    public override string Name => "ShipCalculator";

    public ShipCalculator() : this(WaterDensities)
    {
    }

    /// <summary>
    /// Dùng tỷ trọng nước biển / nước ngọt theo cấu hình người dùng.
    /// </summary>
    public ShipCalculator(LMHullSettings settings) : this(new Dictionary<WaterType, double>
    {
        { WaterType.FreshWater, settings.FreshWaterDensity },
        { WaterType.SeaWater, settings.SeaWaterDensity }
    })
    {
    }

    private ShipCalculator(IReadOnlyDictionary<WaterType, double> densities)
    {
        _densities = densities;
        Completed += (_, result) =>
        {
            if (result.IsSuccess) OnCalculated?.Invoke($"[LMHull.Core] Đã tính xong: {result.Value!.Input.Name}");
        };
    }

    /// <summary>
    /// Tính nhanh, giữ API cũ: ném ArgumentException nếu tính toán thất bại.
    /// </summary>
    public ShipResult Calculate(ShipInput input, WaterType waterType = WaterType.SeaWater)
    {
        var result = Run(new ShipCalcInput(input, waterType));
        if (!result.IsSuccess) throw new ArgumentException(result.GetErrorMessage());
        return result.Value!;
    }

    protected override ValidationResult Validate(ShipCalcInput input)
    {
        var validation = new ValidationResult();
        if (input.Ship == null)
        {
            validation.AddError("Thiếu thông số tàu.");
            return validation;
        }
        if (input.Ship.Length <= 0 || input.Ship.Beam <= 0 || input.Ship.Draft <= 0)
            validation.AddError("Kích thước L, B, T phải lớn hơn 0.");
        if (!_densities.ContainsKey(input.Water))
            validation.AddError($"Không hỗ trợ loại nước: {input.Water}.");
        return validation;
    }

    protected override ShipResult Execute(ShipCalcInput input, CalculationContext context)
    {
        var ship = input.Ship;
        double density = _densities[input.Water];
        double boxVolume = ship.Length * ship.Beam * ship.Draft;
        double displacement = boxVolume * density;

        return new ShipResult(ship)
        {
            LBRatio = Math.Round(ship.Length / ship.Beam, 2),
            BTRatio = Math.Round(ship.Beam / ship.Draft, 2),
            BoxVolume = Math.Round(boxVolume, 2),
            Displacement = Math.Round(displacement, 2)
        };
    }

    public List<ShipResult> FilterByLBRatio(IEnumerable<ShipResult> results, RangeFilter filter)
    {
        return results
            .Where(r => filter.IsInRange(r.LBRatio))
            .OrderByDescending(r => r.Displacement)
            .ToList();
    }
}
