using System;
using System.Collections.Generic;
using System.Linq;
using LMHull.Core.Enums;

namespace LMHull.Core.Models;

public class StructuralNode
{
    private readonly List<StructuralNode> _children = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public StructuralNodeType NodeType { get; set; }

    // Quy cách và thuộc tính vật liệu
    public double ThicknessMm { get; set; } = 10.0;
    public string Material { get; set; } = "Steel AH36";
    public double WeightTonnes { get; set; } = 0.0;
    public bool IsVisible { get; set; } = true;

    // Liên kết với đối tượng hình học 3D thực tế trong Rhino
    public Guid RhinoGeometryId { get; set; } = Guid.Empty;
    public bool HasGeometry => RhinoGeometryId != Guid.Empty;

    // Quan hệ phân cấp hình cây (ExpressMarine Tree Structure)
    public StructuralNode? Parent { get; private set; }
    public IReadOnlyList<StructuralNode> Children => _children;
    public bool IsLeaf => _children.Count == 0;

    /// <summary>
    /// Node lá trả về trọng lượng của chính nó; node nhóm cộng dồn từ các node con.
    /// </summary>
    public double TotalWeightTonnes => IsLeaf ? WeightTonnes : _children.Sum(c => c.TotalWeightTonnes);

    /// <summary>
    /// Chỉ mặt boong và vách mới dựng được từ nét vẽ 2D trên GA.
    /// </summary>
    public bool IsPickableFromGA => NodeType is StructuralNodeType.DeckPlate
        or StructuralNodeType.TransverseBulkhead
        or StructuralNodeType.LongitudinalBulkhead;

    public StructuralNode(string name, StructuralNodeType nodeType)
    {
        Name = name;
        NodeType = nodeType;
    }

    public void AddChild(StructuralNode child)
    {
        child.Parent?.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
    }

    public void RemoveChild(StructuralNode child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }

    /// <summary>
    /// Duyệt toàn bộ node con cháu theo chiều sâu (không gồm chính node này).
    /// </summary>
    public IEnumerable<StructuralNode> Descendants()
    {
        var stack = new Stack<StructuralNode>();
        for (int i = _children.Count - 1; i >= 0; i--) stack.Push(_children[i]);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (int i = node._children.Count - 1; i >= 0; i--) stack.Push(node._children[i]);
        }
    }

    public IEnumerable<StructuralNode> Leaves() => Descendants().Where(n => n.IsLeaf);

    /// <summary>
    /// Chuỗi node cha từ cha trực tiếp lên tới gốc.
    /// </summary>
    public IEnumerable<StructuralNode> Ancestors()
    {
        for (var p = Parent; p != null; p = p.Parent) yield return p;
    }

    public StructuralNode? FindById(Guid id) => Id == id ? this : Descendants().FirstOrDefault(n => n.Id == id);

    public void LinkGeometry(Guid rhinoObjectId, double weightTonnes)
    {
        RhinoGeometryId = rhinoObjectId;
        WeightTonnes = weightTonnes;
    }

    public void UnlinkGeometry()
    {
        RhinoGeometryId = Guid.Empty;
        WeightTonnes = 0.0;
    }
}
