using System.Linq;
using LMHull.Core.Calculation;
using LMHull.Core.Services;
using LMHull.Core.Validation;
using LMHull.Rhino.Features.Structure.Calculations;
using LMHull.Rhino.Features.Structure.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class WeightEstimationService
{
    /// <summary>
    /// Quét các Brep thuộc các Layer kết cấu thực tế để tính tổng khối lượng và trọng tâm CoG.
    /// </summary>
    public static WeightResult ComputeStructuralWeight(ICadHandle cad, string defaultMaterialName = "Steel AH36")
    {
        return new WeightEstimationCalculation().Run(new WeightInput(cad, defaultMaterialName)).Value ?? new WeightResult();
    }
}
