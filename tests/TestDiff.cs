using System.Collections.Generic;
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

        var diff = new CSVDiff(source1, source2, new Dictionary<string, object>
        {
            { "parent_field", 0 },
            { "child_field", 1 }
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

        var diff = new CSVDiff(source1, source2, new Dictionary<string, object>
        {
            { "parent_field", 0 },
            { "child_field", 1 }
        });

        Assert.Equal(1, source1.SkipCount);
        Assert.Equal(1, source2.SkipCount);
    }
}