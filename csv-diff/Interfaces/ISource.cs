using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace csv_diff.Interfaces;

public interface ISource
{
    
    [ExcludeFromCodeCoverage]
    public string Path { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<string[]> Data { get; internal set; }
    
    [ExcludeFromCodeCoverage]
    public List<string> FieldNames { get; private protected set; }
    
    [ExcludeFromCodeCoverage]
    public List<string> KeyFields { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<string> ParentFields { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<string> ChildFields { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<int> KeyFieldIndexes { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<int> ParentFieldIndexes { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<int> ChildFieldIndexes { get; set; }
    
    [ExcludeFromCodeCoverage]
    public bool CaseSensitive { get; set; }
    
    [ExcludeFromCodeCoverage]
    public bool TrimWhitespace { get; set; }
    
    [ExcludeFromCodeCoverage]
    public bool IgnoreHeader { get; set; }
    
    [ExcludeFromCodeCoverage]
    public Dictionary<string, Regex> Include { get; set; }
    
    [ExcludeFromCodeCoverage]
    public Dictionary<string, Regex> Exclude { get; set; }
    
    [ExcludeFromCodeCoverage]
    public List<string> Warnings { get; set; }
    
    [ExcludeFromCodeCoverage]
    public int LineCount { get; set; }
    
    [ExcludeFromCodeCoverage]
    public int SkipCount { get; set; }
    
    [ExcludeFromCodeCoverage]
    public int DupCount { get; set; }
    
    [ExcludeFromCodeCoverage]
    public SortedList<string, Dictionary<string, object>> Lines { get; set; }
    
    [ExcludeFromCodeCoverage]
    public Dictionary<string, List<string>> Index { get; set; }
    
    [ExcludeFromCodeCoverage]
    public bool PathExists => Path != "NA";
    
    [ExcludeFromCodeCoverage]
    public void IndexSource();
    
    [ExcludeFromCodeCoverage]
    public void SaveCSV(string filePath, Dictionary<string, object> options = null);
    
    [ExcludeFromCodeCoverage]
    public List<Dictionary<string, string>> ToHash();
}