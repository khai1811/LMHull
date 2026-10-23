using System.IO;
using LMHull.Core.Abstractions;
using LMHull.Core.Models;
using LMHull.Core.Reporting;
using LMHull.Core.Services;
using LMHull.Infrastructure.Export;
using Xunit;

namespace LMHull.Tests.Infrastructure;

public class FileResultExporterTests
{
    [Fact]
    public void ExportCsv_WritesUtf8BomAndTableContent()
    {
        using var temp = new TempFolder();
        string path = temp.File("out/result.csv");
        var table = ResultTable.KeyValue("Thủy tĩnh", "LMHydro").AddRow("Lượng chiếm nước", 820.0, "F2", "t");

        new FileResultExporter().ExportCsv(table, path);

        byte[] bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal(table.ToCsv(), File.ReadAllText(path));
    }

    [Fact]
    public void ExportShip_Csv_MatchesBuildCsv()
    {
        using var temp = new TempFolder();
        string path = temp.File("ship.csv");
        var input = new ShipInput("Test", 40, 10, 2, 3);
        var result = new ShipCalculator().Calculate(input);

        new FileResultExporter().ExportShip(input, result, path, ExportFormat.Csv);

        Assert.Equal(ShipExportService.BuildCsv(input, result), File.ReadAllText(path));
        Assert.Contains("ChieuDai_L,40.00,m", File.ReadAllText(path));
    }
}
