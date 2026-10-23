using System.IO;
using LMHull.Core.Models;
using LMHull.Infrastructure.Settings;
using Xunit;

namespace LMHull.Tests.Infrastructure;

public class JsonSettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var temp = new TempFolder();
        var store = new JsonSettingsStore<LMHullSettings>(temp.File("settings.json"));

        var settings = store.Load();

        Assert.Equal(1.025, settings.SeaWaterDensity);
        Assert.Equal("Steel AH36", settings.DefaultMaterial);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        using var temp = new TempFolder();
        string path = temp.File("settings.json");
        File.WriteAllText(path, "### corrupt ###");

        Assert.Equal(30, new JsonSettingsStore<LMHullSettings>(path).Load().MaxResultHistory);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        using var temp = new TempFolder();
        var store = new JsonSettingsStore<LMHullSettings>(temp.File("sub/settings.json"));

        store.Save(new LMHullSettings { SeaWaterDensity = 1.0, DefaultMaterial = "Aluminium 5083", MaxResultHistory = 5 });
        var loaded = store.Load();

        Assert.Equal(1.0, loaded.SeaWaterDensity);
        Assert.Equal("Aluminium 5083", loaded.DefaultMaterial);
        Assert.Equal(5, loaded.MaxResultHistory);
    }
}
