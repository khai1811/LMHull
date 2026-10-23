using System.Linq;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Services;
using Xunit;

namespace LMHull.Tests.Structure;

public class StructuralNodeTests
{
    private static (StructuralNode root, StructuralNode groupA, StructuralNode leaf1, StructuralNode leaf2, StructuralNode leaf3) BuildSample()
    {
        var root = new StructuralNode("Root", StructuralNodeType.Project);
        var groupA = new StructuralNode("A", StructuralNodeType.BulkheadGroup);
        var groupB = new StructuralNode("B", StructuralNodeType.DeckGroup);
        var leaf1 = new StructuralNode("A1", StructuralNodeType.TransverseBulkhead) { WeightTonnes = 1.5 };
        var leaf2 = new StructuralNode("A2", StructuralNodeType.LongitudinalBulkhead) { WeightTonnes = 2.0 };
        var leaf3 = new StructuralNode("B1", StructuralNodeType.DeckPlate) { WeightTonnes = 0.5 };

        root.AddChild(groupA);
        root.AddChild(groupB);
        groupA.AddChild(leaf1);
        groupA.AddChild(leaf2);
        groupB.AddChild(leaf3);
        return (root, groupA, leaf1, leaf2, leaf3);
    }

    [Fact]
    public void AddChild_MovesNodeFromPreviousParent()
    {
        var (root, groupA, leaf1, _, _) = BuildSample();

        root.AddChild(leaf1);

        Assert.Same(root, leaf1.Parent);
        Assert.DoesNotContain(leaf1, groupA.Children);
        Assert.Contains(leaf1, root.Children);
    }

    [Fact]
    public void Descendants_And_Leaves_ReturnAllNodesDepthFirst()
    {
        var (root, _, _, _, _) = BuildSample();

        Assert.Equal(new[] { "A", "A1", "A2", "B", "B1" }, root.Descendants().Select(n => n.Name));
        Assert.Equal(new[] { "A1", "A2", "B1" }, root.Leaves().Select(n => n.Name));
    }

    [Fact]
    public void TotalWeightTonnes_SumsLeaves()
    {
        var (root, groupA, leaf1, _, _) = BuildSample();

        Assert.Equal(3.5, groupA.TotalWeightTonnes, 6);
        Assert.Equal(4.0, root.TotalWeightTonnes, 6);
        Assert.Equal(1.5, leaf1.TotalWeightTonnes, 6);
    }

    [Fact]
    public void FindById_And_Ancestors()
    {
        var (root, groupA, _, leaf2, _) = BuildSample();

        Assert.Same(leaf2, root.FindById(leaf2.Id));
        Assert.Same(root, root.FindById(root.Id));
        Assert.Null(groupA.FindById(System.Guid.NewGuid()));
        Assert.Equal(new[] { groupA, root }, leaf2.Ancestors());
    }

    [Fact]
    public void LinkAndUnlinkGeometry_UpdatesState()
    {
        var node = new StructuralNode("X", StructuralNodeType.DeckPlate);
        var id = System.Guid.NewGuid();

        node.LinkGeometry(id, 3.2);
        Assert.True(node.HasGeometry);
        Assert.Equal(3.2, node.WeightTonnes);

        node.UnlinkGeometry();
        Assert.False(node.HasGeometry);
        Assert.Equal(0.0, node.WeightTonnes);
    }

    [Fact]
    public void Template_HasStableIdsAcrossBuilds()
    {
        var first = ExpressMarineProjectBuilder.CreateExpressMarineTemplate("ShipA").Descendants().Select(n => n.Id).ToList();
        var second = ExpressMarineProjectBuilder.CreateExpressMarineTemplate("ShipB").Descendants().Select(n => n.Id).ToList();

        Assert.Equal(first, second);
        Assert.Equal(first.Count, first.Distinct().Count());
    }

    [Fact]
    public void Template_UsesCorrectNodeTypes()
    {
        var root = ExpressMarineProjectBuilder.CreateExpressMarineTemplate();
        var leaves = root.Leaves().ToList();

        Assert.Contains(leaves, n => n.Name == "LongBulks on Dk1" && n.NodeType == StructuralNodeType.LongitudinalBulkhead);
        Assert.Contains(leaves, n => n.Name == "Deck1Plane" && n.NodeType == StructuralNodeType.DeckPlate);
        Assert.All(leaves.Where(n => n.NodeType is StructuralNodeType.ShellPortSide or StructuralNodeType.ShellStarboardSide),
            n => Assert.False(n.IsPickableFromGA));
    }
}
