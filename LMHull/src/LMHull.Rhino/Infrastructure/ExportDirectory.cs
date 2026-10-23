using System;
using System.IO;

namespace LMHull.Rhino.Infrastructure;

/// <summary>
/// Thư mục mở sẵn trong hộp thoại lưu/xuất tệp: thư mục xuất trong cấu hình, nếu không có thì Documents.
/// </summary>
public static class ExportDirectory
{
    public static Uri Default
    {
        get
        {
            string folder = ServiceRegistry.Settings.ExportFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            return new Uri(folder);
        }
    }
}
