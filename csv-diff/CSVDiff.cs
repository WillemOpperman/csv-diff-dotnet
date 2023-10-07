using csv_diff.Interfaces;

namespace csv_diff;

public class CSVDiff : Algorithm
{
    // CSVSource object containing details of the left/from input.
    public ISource Left { get; }
    // CSVSource object containing details of the right/to input.
    public ISource Right { get; }
    // An array of differences
    public Dictionary<string, Diff> Diffs { get; set; }

    // An array of field names that are compared in the diff process.
    public List<string> DiffFields { get; }
    // An array of field names of the key fields that uniquely identify each row.
    public List<string> KeyFields { get; }
    // An array of field names for the parent field(s).
    public List<string> ParentFields { get; }
    // An array of field names for the child field(s).
    public List<string> ChildFields { get; }
    // The options dictionary used for the diff.
    public Dictionary<string, object> Options { get; set; }

    // Constructor
    public CSVDiff(object left, object right, Dictionary<string, object> options = null)
    {
        options ??= new Dictionary<string, object>();
        Left = left is ISource leftSource ? leftSource : new CSVSource(left, options);
        if (Left.Lines == null)
        {
            Left.IndexSource();
        }
        if (Left.FieldNames == null || Left.FieldNames.Count == 0)
        {
            throw new Exception("No field names found in left (from) source");
        }

        Right = right is ISource rightSource ? rightSource : new CSVSource(right, options);
        if (Right.Lines == null)
        {
            Right.IndexSource();
        }
        if (Right.FieldNames == null || Right.FieldNames.Count == 0)
        {
            throw new Exception("No field names found in right (to) source");
        }

        Diffs = new Dictionary<string, Diff>();
        DiffFields = GetDiffFields(Left.FieldNames, Right.FieldNames, options);
        KeyFields = Left.KeyFields;
        Diff(options);
    }

    // Performs a diff with the specified options.
    public void Diff(Dictionary<string, object> options = null)
    {
        Options = options;
        Diffs = DiffSources(Left, Right, KeyFields.ToArray(), DiffFields.ToArray(), options);
    }

    // Returns a summary of the number of adds, deletes, moves, and updates.
    public Dictionary<string, int> Summary
    {
        get
        {
            var summary = new Dictionary<string, int>
            {
                { "Add", 0 },
                { "Delete", 0 },
                { "Update", 0 },
                { "Move", 0 }
            };
            foreach (var diff in Diffs)
            {
                // summary[diff.Value.DiffType] += 1;
                summary[(string)diff.Value["action"]] += 1;
            }
            summary["warning"] = Warnings.Count > 0 ? Warnings.Count : 0;
            return summary;
        }
    }

    // Returns an array of adds.
    public Dictionary<string, Diff> Adds
    {
        get
        {
            return Diffs.Where(diff => (string)diff.Value["action"] == "Add").ToDictionary(x => x.Key, x => x.Value);
        }
    }
    
    // Returns an array of deletes.
    public Dictionary<string, Diff> Deletes
    {
        get
        {
            return Diffs.Where(diff => (string)diff.Value["action"] == "Delete").ToDictionary(x => x.Key, x => x.Value);
        }
    }
    
    // Returns an array of updates.
    public Dictionary<string, Diff> Updates
    {
        get
        {
            return Diffs.Where(diff => (string)diff.Value["action"] == "Update").ToDictionary(x => x.Key, x => x.Value);
        }
    }
    
    // Returns an array of moves.
    public Dictionary<string, Diff> Moves
    {
        get
        {
            return Diffs.Where(diff => (string)diff.Value["action"] == "Move").ToDictionary(x => x.Key, x => x.Value);
        }
    }

    // Returns an array of warning messages generated from the sources and the diff process.
    public List<string> Warnings
    {
        get
        {
            var warnings = new List<string>();
            warnings.AddRange(Left.Warnings);
            warnings.AddRange(Right.Warnings);
            warnings.AddRange(_warnings);
            return warnings;
        }
    }

    // Returns an array of warning messages from the diff process.
    public List<string> DiffWarnings
    {
        get
        {
            return _warnings;
        }
    }

    private List<string> _warnings = new List<string>();

    // Given two sets of field names, determines the common set of fields present
    // in both, on which members can be diffed.
    private List<string> GetDiffFields(List<string> leftFields, List<string> rightFields, Dictionary<string, object> options)
    {
        var ignoreFields = options.ContainsKey("ignore_fields") ? options["ignore_fields"] : new List<object>();
        var ignoreFieldsList = new List<string>();
        if (ignoreFields is string ignoreFieldString)
        {
            ignoreFieldsList.Add(ignoreFieldString.ToUpper());
        }
        else if (ignoreFields is IEnumerable<object> ignoreFieldEnumerable)
        {
            ignoreFieldsList.AddRange(ignoreFieldEnumerable.Select(f => f.ToString().ToUpper()));
        }

        var diffFields = new List<string>();
        if (options.ContainsKey("diff_common_fields_only") && (bool)options["diff_common_fields_only"])
        {
            foreach (var field in rightFields)
            {
                if (leftFields.Contains(field))
                {
                    var upperCaseField = field.ToUpper();
                    if (!ignoreFieldsList.Contains(upperCaseField))
                    {
                        diffFields.Add(field);
                    }
                }
            }
        }
        else
        {
            diffFields.AddRange(rightFields);
            diffFields.AddRange(leftFields);
            diffFields = diffFields.Distinct().Where(f => !ignoreFieldsList.Contains(f.ToUpper())).ToList();
        }

        return diffFields;
    }
}