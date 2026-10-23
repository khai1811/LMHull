using System;
using System.IO;
using System.Linq;
using LMHull.Core.Enums;
using LMHull.Core.Models;
using LMHull.Core.Reporting;
using LMHull.Core.Services;
using LMHull.Infrastructure.Persistence;
using Xunit;

namespace LMHull.Tests.Infrastructure;

public class JsonProjectRepositoryTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsTreeAndResults()
    {
        using var temp = new TempFolder();
        var repo = new JsonProjectRepository();
        var root = ExpressMarineProjectBuilder.CreateExpressMarineTemplate("ShipA");
        var leaf = root.Leaves().First(n => n.NodeType == StructuralNodeType.DeckPlate);
        leaf.ThicknessMm = 14.5;
        leaf.Material = "Aluminium 5083";
        leaf.LinkGeometry(Guid.NewGuid(), 1.25);

        var data = new ProjectData(root);
        data.Results.Add(ResultTable.KeyValue("Thủy tĩnh", "LMHydro").AddRow("V", 12.5, "F1", "m³"));
        string path = temp.File("ship" + JsonProjectRepository.FileExtension);

        repo.Save(data, path);
        var loaded = repo.Load(path);

        Assert.True(repo.Exists(path));
        Assert.Equal(root.Id, loaded.Root.Id);
        Assert.Equal(root.Descendants().Select(n => (n.Id, n.Parent!.Id, n.Name, n.NodeType)),
                     loaded.Root.Descendants().Select(n => (n.Id, n.Parent!.Id, n.Name, n.NodeType)));

        var loadedLeaf = loaded.Root.FindById(leaf.Id)!;
        Assert.Equal(14.5, loadedLeaf.ThicknessMm);
        Assert.Equal("Aluminium 5083", loadedLeaf.Material);
        Assert.Equal(leaf.RhinoGeometryId, loadedLeaf.RhinoGeometryId);
        Assert.Equal(1.25, loadedLeaf.WeightTonnes);

        var table = Assert.Single(loaded.Results);
        Assert.Equal("LMHydro", table.Source);
        Assert.Equal(new[] { "V", "12.5", "m³" }, table.Rows[0]);
    }

    [Fact]
    public void Load_UnknownSchemaVersion_ThrowsInvalidData()
    {
        using var temp = new TempFolder();
        string path = temp.File("bad.lmhull.json");
        File.WriteAllText(path, "{\"SchemaVersion\": 99, \"Nodes\": []}");

        var ex = Assert.Throws<InvalidDataException>(() => new JsonProjectRepository().Load(path));
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void Load_MalformedJson_ThrowsInvalidData()
    {
        using var temp = new TempFolder();
        string path = temp.File("broken.lmhull.json");
        File.WriteAllText(path, "{ not json");

        Assert.Throws<InvalidDataException>(() => new JsonProjectRepository().Load(path));
    }
}
