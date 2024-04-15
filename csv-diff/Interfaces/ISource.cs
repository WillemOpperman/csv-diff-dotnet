using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace csv_diff.Interfaces
{
    public interface ISource
    {
        
        [ExcludeFromCodeCoverage]
        string Path { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string[]> Data { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string> FieldNames { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string> KeyFields { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string> ParentFields { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string> ChildFields { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<int> KeyFieldIndexes { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<int> ParentFieldIndexes { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<int> ChildFieldIndexes { get; set; }
        
        [ExcludeFromCodeCoverage]
        bool CaseSensitive { get; set; }
        
        [ExcludeFromCodeCoverage]
        bool TrimWhitespace { get; set; }
        
        [ExcludeFromCodeCoverage]
        bool IgnoreHeader { get; set; }
        
        [ExcludeFromCodeCoverage]
        Dictionary<string, Regex> Include { get; set; }
        
        [ExcludeFromCodeCoverage]
        Dictionary<string, Regex> Exclude { get; set; }
        
        [ExcludeFromCodeCoverage]
        List<string> Warnings { get; set; }
        
        [ExcludeFromCodeCoverage]
        int LineCount { get; set; }
        
        [ExcludeFromCodeCoverage]
        int SkipCount { get; set; }
        
        [ExcludeFromCodeCoverage]
        int DupCount { get; set; }
        
        [ExcludeFromCodeCoverage]
        SortedList<string, Dictionary<string, object>> Lines { get; set; }
        
        [ExcludeFromCodeCoverage]
        Dictionary<string, Dictionary<string, int>> Index { get; set; }
        
        [ExcludeFromCodeCoverage]
        bool PathExists();
        
        [ExcludeFromCodeCoverage]
        void IndexSource();
        
        [ExcludeFromCodeCoverage]
        void SaveCSV(string filePath, Dictionary<string, object> options = null);
        
        [ExcludeFromCodeCoverage]
        List<Dictionary<string, string>> ToHash();
    }
}
