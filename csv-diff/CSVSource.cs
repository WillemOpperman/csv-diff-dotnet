using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;

namespace csv_diff
{
    // Represents a CSV input (i.e., the left/from or right/to input) to the diff process.
    public class CSVSource : Source
    {
        // Constructor
        public CSVSource(object source, Dictionary<string, object> options = null) : base(options)
        {
            if (source is string filePath)
            {
                ReadCSVFile(filePath, options);
            }
            else if (source is IEnumerable<string[]> dataRows)
            {
                Data = dataRows.ToList();
            }
            else
            {
                throw new ArgumentException("source must be a path to a file or an IEnumerable<IEnumerable<string>>");
            }

            IndexSource();
        }

        private void ReadCSVFile(string filePath, Dictionary<string, object> options)
        {
            options.TryGetValue("encoding", out var tempEncoding);
            var encoding = tempEncoding as string;
            options.TryGetValue("encoding", out var tempCsvOptions);
            var csvOptions = tempCsvOptions as Dictionary<string, object>;

            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null,
                Delimiter = ",",
                Quote = '"',
                AllowComments = false,
                Comment = '#',
            };

            if (csvOptions != null)
            {
                foreach (var kvp in csvOptions)
                {
                    config.GetType().GetProperty(kvp.Key)?.SetValue(config, kvp.Value);
                }
            }

            Data = new List<string[]>();
            using (var reader = new StreamReader(filePath, encoding != null ? System.Text.Encoding.GetEncoding(encoding) : new UTF8Encoding(false)))
            {
                using (var csvParser = new CsvParser(reader, config))
                {
                    while (csvParser.Read())
                    {
                        Data.Add(csvParser.Record.ToArray());
                    }
                }
            }
        }
    }
}
