using System.Collections.Generic;
using System.Diagnostics;

namespace csv_diff
{
    // Holds the details of a single difference

    [DebuggerDisplay("{DiffType} {Row}")]
    public class Diff
    {
        public string DiffType { get; set; }
        public Dictionary<string, object> Fields { get; }
        public int Row { get; }
        public object SiblingPosition { get; set; }

        public Diff(string diffType, Dictionary<string, object> fields, int rowIdx, object posIdx)
        {
            DiffType = diffType;
            Fields = fields;
            Row = rowIdx + 1;
            SetSiblingPosition(posIdx);
        }

        private void SetSiblingPosition(object posIdx)
        {
            if (posIdx is List<int> posList)
            {
                posList.RemoveAll(item => item == 0);
                if (posList.Count > 1)
                {
                    SiblingPosition = posList.ConvertAll(pos => pos + 1);
                }
                else
                {
                    SiblingPosition = posList.Count > 0 ? posList[0] + 1 : -1;
                }
            }
            else if (posIdx is int pos)
            {
                SiblingPosition = pos + 1;
            }
        }

        // For backwards compatibility and access to fields with differences
        public object this[string key]
        {
            get
            {
                switch (key)
                {
                    case "Action":
                    case "action":
                        string a = DiffType;
                        if (!string.IsNullOrEmpty(a))
                        {
                            a = char.ToUpper(a[0]) + a.Substring(1);
                        }
                        return a;
                    case "Row":
                    case "row":
                        return Row;
                    case "SiblingPosition":
                    case "sibling_position":
                        return SiblingPosition;
                    default:
                        return Fields.TryGetValue(key, out object value) ? value : null;
                }
            }
        }
    }
}
