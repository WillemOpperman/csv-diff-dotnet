using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using csv_diff;
using Xunit;

namespace tests;

public class TestDiff
{
    private static readonly List<string[]> Data1 = new()
    {
        new[] { "Parent", "Child", "Description" },
        new[] { "A", "A1", "Account1" },
        new[] { "A", "A2", "Account 2" },
        new[] { "A", "A3", "Account 3" },
        new[] { "A", "A4", "Account 4" },
        new[] { "A", "A6", "Account 6" }
    };

    private static readonly List<string[]> Data2 = new()
    {
        new[] { "Parent", "Child", "Description" },
        new[] { "A", "A1", "Account1" },
        new[] { "A", "A2", "Account2" },
        new[] { "A", "a3", "ACCOUNT 3" },
        new[] { "A", "A5", "Account 5" },
        new[] { "B", "A6", "Account 6" },
        new[] { "C", "A6", "Account 6c" }
    };

    [Fact]
    public void TestArrayDiff()
    {
        var diff = new CSVDiff(Data1, Data2, new Dictionary<string, object>
        {
            { "parent_field", 0 },
            { "child_field", 1 }
        });

        Assert.Equal(new[] {"Parent"}, diff.Left.ParentFields);
        Assert.Equal(new[] {"Parent"}, diff.Right.ParentFields);
        Assert.Equal(new[] {"Child"}, diff.Left.ChildFields);
        Assert.Equal(new[] {"Child"}, diff.Right.ChildFields);

        Assert.Equal(3, diff.Adds.Count);
        Assert.Equal(2, diff.Deletes.Count);
        Assert.Equal(2, diff.Updates.Count);
    }

    [Fact]
    public void TestCsvDiff()
    {
        var data1Path = Path.Combine(Path.GetDirectoryName(typeof(TestDiff).Assembly.Location)!, "files", "data1.csv");
        var data2Path = Path.Combine(Path.GetDirectoryName(typeof(TestDiff).Assembly.Location)!, "files", "data2.csv");
        var diff = new CSVDiff(data1Path, data2Path, new Dictionary<string, object>
        {
            { "parent_field", 0 },
            { "child_field", 1 }
        });

        Assert.Equal(3, diff.Adds.Count);
        Assert.Equal(2, diff.Deletes.Count);
        Assert.Equal(2, diff.Updates.Count);
    }

    [Fact]
    public void TestXmlDiff()
    {
        var data1Path = Path.Combine(Path.GetDirectoryName(typeof(TestDiff).Assembly.Location)!, "files", "data1.xls");
        var data2Path = Path.Combine(Path.GetDirectoryName(typeof(TestDiff).Assembly.Location)!, "files", "data2.xls");

        var xmlSourceOptions = new Dictionary<string, object>
        {
            {"parent_field", 0},
            {"child_field", 1}
        };
        
        var leftXmlSource = new XMLSource(data1Path, xmlSourceOptions);
        leftXmlSource.Process(data1Path, "//Workbook/Worksheet/Table/Row", new Dictionary<string, string>
        {
            { "Parent", "Cell[1]/Data/text()" },
            { "Child", "Cell[2]/Data/text()"  },
            { "Description", "Cell[3]/Data/text()"  }
        });
        
        var rightXmlSource = new XMLSource(data2Path, xmlSourceOptions);
        rightXmlSource.Process(data2Path, "//Workbook/Worksheet/Table/Row", new Dictionary<string, string>
        {
            { "Parent", "Cell[1]/Data/text()" },
            { "Child", "Cell[2]/Data/text()"  },
            { "Description", "Cell[3]/Data/text()"  }
        });
        
        var diff = new CSVDiff(leftXmlSource, rightXmlSource, new Dictionary<string, object>
        {
            { "ignore_moves", true },
            { "parent_field", 0 },
            { "child_field", 1 }
        });

        Assert.Equal(3, diff.Adds.Count);
        Assert.Equal(2, diff.Deletes.Count);
        Assert.Equal(2, diff.Updates.Count);
    }

    [Fact]
    public void TestCaseInsensitiveDiff()
    {
        var diff = new CSVDiff(Data1, Data2, new Dictionary<string, object>
        {
            { "parent_field", 0 },
            { "child_field", 1 },
            { "case_sensitive", false }
        });

        Assert.Equal(2, diff.Adds.Count);
        Assert.Equal(1, diff.Deletes.Count);
        Assert.Equal(2, diff.Updates.Count);
    }

    [Fact]
    public void TestIncludeFilter()
    {
        CSVSource source1 = new CSVSource(Data1, new Dictionary<string, object>
        {
            { "key_fields", new List<int> { 0, 1 } },
            { "include", new Dictionary<string, Regex> { { "Description", new Regex("Account") } } }
        });

        CSVSource source2 = new CSVSource(Data2, new Dictionary<string, object>
        {
            { "key_fields", new List<int> { 0, 1 } },
            { "include", new Dictionary<string, Regex> { { "Description", new Regex("Account") } } }
        });

        Assert.Equal(0, source1.SkipCount);
        Assert.Equal(1, source2.SkipCount);
    }

    [Fact]
    public void TestExcludeFilter()
    {
        CSVSource source1 = new CSVSource(Data1, new Dictionary<string, object>
        {
            { "key_fields", new List<int> { 0, 1 } },
            { "exclude", new Dictionary<string, Regex> { { "Description", new Regex("Account\\d") } } }
        });

        CSVSource source2 = new CSVSource(Data2, new Dictionary<string, object>
        {
            { "key_fields", new List<int> { 0, 1 } },
            { "exclude", new Dictionary<string, Regex> { { "2", new Regex("^ACC") } } }
        });

        Assert.Equal(1, source1.SkipCount);
        Assert.Equal(1, source2.SkipCount);
    }
}