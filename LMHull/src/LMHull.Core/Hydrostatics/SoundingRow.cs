namespace LMHull.Core.Hydrostatics;

/// <summary>
/// Một mức đo của két: chiều cao chất lỏng (từ đáy két), thể tích, % đầy và VCG chất lỏng (từ đáy két).
/// </summary>
public record SoundingRow(double Sounding, double Volume, double Percent, double VCG);
