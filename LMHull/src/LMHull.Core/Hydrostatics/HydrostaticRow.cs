namespace LMHull.Core.Hydrostatics;

/// <summary>
/// Đặc trưng thủy tĩnh tại một mớn nước T. LCB / LCF đo từ gốc tham chiếu dọc, KB đo từ đường cơ bản.
/// </summary>
public record HydrostaticRow(
    double Draft,
    double Volume,
    double Displacement,
    double WaterplaneArea,
    double BlockCoefficient,
    double KB,
    double LCB,
    double LCF);
