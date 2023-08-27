using System.Text.RegularExpressions;
using System.Xml;

namespace csv_diff;

// Convert XML content to CSV format using XPath selectors to identify the rows and field values in an XML document
public class XMLSource : Source
{
    public string? Context { get; set; }

    // Constructor
    public XMLSource(string path, Dictionary<string, object> options = null) : base(options)
    {
        Path = path;
        Context = options?.GetValueOrDefault("context") as string;
        Data = new List<string[]>();
    }

    // Process a +source+, converting the XML into a table of data, using +rec_xpath+ to identify the nodes that correspond each record that should appear in the output,
    // and +field_maps+ to populate each field in each row.
    public List<string[]> Process(object source, string recXPath, Dictionary<string, string> fieldMaps, string? context = null)
    {
        FieldNames ??= fieldMaps.Keys.ToList();
        if (source is XmlDocument xmlDoc)
        {
            AddData(xmlDoc, recXPath, fieldMaps, context ?? Context);
        }
        else if (source is string xmlString && xmlString.StartsWith("<?xml"))
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlString);
            AddData(doc, recXPath, fieldMaps, context ?? Context);
        }
        else if (source is IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                ProcessFile(filePath, recXPath, fieldMaps);
            }
        }
        else if (source is string filePath)
        {
            ProcessFile(filePath, recXPath, fieldMaps);
        }
        else
        {
            throw new ArgumentException($"Unhandled source type {source.GetType().Name}");
        }

        return Data;
    }

    private void ProcessFile(string filePath, string recXPath, Dictionary<string, string> fieldMaps)
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);
            AddData(doc, recXPath, fieldMaps, Context ?? filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred while attempting to open {filePath}");
            throw;
        }
    }

    private void AddData(XmlDocument doc, string recXPath, Dictionary<string, string> fieldMaps, string context)
    {
        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        namespaceManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI);

        var recNodes = doc.SelectNodes(recXPath, namespaceManager);
        foreach (XmlNode recNode in recNodes)
        {
            var rec = new List<string>();
            foreach (var fieldMap in fieldMaps)
            {
                var expr = fieldMap.Value;
                if (VerifyRegEx(expr)) // Match context against Regexp and extract first grouping
                {
                    var regex = new Regex(expr);
                    if (!string.IsNullOrEmpty(context) && regex.IsMatch(context))
                    {
                        rec.Add(regex.Match(context).Groups[1].Value);
                    }
                    else
                    {
                        rec.Add(null);
                    }
                }
                else if (new[] { "/", "(", ".", "@" }.Any(c => expr.Contains(c))) // XPath expression
                {
                    var res = recNode.SelectSingleNode(expr, namespaceManager);
                    rec.Add(res?.InnerText);
                }
                else // Use expr as the value for this field
                {
                    rec.Add(expr);
                }
            }
            Data.Add(rec.ToArray());
        }
    }
    
    private bool VerifyRegEx(string testPattern)
    {
        bool isValid = true;

        if ((testPattern != null) && (testPattern.Trim( ).Length > 0))
        {
            try
            {
                Regex.Match("", testPattern);
            }
            catch (ArgumentException)
            {
                // BAD PATTERN: Syntax error
                isValid = false;
            }
        }
        else
        {
            //BAD PATTERN: Pattern is null or blank
            isValid = false;
        }

        return (isValid);
    }
}