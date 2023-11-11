using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace csv_diff
{
    // Convert XML content to CSV format using XPath selectors to identify the rows and field values in an XML document
    public class XMLSource : Source
    {
        public string Context { get; set; }

        // Constructor
        public XMLSource(string path, Dictionary<string, object> options = null) : base(options)
        {
            Path = path;

            if (!ReferenceEquals(options, null))
            {
                options.TryGetValue("context", out var tempContext);
                Context = tempContext as string;
            }
            Data = new List<string[]>();
        }

        // Process a +source+, converting the XML into a table of data, using +rowsXPath+ to identify the nodes that correspond each record that should appear in the output,
        // and +fieldMaps+ to populate each field in each row.
        public void Process(object source, string rowsXPath, Dictionary<string, string> fieldMaps, string context = null)
        {
            if (FieldNames is null)
                FieldNames = fieldMaps.Keys.ToList();

            if (source is XmlDocument xmlDoc)
            {
                AddData(xmlDoc, rowsXPath, fieldMaps, context ?? Context);
            }
            else if (source is string xmlString && xmlString.StartsWith("<?xml"))
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlString);
                AddData(doc, rowsXPath, fieldMaps, context ?? Context);
            }
            else if (source is IEnumerable<string> filePaths)
            {
                foreach (var filePath in filePaths)
                {
                    ProcessFile(filePath, rowsXPath, fieldMaps);
                }
            }
            else if (source is string filePath)
            {
                ProcessFile(filePath, rowsXPath, fieldMaps);
            }
            else
            {
                throw new ArgumentException($"Unhandled source type {source.GetType().Name}");
            }
        }

        private void ProcessFile(string filePath, string rowsXPath, Dictionary<string, string> fieldMaps)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(filePath);
                AddData(doc, rowsXPath, fieldMaps, Context ?? filePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred while attempting to open {filePath}");
                throw;
            }
        }

        private void AddData(XmlDocument doc, string rowsXPath, Dictionary<string, string> fieldMaps, string context)
        {
            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI);
            
            var rowNodes = doc.SelectNodes(rowsXPath, namespaceManager);
            foreach (XmlElement rowNode in rowNodes)
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
                        var value = rowNode.CreateNavigator().Evaluate($"string({expr})", namespaceManager);
                        rec.Add(value.ToString());
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
            if (testPattern is null)
                return false;
            
            if (testPattern.Trim( ).Length == 0)
                return false;
            
            if (Regex.Escape(testPattern) != testPattern)
                return false;

            return true;
        }
    }
}
