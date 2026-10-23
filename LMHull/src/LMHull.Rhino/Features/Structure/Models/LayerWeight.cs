namespace LMHull.Rhino.Features.Structure.Models;

/// <summary>
/// Trọng lượng gom theo một layer: số chi tiết và tổng tấn.
/// </summary>
public record LayerWeight(string Layer, int Count, double Tonnes);
