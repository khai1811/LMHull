using System;
using System.IO;
using System.Text;
using LMHull.Core.Abstractions;
using LMHull.Infrastructure.Settings;

namespace LMHull.Infrastructure.Logging;

/// <summary>
/// Ghi log theo ngày: {folder}/lmhull-yyyyMMdd.log. Lỗi ghi log không bao giờ làm hỏng lệnh đang chạy.
/// </summary>
public class FileLogger : IAppLogger
{
    private readonly object _lock = new();
    private readonly string _folder;

    public FileLogger(string? folder = null)
    {
        _folder = folder ?? AppDataFolder.Logs;
    }

    public string CurrentFile => Path.Combine(_folder, $"lmhull-{DateTime.Now:yyyyMMdd}.log");

    public void Info(string message) => Write("INFO", message, null);

    public void Warn(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" [").Append(level).Append("] ")
            .Append(message);
        if (exception != null) line.AppendLine().Append(exception);

        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_folder);
                File.AppendAllText(CurrentFile, line.AppendLine().ToString(), Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Bỏ qua: không để lỗi ghi log làm gián đoạn thao tác của người dùng
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
