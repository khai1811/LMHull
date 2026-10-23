using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using LMHull.Core.Models;

namespace LMHull.Rhino.Features.Structure.UI;

/// <summary>
/// Cây hiển thị StructuralNode: dựng một lần, sau đó chỉ vẽ lại các node thay đổi
/// (giữ nguyên trạng thái mở rộng của các nhánh).
/// </summary>
public class StructureTreeView : TreeGridView
{
    private readonly Dictionary<Guid, TreeGridItem> _itemsByNodeId = new();

    public event EventHandler<StructuralNode>? NodeSelected;

    public StructureTreeView(string headerText)
    {
        Size = new Size(240, 280);
        Columns.Add(new GridColumn
        {
            HeaderText = headerText,
            DataCell = new TextBoxCell
            {
                Binding = Binding.Delegate<TreeGridItem, string>(r => r.Tag is StructuralNode n ? GetDisplayText(n) : string.Empty)
            },
            AutoSize = true
        });

        SelectedItemChanged += (_, _) =>
        {
            if (SelectedItem is TreeGridItem { Tag: StructuralNode node }) NodeSelected?.Invoke(this, node);
        };
    }

    /// <summary>
    /// Node lá: ký hiệu đã/chưa dựng 3D; node nhóm: tổng trọng lượng cộng dồn.
    /// </summary>
    public static string GetDisplayText(StructuralNode node)
    {
        if (node.IsLeaf) return (node.HasGeometry ? "● " : "○ ") + node.Name;

        double total = node.TotalWeightTonnes;
        return total > 0 ? $"{node.Name} — {total:F3} t" : node.Name;
    }

    public void LoadTree(StructuralNode root)
    {
        _itemsByNodeId.Clear();
        var rootItem = CreateItem(root);
        rootItem.Expanded = true;
        DataStore = new TreeGridItemCollection { rootItem };
    }

    /// <summary>
    /// Vẽ lại các node đã đổi cùng các node cha của chúng; trả về tập node đã vẽ lại.
    /// </summary>
    public HashSet<StructuralNode> UpdateNodes(IEnumerable<StructuralNode> changedNodes)
    {
        var toReload = new HashSet<StructuralNode>();
        foreach (var node in changedNodes)
        {
            toReload.Add(node);
            toReload.UnionWith(node.Ancestors());
        }

        foreach (var node in toReload)
        {
            if (_itemsByNodeId.TryGetValue(node.Id, out var item)) ReloadItem(item, false);
        }
        return toReload;
    }

    private TreeGridItem CreateItem(StructuralNode node)
    {
        var item = new TreeGridItem { Tag = node, Expanded = false };
        _itemsByNodeId[node.Id] = item;

        foreach (var child in node.Children)
        {
            item.Children.Add(CreateItem(child));
        }
        return item;
    }
}
