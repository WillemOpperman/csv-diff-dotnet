using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace csv_diff;

// Represents an input (i.e the left/from or right/to input) to the diff process.
public class Source
{
    public string Path { get; set; }
    public List<string[]> Data { get; internal set; }
    public List<string> FieldNames { get; private protected set; }
    public List<string> KeyFields { get; private set; }
    public List<string> ParentFields { get; private set; }
    public List<string> ChildFields { get; private set; }
    public List<int> KeyFieldIndexes { get; private set; }
    public List<int> ParentFieldIndexes { get; private set; }
    public List<int> ChildFieldIndexes { get; private set; }
    public bool CaseSensitive { get; private set; }
    public bool TrimWhitespace { get; private set; }
    public bool IgnoreHeader { get; private set; }
    public Dictionary<string, Regex> Include { get; private set; }
    public Dictionary<string, Regex> Exclude { get; private set; }
    public List<string> Warnings { get; private set; }
    public int LineCount { get; private set; }
    public int SkipCount { get; private set; }
    public int DupCount { get; private set; }
    public Dictionary<string, Dictionary<string, object>> Lines { get; private set; }
    public Dictionary<string, List<string>> Index { get; private set; }

    public Source(Dictionary<string, object> options = null)
    {
        if (options == null)
        {
            options = new Dictionary<string, object>();
        }

        if ((!options.ContainsKey("parent_field") && !options.ContainsKey("parent_fields") &&
            !options.ContainsKey("child_field") && !options.ContainsKey("child_fields")) &&
            (options.ContainsKey("key_field") || options.ContainsKey("key_fields")))
        {
            var keyFields = options.ContainsKey("key_field") ?
                new List<string> { options["key_field"].ToString() } :
                ((IList)options["key_fields"]).Cast<object>().ToList().Select(kf => kf.ToString()).ToList();
            ParentFields = new List<string>();
            ChildFields = keyFields;
            KeyFields = keyFields;
        }
        else
        {
            ParentFields = options.ContainsKey("parent_field") ?
                new List<string> { options["parent_field"].ToString() } :
                ((List<object>)options["parent_fields"]).Select(pf => pf.ToString()).ToList();

            ChildFields = options.ContainsKey("child_field") ?
                new List<string> { options["child_field"].ToString() } :
                ((List<object>)options["child_fields"]).Select(cf => cf.ToString()).ToList();

            KeyFields = ParentFields.Concat(ChildFields).ToList();
        }

        if (options.ContainsKey("field_names"))
        {
            FieldNames = ((List<object>)options["field_names"]).Select(fn => fn.ToString()).ToList();
        }
        CaseSensitive = options.ContainsKey("case_sensitive") ? (bool)options["case_sensitive"] : true;
        TrimWhitespace = options.ContainsKey("trim_whitespace") ? (bool)options["trim_whitespace"] : false;
        IgnoreHeader = options.ContainsKey("ignore_header") ? (bool)options["ignore_header"] : false;

        if (options.ContainsKey("include"))
        {
            Include = (Dictionary<string, Regex>)options["include"];
            // Include = ConvertFilter((Dictionary<string, Regex>)options["include"], FieldNames);
        }

        if (options.ContainsKey("exclude"))
        {
            Exclude = (Dictionary<string, Regex>)options["exclude"];
            // Exclude = ConvertFilter((Dictionary<string, Regex>)options["exclude"], FieldNames);
        }

        Path = options.ContainsKey("path") ? options["path"].ToString() : "NA";
        Warnings = new List<string>();
    }

    public bool PathExists => Path != "NA";

    public Dictionary<string, object> this[string key]
    {
        get
        {
            if (Lines.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }
    }

    public void IndexSource()
    {
        Lines = new Dictionary<string, Dictionary<string, object>>();
        Index = new Dictionary<string, List<string>>();
        if (FieldNames != null)
        {
            IndexFields();
        }
        var includeFilter = ConvertFilter(Include, FieldNames);
        var excludeFilter = ConvertFilter(Exclude, FieldNames);
        LineCount = 0;
        SkipCount = 0;
        DupCount = 0;
        var lineNum = 0;
        foreach (var row in Data)
        {
            lineNum++;
            if (lineNum == 1 && FieldNames != null && IgnoreHeader)
            {
                continue;
            }

            if (FieldNames == null)
            {
                FieldNames = row.Select((_, i) => _.ToString() ?? i.ToString()).ToList();
                IndexFields();
                includeFilter = ConvertFilter(Include, FieldNames);
                excludeFilter = ConvertFilter(Exclude, FieldNames);
                continue;
            }

            var line = new Dictionary<string, object>();
            var filter = false;

            for (var i = 0; i < FieldNames.Count; i++)
            {
                var field = FieldNames[i];
                var val = row[i]?.ToString();
                if (TrimWhitespace && val != null)
                {
                    val = val.Trim();
                }
                line[field] = val;
                if (includeFilter != null && includeFilter.TryGetValue(field, out var include))
                {
                    filter = !CheckFilter(include, line[field]);
                }

                if (excludeFilter != null && excludeFilter.TryGetValue(field, out var exclude))
                {
                    filter = CheckFilter(exclude, line[field]);
                }

                if (filter)
                {
                    SkipCount++;
                    break;
                }
            }

            if (filter)
            {
                continue;
            }

            var keyValues = KeyFieldIndexes.Select(kf => (CaseSensitive ? line[FieldNames[kf]] : line[FieldNames[kf]].ToString().ToUpper())).ToList();
            var key = string.Join("~", keyValues);
            var parentKey = string.Join("~", keyValues.Take(ParentFields.Count));
            if (Lines.ContainsKey(key))
            {
                Warnings.Add($"Duplicate key '{key}' encountered at line {lineNum}");
                DupCount++;
                key += $"[{DupCount}]";
            }

            if (!Index.ContainsKey(parentKey))
            {
                Index[parentKey] = new List<string>();
            }

            Index[parentKey].Add(key);
            Lines[key] = line;
            LineCount++;
        }
    }

    public void SaveCSV(string filePath, Dictionary<string, object> options = null)
    {
        using (var writer = new System.IO.StreamWriter(filePath))
        {
            var defaultOpts = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            var csv = new CsvHelper.CsvWriter(writer, defaultOpts);
            foreach (var row in Data)
            {
                csv.WriteRecords(new[] { row });
            }
        }
    }

    public List<Dictionary<string, string>> ToHash()
    {
        return Data.Select(row =>
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < FieldNames.Count; i++)
            {
                dict[FieldNames[i]] = row[i];
            }
            return dict;
        }).ToList();
    }

    private void IndexFields()
    {
        KeyFieldIndexes = FindFieldIndexes(KeyFields, FieldNames);
        ParentFieldIndexes = FindFieldIndexes(ParentFields, FieldNames);
        ChildFieldIndexes = FindFieldIndexes(ChildFields, FieldNames);
        KeyFields = KeyFieldIndexes.Select(kf => FieldNames[kf]).ToList();
        ParentFields = ParentFieldIndexes.Select(pf => FieldNames[pf]).ToList();
        ChildFields = ChildFieldIndexes.Select(cf => FieldNames[cf]).ToList();
    }

    private List<int> FindFieldIndexes(List<string> keyFields, List<string> fieldNames)
    {
        return keyFields.Select(field =>
        {
            if (int.TryParse(field, out var fieldIndex))
            {
                return fieldIndex;
            }

            var fieldIndexIgnoreCase = fieldNames.FindIndex(fn =>
                fn.Equals(field, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

            if (fieldIndexIgnoreCase == -1)
            {
                throw new ArgumentException($"Could not locate field '{field}' in source field names: {string.Join(", ", fieldNames)}");
            }

            return fieldIndexIgnoreCase;
        }).ToList();
    }

    private Dictionary<string, Regex> ConvertFilter(Dictionary<string, Regex> hsh, List<string> fieldNames)
    {
        if (hsh is null || fieldNames is null)
        {
            return null;
        }

        var filter = new Dictionary<string, Regex>();
        foreach (var kvp in hsh)
        {
            var key = kvp.Key.ToString();
            var index = int.TryParse(key, out var fieldIndex) ? fieldIndex : fieldNames.IndexOf(key);

            if (index == -1)
            {
                throw new ArgumentException($"Field '{key}' specified in filter not found in field names: {string.Join(", ", fieldNames)}");
            }

            filter[fieldNames[index]] = new Regex(kvp.Value.ToString());
        }

        return filter;
    }

    // Checks whether the given filter matches the field value.
    private bool CheckFilter(object filter, object fieldValue)
    {
        if (filter is string s)
        {
            return CaseSensitive ? s == fieldValue : s.Equals((string)fieldValue, StringComparison.OrdinalIgnoreCase);
        }
        
        if (filter is Regex regex)
        {
            return regex.IsMatch((string)fieldValue);
        }
        
        if (filter is Func<string, bool> func)
        {
            return func((string)fieldValue);
        }
        
        throw new ArgumentException($"Unsupported filter expression: {filter}");
    }
}