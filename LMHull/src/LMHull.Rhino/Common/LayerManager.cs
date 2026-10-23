using System.Drawing;
using LMHull.Rhino.Infrastructure.Cad;

namespace LMHull.Rhino.Common;

/// <summary>
/// Các layer tiêu chuẩn của LMHull (Hull, Reference, Results); việc tạo layer do ICadHandle đảm nhận.
/// </summary>
public static class LayerManager
{
    public const string HullLayerName = "Hull";
    public const string ReferenceLayerName = "Reference";
    public const string ResultsLayerName = "Results";

    public static readonly CadLayer Hull = new(HullLayerName, Color.FromArgb(200, 40, 40));         // Đỏ sẫm cho vỏ tàu
    public static readonly CadLayer Reference = new(ReferenceLayerName, Color.FromArgb(0, 120, 215)); // Xanh dương cho đường tham chiếu
    public static readonly CadLayer Results = new(ResultsLayerName, Color.FromArgb(40, 160, 60));     // Xanh lá cho kết quả tính toán

    /// <summary>
    /// Đảm bảo bộ 3 layer Hull, Reference, Results đã tồn tại.
    /// </summary>
    public static void EnsureStandardLayers(ICadHandle cad)
    {
        cad.Layer(Hull.Path, Hull.Color);
        cad.Layer(Reference.Path, Reference.Color);
        cad.Layer(Results.Path, Results.Color);
    }
}
