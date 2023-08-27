using CsvHelper;

namespace csv_diff;

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
        var encoding = options?.GetValueOrDefault("encoding") as string;
        var csvOptions = options?.GetValueOrDefault("csv_options") as Dictionary<string, object>;

        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
        if (csvOptions != null)
        {
            foreach (var kvp in csvOptions)
            {
                config.GetType().GetProperty(kvp.Key)?.SetValue(config, kvp.Value);
            }
        }

        using (var reader = new StreamReader(filePath, encoding != null ? System.Text.Encoding.GetEncoding(encoding) : null))
        using (var csv = new CsvReader(reader, config))
        {
            Data = csv.GetRecords<string[]>().ToList();
        }
    }
}