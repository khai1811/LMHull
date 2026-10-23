using System;
using System.Security.Cryptography;
using System.Text;
using LMHull.Core.Enums;
using LMHull.Core.Models;

namespace LMHull.Core.Services;

public static class ExpressMarineProjectBuilder
{
    public static StructuralNode CreateExpressMarineTemplate(string projectName = "NewProject")
    {
        var root = new StructuralNode(projectName, StructuralNodeType.Project);

        // 1. Shell
        var shell = AddNode(root, "Shell", StructuralNodeType.ShellGroup);
        AddNode(shell, "ShellPS", StructuralNodeType.ShellPortSide, 12.0);
        AddNode(shell, "ShellSB", StructuralNodeType.ShellStarboardSide, 12.0);

        // 2. Decks & Tiers (Double Bottom, Main Deck, Upper Deck,...)
        string[] tierNames = { "Tier1 (DoubleBottom)", "Tier2", "Tier3", "Tier4", "Tier5" };
        for (int i = 0; i < tierNames.Length; i++)
        {
            var tier = AddNode(root, tierNames[i], StructuralNodeType.DeckTier);
            AddNode(tier, $"Deck{i + 1}Plane", StructuralNodeType.DeckPlate, 10.0);

            var bulks = AddNode(tier, "Bulkheads", StructuralNodeType.BulkheadGroup);
            AddNode(bulks, $"TransBulks on Dk{i + 1}", StructuralNodeType.TransverseBulkhead, 10.0);
            AddNode(bulks, $"LongBulks on Dk{i + 1}", StructuralNodeType.LongitudinalBulkhead, 10.0);
        }

        return root;
    }

    // Id sinh từ đường dẫn node (không gồm tên dự án) để liên kết hình học còn khớp khi mở lại file
    private static StructuralNode AddNode(StructuralNode parent, string name, StructuralNodeType type, double thicknessMm = 10.0)
    {
        string path = parent.Parent == null ? name : $"{GetPath(parent)}/{name}";
        var node = new StructuralNode(name, type)
        {
            Id = DeterministicGuid(path),
            ThicknessMm = thicknessMm
        };
        parent.AddChild(node);
        return node;
    }

    private static string GetPath(StructuralNode node) =>
        node.Parent == null || node.Parent.Parent == null ? node.Name : $"{GetPath(node.Parent)}/{node.Name}";

    private static Guid DeterministicGuid(string path)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("LMHull.ExpressMarine/" + path));
        return new Guid(hash);
    }
}
