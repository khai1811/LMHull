using System;
using System.IO;

namespace LMHull.Infrastructure.Settings;

/// <summary>
/// Thư mục dữ liệu người dùng của LMHull: %AppData%/LMHull.
/// </summary>
public static class AppDataFolder
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LMHull");

    public static string Logs => Path.Combine(Root, "logs");
}
