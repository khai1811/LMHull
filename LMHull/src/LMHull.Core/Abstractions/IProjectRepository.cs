using LMHull.Core.Models;

namespace LMHull.Core.Abstractions;

/// <summary>
/// Lưu / đọc dữ liệu dự án (cây kết cấu, thuộc tính, liên kết hình học) ra kho lưu trữ.
/// </summary>
public interface IProjectRepository
{
    void Save(ProjectData data, string path);

    ProjectData Load(string path);

    bool Exists(string path);
}
