using LMHull.Core.Models;
using LMHull.Rhino.Infrastructure.Cad;

namespace LMHull.Rhino.Features.Structure.Models;

/// <param name="Limits">Vùng giới hạn theo trọng tâm từng chi tiết; null = không giới hạn.</param>
public record WeightInput(ICadHandle Cad, string DefaultMaterialName = "Steel AH36", CalculationLimits? Limits = null);
