using System.Drawing;
using Rhino;
using Rhino.DocObjects;

namespace LMHull.Rhino.Common;

/// <summary>
/// Quản lý việc tạo và lấy các layer tiêu chuẩn trong bảng doc.Layers của Rhino.
/// </summary>
public static class LayerManager
{
    public const string HullLayerName = "Hull";
    public const string ReferenceLayerName = "Reference";
    public const string ResultsLayerName = "Results";

    /// <summary>
    /// Đảm bảo bộ 3 layer Hull, Reference, Results đã tồn tại trong doc.Layers.
    /// </summary>
    public static void EnsureStandardLayers(RhinoDoc doc)
    {
        GetOrCreateLayer(doc, HullLayerName, Color.FromArgb(200, 40, 40));      // Đỏ sẫm cho vỏ tàu
        GetOrCreateLayer(doc, ReferenceLayerName, Color.FromArgb(0, 120, 215)); // Xanh dương cho đường tham chiếu
        GetOrCreateLayer(doc, ResultsLayerName, Color.FromArgb(40, 160, 60));   // Xanh lá cho kết quả tính toán
    }

    /// <summary>
    /// Tìm layer theo tên, nếu chưa có thì thêm mới vào bảng doc.Layers và trả về LayerIndex.
    /// </summary>
    public static int GetOrCreateLayer(RhinoDoc doc, string layerName, Color color)
    {
        // Kiểm tra xem layer đã tồn tại trong doc.Layers chưa
        int existingIndex = doc.Layers.FindByFullPath(layerName, -1);
        if (existingIndex >= 0)
        {
            return existingIndex;
        }

        // Tạo layer mới và thêm vào bảng doc.Layers
        var newLayer = new Layer
        {
            Name = layerName,
            Color = color
        };

        return doc.Layers.Add(newLayer);
    }
}