using System;
using System.Collections.Generic;
using LMHull.Core.Reporting;

namespace LMHull.Core.Models;

/// <summary>
/// Dữ liệu một dự án LMHull được lưu ra tệp: cây kết cấu và (tùy chọn) lịch sử bảng kết quả.
/// </summary>
public class ProjectData
{
    public string Name { get; set; } = "LMHull Project";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public StructuralNode Root { get; set; }
    public List<ResultTable> Results { get; set; } = new();

    public ProjectData(StructuralNode root)
    {
        Root = root;
        Name = root.Name;
    }
}
