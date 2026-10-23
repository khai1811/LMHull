using System;
using LMHull.Core.Reporting;
using Xunit;

namespace LMHull.Tests.Reporting;

public class ResultTableTests
{
    [Fact]
    public void KeyValue_HasThreeColumns()
    {
        var table = ResultTable.KeyValue("T", "Src");

        Assert.Equal(new[] { "Hạng mục", "Giá trị", "Đơn vị" }, table.Columns);
        Assert.Equal("Src", table.Source);
    }

    [Fact]
    public void AddRow_WrongCellCount_Throws()
    {
        var table = new ResultTable("T", "Src", "A", "B");

        Assert.Throws<ArgumentException>(() => table.AddRow("only one"));
        Assert.Throws<ArgumentException>(() => table.AddRow("1", "2", "3"));
    }

    [Fact]
    public void AddRow_NumericOverload_FormatsInvariant()
    {
        var table = ResultTable.KeyValue("T", "Src").AddRow("Thể tích", 1234.5678, "F2", "m³");

        Assert.Equal(new[] { "Thể tích", "1234.57", "m³" }, table.Rows[0]);
    }

    [Fact]
    public void ToCsv_QuotesSpecialCharacters()
    {
        var table = new ResultTable("T", "Src", "Name", "Note")
            .AddRow("a,b", "say \"hi\"")
            .AddRow("line1\nline2", "plain");

        string csv = table.ToCsv().Replace("\r\n", "\n");

        Assert.Equal("Name,Note\n\"a,b\",\"say \"\"hi\"\"\"\n\"line1\nline2\",plain\n", csv);
    }

    [Fact]
    public void ToTsv_UsesTabsAndFlattensNewlines()
    {
        var table = new ResultTable("T", "Src", "A", "B").AddRow("x\ty", "1\n2");

        string tsv = table.ToTsv().Replace("\r\n", "\n");

        Assert.Equal("A\tB\nx y\t1 2\n", tsv);
    }

    [Fact]
    public void Constructor_WithoutColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ResultTable("T", "Src"));
    }
}
