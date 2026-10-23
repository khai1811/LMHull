using System;
using System.Collections.Generic;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Rhino.Infrastructure.Cad;
using Rhino.Geometry;

namespace LMHull.Rhino.Features.Structure.Services;

public static class LiveStructureScanner
{
    /// <summary>
    /// Quét các đối tượng hình học 3D thực tế trong tài liệu Rhino để xây dựng cây kết cấu thật.
    /// Không sử dụng bất kỳ dữ liệu mẫu cố định nào.
    /// </summary>
    public static StructuralNode BuildTreeFromDocument(ICadHandle cad, string? documentName)
    {
        string projectName = string.IsNullOrWhiteSpace(documentName) ? "Dự án tàu hiện hành" : documentName.Replace(".3dm", "");
        var root = new StructuralNode(projectName, StructuralNodeType.Project);

        var shellGroup = new StructuralNode("Tôn vỏ (Shell Plating)", StructuralNodeType.ShellGroup);
        var deckGroup = new StructuralNode("Mặt boong (Decks)", StructuralNodeType.DeckGroup);
        var bhGroup = new StructuralNode("Hệ vách ngăn (Bulkheads)", StructuralNodeType.BulkheadGroup);
        var frameGroup = new StructuralNode("Khung sườn & Nẹp (Frames & Stiffeners)", StructuralNodeType.FrameGroup);
        var plateGroup = new StructuralNode("Tấm tôn đặc (Solid Plates)", StructuralNodeType.ShellGroup);

        foreach (var obj in cad.FindBreps())
        {
            if (obj.Geometry is not Brep brep) continue;

            string fullPath = obj.LayerPath.ToLowerInvariant();
            string layerName = fullPath[(fullPath.LastIndexOf('/') + 1)..];

            // Đọc thông số thực tế từ UserDictionary và tính khối lượng thực tế của chi tiết
            double thickness = StructureObjectData.ReadThicknessMm(obj);
            string material = StructureObjectData.ReadMaterial(obj);
            double weightTonnes = StructureObjectData.ComputeWeightTonnes(brep, material);

            string objName = string.IsNullOrWhiteSpace(obj.Name) ? $"Part_{obj.Id.ToString()[..6]}" : obj.Name;

            var node = new StructuralNode(objName, ClassifyNodeType(fullPath, layerName))
            {
                ThicknessMm = thickness,
                Material = material
            };
            node.LinkGeometry(obj.Id, weightTonnes);

            // Phân loại node vào đúng nhánh cây thực tế
            if (fullPath.Contains("hull") || layerName.Contains("hull"))
            {
                shellGroup.AddChild(node);
            }
            else if (fullPath.Contains("deck") || layerName.Contains("deck"))
            {
                deckGroup.AddChild(node);
            }
            else if (fullPath.Contains("bulkhead") || fullPath.Contains("01_bulkh") || layerName.Contains("bulkh"))
            {
                bhGroup.AddChild(node);
            }
            else if (fullPath.Contains("frame") || fullPath.Contains("stiffener") || layerName.Contains("stiff"))
            {
                frameGroup.AddChild(node);
            }
            else if (fullPath.Contains("solidplate") || layerName.Contains("plate"))
            {
                plateGroup.AddChild(node);
            }
            else
            {
                shellGroup.AddChild(node);
            }
        }

        if (shellGroup.Children.Count > 0) root.AddChild(shellGroup);
        if (deckGroup.Children.Count > 0) root.AddChild(deckGroup);
        if (bhGroup.Children.Count > 0) root.AddChild(bhGroup);
        if (frameGroup.Children.Count > 0) root.AddChild(frameGroup);
        if (plateGroup.Children.Count > 0) root.AddChild(plateGroup);

        return root;
    }

    private static StructuralNodeType ClassifyNodeType(string path, string name)
    {
        if (path.Contains("deck") || name.Contains("deck")) return StructuralNodeType.DeckPlate;
        if (path.Contains("longbulkh") || name.Contains("longbulkh")) return StructuralNodeType.LongitudinalBulkhead;
        if (path.Contains("bulkh") || name.Contains("bulkh")) return StructuralNodeType.TransverseBulkhead;
        if (path.Contains("frame") || name.Contains("frame")) return StructuralNodeType.WebFrame;
        return StructuralNodeType.ShellPortSide;
    }
}