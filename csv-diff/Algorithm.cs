using csv_diff.Interfaces;

namespace csv_diff;

// Implements the CSV diff algorithm.
public class Algorithm
{
    // Diffs two CSVSource structures.
    public Dictionary<string, Diff> DiffSources(
        ISource left,
        ISource right,
        string[] keyFields,
        string[] diffFields,
        IDictionary<string, object> options = null)
    {
        if (left.CaseSensitive != right.CaseSensitive)
        {
            throw new ArgumentException("Left and right must have the same settings for case-sensitivity");
        }

        if (left.ParentFields.Count != right.ParentFields.Count)
        {
            throw new ArgumentException("Left and right must have the same settings for parent/child fields");
        }

        // Ensure key fields are not also in the diff_fields
        var diffFieldSet = new HashSet<string>(diffFields);
        var diffFieldsNoKeys = diffFieldSet.Except(keyFields).ToArray();

        var leftIndex = left.Index;
        var leftValues = left.Lines;
        var leftKeys = leftValues.Keys;
        var rightIndex = right.Index;
        var rightValues = right.Lines;
        var rightKeys = rightValues.Keys;
        var parentFieldCount = left.ParentFields.Count;

        var includeAdds = options.TryGetValue("ignore_adds", out var ignoreAdds) ? !(bool)ignoreAdds : true;
        var includeMoves = options.TryGetValue("ignore_moves", out var ignoreMoves) ? !(bool)ignoreMoves : true;
        var includeUpdates = options.TryGetValue("ignore_updates", out var ignoreUpdated) ? !(bool)ignoreUpdated : true;
        var includeDeletes = options.TryGetValue("ignore_deletes", out var ignoreDeletes) ? !(bool)ignoreDeletes : true;

        var caseSensitive = left.CaseSensitive;
        var equalityProcs = options?.ContainsKey("equality_procs") == true
            ? (IDictionary<string, Func<object, object, bool>>)options["equality_procs"]
            : new Dictionary<string, Func<object, object, bool>>();

        var diffs = new Dictionary<string, Diff>();
        var potentialMoves = new Dictionary<string, List<string>>();

        // First identify deletions
        if (includeDeletes)
        {
            foreach (var key in leftKeys.Except(rightKeys))
            {
                // Delete
                var keyVals = key.Split('~');
                var parent = string.Join("~", keyVals.Take(parentFieldCount));
                var child = string.Join("~", keyVals.Skip(parentFieldCount));
                var leftParent = leftIndex[parent];
                var leftValue = leftValues[key];
                
                var rowIdx = leftKeys.IndexOf(key);
                var sibIdx = leftParent.IndexOf(key);
                if (sibIdx < 0)
                {
                    throw new Exception($"Can't locate key {key} in parent {parent}");
                }

                diffs[key] = new Diff("delete", leftValue, rowIdx, sibIdx);
                if (!potentialMoves.ContainsKey(child))
                {
                    potentialMoves[child] = new List<string>();
                }

                potentialMoves[child].Add(key);
            }
        }

        // Now identify adds/updates
        foreach (var key in rightKeys)
        {
            var keyVals = key.Split('~');
            var parent = string.Join("~", keyVals.Take(parentFieldCount));
            var leftParent = leftIndex.ContainsKey(parent) ? leftIndex[parent] : null;
            var rightParent = rightIndex[parent];
            var leftValue = leftValues.ContainsKey(key) ? leftValues[key] : null;
            var rightValue = rightValues[key];
            var leftIdx = leftParent?.IndexOf(key) ?? -1;
            var rightIdx = rightParent.IndexOf(key);

            if (leftIdx >= 0 && rightIdx >= 0)
            {
                if (includeUpdates && diffFieldsNoKeys.Length > 0)
                {
                    var changes = DiffRow(leftValue, rightValue, diffFieldsNoKeys, caseSensitive, equalityProcs);
                    if (changes.Count > 0)
                    {
                        var id = IdFields(keyFields, rightValue);
                        
                        diffs[key] = new Diff("update", id.Union(changes).ToDictionary(x => x.Key, x => x.Value), rightIdx, rightIdx);
                    }
                }

                if (includeMoves)
                {
                    var leftCommon = leftParent.Intersect(rightParent).ToList();
                    var rightCommon = rightParent.Intersect(leftParent).ToList();
                    var leftPos = leftCommon.IndexOf(key);
                    var rightPos = rightCommon.IndexOf(key);
                    if (leftPos != rightPos)
                    {
                        // Move
                        if (diffs.TryGetValue(key, out var d))
                        {
                            d.SiblingPosition = new List<int> { leftIdx, rightIdx };
                        }
                        else
                        {
                            var id = IdFields(keyFields, rightValue);
                            diffs[key] = new Diff("move", id, rightIdx, new List<int> { leftIdx, rightIdx });
                        }
                    }
                }
            }
            else if (rightIdx >= 0)
            {
                // Add
                var child = string.Join("~", keyVals.Skip(parentFieldCount));
                if (potentialMoves.TryGetValue(child, out var potentialMovesList) && potentialMovesList.Count > 0)
                {
                    var oldKey = potentialMovesList[^1];
                    potentialMovesList.RemoveAt(potentialMovesList.Count - 1);
                    diffs.Remove(oldKey);
                    if (includeUpdates && diffFieldsNoKeys.Length > 0)
                    {
                        leftValue = leftValues[oldKey];
                        var id = IdFields(right.ChildFields.ToArray(), rightValue);
                        var changes = DiffRow(leftValue, rightValue, left.ParentFields.Concat(diffFieldsNoKeys).ToArray(), caseSensitive, equalityProcs);
                        
                        diffs[key] = new Diff("update", id.Union(changes).ToDictionary(x => x.Key, x => x.Value), rightIdx, rightIdx);
                    }
                }
                else if (includeAdds)
                {
                    diffs[key] = new Diff("add", rightValue, rightIdx, rightIdx);
                }
            }
        }

        return diffs;
    }

    // Identifies the fields that are different between two versions of the
    // same row.
    private IDictionary<string, object> DiffRow(
        IDictionary<string, object> leftRow,
        IDictionary<string, object> rightRow,
        string[] fields,
        bool caseSensitive,
        IDictionary<string, Func<object, object, bool>> equalityProcs)
    {
        var diffs = new Dictionary<string, object>();
        foreach (var attr in fields)
        {
            var eqProc = equalityProcs.ContainsKey(attr) ? equalityProcs[attr] : null;
            rightRow.TryGetValue(attr, out var rightVal);
            leftRow.TryGetValue(attr, out var leftVal);

            if (eqProc != null)
            {
                if (!eqProc(leftVal, rightVal))
                {
                    diffs[attr] = new object[] { leftVal, rightVal };
                }
            }
            else
            {
                if (caseSensitive)
                {
                    if (!string.Equals(leftVal?.ToString(), rightVal?.ToString(), StringComparison.Ordinal))
                    {
                        diffs[attr] = new object[] { leftVal, rightVal };
                    }
                }
                else
                {
                    if (!string.Equals(leftVal?.ToString(), rightVal?.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        diffs[attr] = new object[] { leftVal, rightVal };
                    }
                }
            }
        }

        return diffs;
    }

    // Return a dictionary containing just the key field values
    private Dictionary<string, object> IdFields(string[] keyFields, IDictionary<string, object> fields)
    {
        var id = new Dictionary<string, object>();
        foreach (var field in keyFields)
        {
            if (fields.TryGetValue(field, out var value))
            {
                id[field] = value;
            }
        }

        return id;
    }
}