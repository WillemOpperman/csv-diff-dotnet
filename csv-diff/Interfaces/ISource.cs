using System.Text.RegularExpressions;

namespace csv_diff.Interfaces;

public interface ISource
{
    public string Path { get; set; }
    public List<string[]> Data { get; internal set; }
    public List<string> FieldNames { get; private protected set; }
    public List<string> KeyFields { get; set; }
    public List<string> ParentFields { get; set; }
    public List<string> ChildFields { get; set; }
    public List<int> KeyFieldIndexes { get; set; }
    public List<int> ParentFieldIndexes { get; set; }
    public List<int> ChildFieldIndexes { get; set; }
    public bool CaseSensitive { get; set; }
    public bool TrimWhitespace { get; set; }
    public bool IgnoreHeader { get; set; }
    public Dictionary<string, Regex> Include { get; set; }
    public Dictionary<string, Regex> Exclude { get; set; }
    public List<string> Warnings { get; set; }
    public int LineCount { get; set; }
    public int SkipCount { get; set; }
    public int DupCount { get; set; }
    public Dictionary<string, Dictionary<string, object>> Lines { get; set; }
    public Dictionary<string, List<string>> Index { get; set; }
    public bool PathExists => Path != "NA";
    public void IndexSource();
    public void SaveCSV(string filePath, Dictionary<string, object> options = null);
    public List<Dictionary<string, string>> ToHash();
}