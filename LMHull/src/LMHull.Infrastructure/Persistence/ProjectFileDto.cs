using System;
using System.Collections.Generic;

namespace LMHull.Infrastructure.Persistence;

/// <summary>
/// Định dạng tệp *.lmhull.json: cây kết cấu lưu phẳng theo ParentId để dễ đọc và nâng cấp schema.
/// </summary>
public class ProjectFileDto
{
    public int SchemaVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
    public List<NodeDto> Nodes { get; set; } = new();
    public List<ResultTableDto> Results { get; set; } = new();
}
