namespace LMHull.Core.Abstractions;

/// <summary>
/// Đọc / ghi cấu hình; Load trả về giá trị mặc định nếu chưa có hoặc dữ liệu hỏng.
/// </summary>
public interface ISettingsStore<T> where T : class, new()
{
    T Load();

    void Save(T settings);
}
