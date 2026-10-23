using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Validation;

namespace LMHull.Core.Models;

public record ShipCalcInput(ShipInput Ship, WaterType Water = WaterType.SeaWater);
