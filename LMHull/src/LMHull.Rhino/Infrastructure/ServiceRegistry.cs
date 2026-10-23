using LMHull.Core.Abstractions;
using LMHull.Core.Models;
using LMHull.Infrastructure.Export;
using LMHull.Infrastructure.Logging;
using LMHull.Infrastructure.Persistence;
using LMHull.Infrastructure.Settings;

namespace LMHull.Rhino.Infrastructure;

/// <summary>
/// Composition root của plugin: nơi duy nhất chọn cài đặt cụ thể cho các interface ở Core.
/// </summary>
public static class ServiceRegistry
{
    public static IProjectRepository Projects { get; private set; } = new JsonProjectRepository();
    public static IResultExporter Exporter { get; private set; } = new FileResultExporter();
    public static ISettingsStore<LMHullSettings> SettingsStore { get; private set; } = JsonSettingsStore<LMHullSettings>.ForAppData();
    public static IAppLogger Logger { get; private set; } = new CompositeLogger(new FileLogger(), new RhinoCommandLineLogger());

    /// <summary>
    /// Cấu hình đang dùng; nạp từ SettingsStore khi plugin khởi động.
    /// </summary>
    public static LMHullSettings Settings { get; private set; } = new();

    public static void Initialize()
    {
        Settings = SettingsStore.Load();
        Logger.Info("LMHull plugin đã khởi động.");
    }

    public static void SaveSettings(LMHullSettings settings)
    {
        SettingsStore.Save(settings);
        Settings = settings;
        Logger.Info("Đã lưu cấu hình LMHull.");
    }
}
