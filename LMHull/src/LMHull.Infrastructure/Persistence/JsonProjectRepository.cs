using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LMHull.Core.Abstractions;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Reporting;

namespace LMHull.Infrastructure.Persistence;

/// <summary>
/// Lưu / đọc dự án ra tệp JSON (*.lmhull.json).
/// </summary>
public class JsonProjectRepository : IProjectRepository
{
    public const int CurrentSchemaVersion = 1;
    public const string FileExtension = ".lmhull.json";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public bool Exists(string path) => File.Exists(path);

    public void Save(ProjectData data, string path)
    {
        var dto = new ProjectFileDto
        {
            SchemaVersion = CurrentSchemaVersion,
            Name = data.Name,
            SavedAt = DateTime.Now,
            Nodes = Flatten(data.Root).ToList(),
            Results = data.Results.Select(ToDto).ToList()
        };

        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, JsonSerializer.Serialize(dto, Options), new UTF8Encoding(true));
    }

    public ProjectData Load(string path)
    {
        ProjectFileDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<ProjectFileDto>(File.ReadAllText(path))
                  ?? throw new InvalidDataException("Tệp dự án rỗng.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Tệp dự án không đúng định dạng JSON: {ex.Message}", ex);
        }

        if (dto.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Không hỗ trợ phiên bản tệp dự án {dto.SchemaVersion} (cần {CurrentSchemaVersion}).");

        var data = new ProjectData(BuildTree(dto.Nodes)) { Name = dto.Name, SavedAt = dto.SavedAt };
        data.Results.AddRange(dto.Results.Select(FromDto));
        return data;
    }

    private static IEnumerable<NodeDto> Flatten(StructuralNode root)
    {
        foreach (var node in new[] { root }.Concat(root.Descendants()))
        {
            yield return new NodeDto
            {
                Id = node.Id,
                ParentId = node.Parent?.Id,
                Name = node.Name,
                NodeType = node.NodeType.ToString(),
                ThicknessMm = node.ThicknessMm,
                Material = node.Material,
                WeightTonnes = node.WeightTonnes,
                RhinoGeometryId = node.RhinoGeometryId
            };
        }
    }

    // Các node được lưu theo thứ tự cha trước con nên chỉ cần một lượt để dựng lại cây
    private static StructuralNode BuildTree(List<NodeDto> nodes)
    {
        var byId = new Dictionary<Guid, StructuralNode>();
        StructuralNode? root = null;

        foreach (var dto in nodes)
        {
            if (!Enum.TryParse(dto.NodeType, out StructuralNodeType type))
                throw new InvalidDataException($"Kiểu node không hợp lệ: '{dto.NodeType}'.");

            var node = new StructuralNode(dto.Name, type)
            {
                Id = dto.Id,
                ThicknessMm = dto.ThicknessMm,
                Material = dto.Material,
                WeightTonnes = dto.WeightTonnes,
                RhinoGeometryId = dto.RhinoGeometryId
            };
            byId[node.Id] = node;

            if (dto.ParentId is Guid parentId)
            {
                if (!byId.TryGetValue(parentId, out var parent))
                    throw new InvalidDataException($"Node '{dto.Name}' tham chiếu tới node cha không tồn tại.");
                parent.AddChild(node);
            }
            else if (root == null)
            {
                root = node;
            }
            else
            {
                throw new InvalidDataException("Tệp dự án có nhiều hơn một node gốc.");
            }
        }

        return root ?? throw new InvalidDataException("Tệp dự án không có node gốc.");
    }

    private static ResultTableDto ToDto(ResultTable table) => new()
    {
        Title = table.Title,
        Source = table.Source,
        CreatedAt = table.CreatedAt,
        Columns = table.Columns.ToList(),
        Rows = table.Rows.ToList(),
        Notes = table.Notes.ToList()
    };

    private static ResultTable FromDto(ResultTableDto dto)
    {
        var table = new ResultTable(dto.Title, dto.Source, dto.Columns.ToArray()) { CreatedAt = dto.CreatedAt };
        foreach (var row in dto.Rows) table.AddRow(row);
        table.Notes.AddRange(dto.Notes);
        return table;
    }
}
