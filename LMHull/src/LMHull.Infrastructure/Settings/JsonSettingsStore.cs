using System;
using System.IO;
using System.Text;
using System.Text.Json;
using LMHull.Core.Abstractions;

namespace LMHull.Infrastructure.Settings;

/// <summary>
/// Lưu cấu hình dạng JSON; tệp chưa có hoặc hỏng thì trả về cấu hình mặc định thay vì báo lỗi.
/// </summary>
public class JsonSettingsStore<T> : ISettingsStore<T> where T : class, new()
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public string FilePath { get; }

    public JsonSettingsStore(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Đường dẫn mặc định: %AppData%/LMHull/settings.json.
    /// </summary>
    public static JsonSettingsStore<T> ForAppData(string fileName = "settings.json") =>
        new(Path.Combine(AppDataFolder.Root, fileName));

    public T Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new T();
            return JsonSerializer.Deserialize<T>(File.ReadAllText(FilePath)) ?? new T();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new T();
        }
    }

    public void Save(T settings)
    {
        string? folder = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options), new UTF8Encoding(false));
    }
}
