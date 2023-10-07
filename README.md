# CSVDiff

[![Sponsors on Open Collective](https://opencollective.com/csvdiff/sponsors/badge.svg)](#sponsors)
<a href="https://www.nuget.org/packages/CSVDiff"><img src="https://img.shields.io/nuget/v/CSVDiff.svg" alt="NuGet Version" /></a>
<a href="https://www.nuget.org/packages/CSVDiff"><img src="https://img.shields.io/nuget/dt/CSVDiff.svg" alt="NuGet Download Count" /></a>

A library for performing diffs of Arrays of Arrays, CSV files, or XML files.

## Install

### Package Manager Console

```
PM> Install-Package CSVDiff
```

### .NET CLI Console

```
> dotnet add package CSVDiff
```

## Usage

Comparing two CSV files is as simple as:

```csharp
var diff = new CSVDiff(leftSource, rightSource);
```

Inspect the CSVDiff instance:
```csharp
diff.Summary   // Summary of the adds, deletes, updates, and moves
diff.Adds      // Details of the additions to file2
diff.Deletes   // Details of the deletions to file1
diff.Updates   // Details of the updates from file1 to file2
diff.Moves     // Details of the moves from file1 to file2
diff.Diffs     // Details of all differences
diff.Warnings  // Any warnings generated during the diff process
```

## Warnings

Warnings may be raised for any of the following:
* Missing fields: If the right/to file contains fields that are not present in the
  left/from file, a warning is raised and the field is ignored for diff purposes.
* Duplicate keys: If two rows are found that have the same values for the key field(s),
  a warning is raised, and the duplicate values are ignored.

## Options

The following options are available when creating a CSVDiff instance:

#### Unique row identifier options
* `key_field` | `key_fields`:
  * The column index(es) or name(s) of the unique identifier(s) for each row.
* `parent_field` | `parent_fields`:
  * The column index(es) or name(s) of the parent identifier(s) for each row.
* `child_field` | `child_fields`:
  * The column index(es) or name(s) of the child identifier(s) for each row.
* `field_names`:
  * The names of each column in the data. If not supplied, the first row of the data
    is used as the column names.
  * If your data file does contain a header row, but you wish to use your own column
    names, you can specify the **:field_names** option and the **:ignore_header** option to
    ignore the first row.


#### Diff options
* `ignore_fields`:
  * The column index(es) or name(s) of the fields to ignore.
* `ignore_header`:
  * Ignore the first row of the data.
* `ignore_deletes`:
  * Ignore deletes when generating the diff.
* `ignore_updates`:
  * Ignore updates when generating the diff.
* `ignore_adds`:
  * Ignore adds when generating the diff.
* `ignore_moves`:
  * Ignore moves when generating the diff.


* `case_sensitive`:
  * Ignore case when comparing values.
* `trim_whitespace`:
  * Strip leading and trailing whitespace from values.


#### Filtering options
* `include`:
  * A Dictionary of field names or indexes (0-based) and regular expressions or functions
    to be applied to values of the corresponding field. Rows will only be diffed if they satisfy
    :include conditions.
* `exclude`:
  * A Dictionary of field names or indexes (0-based) and regular expressions or functions
    to be applied to values of the corresponding field. Rows will not be diffed if they satisfy
    :exclude conditions.

## Examples

#### `key_field` | `key_fields`

Specify the column index(es) of the unique identifier(s) in your file using:

```csharp
var keyFields = new Dictionary<string, object>
{
    { "key_fields", new [] { 0, 1 } }
};

var diff = new CSVDiff(Data1, Data2, keyFields);
```

Or, using the column name(s) (optional setting the **field_names** option):

```csharp
var keyFields = new Dictionary<string, object>
{
    { "key_fields", new [] { "First Name", "Last Name" } }
};

var diff = new CSVDiff(Data1, Data2, keyFields);
```


#### `parent_field(s)` / `child_field(s)`

Use this option when your data represents a tree structure flattened to a table in parent-child form.

Using the **:parent_fields** and **:child_fields** with field numbers:

```csharp
var parentChildFields = new Dictionary<string, object>
{
  { "parent_field", 1 },
  { "child_fields", new [] { 2, 3 } }
};

var diff = new CSVDiff(Data1, Data2, parentChildFields);
```

Using the **:parent_fields** and **:child_fields** with column names:

```csharp
var parentChildFields = new Dictionary<string, object>
{
  { "parent_field", "Date" },
  { "child_fields", new [] { "HomeTeam", "AwayTeam" } }
  
};

var diff = new CSVDiff(Data1, Data2, parentChildFields);
```

### Using Non-CSV Sources

Data from non-CSV sources can be diffed, as long as it can be supplied as an List of Arrays:
```csharp
var data1 = new List<string[]>()
{
    new[] { "Parent", "Child", "Description" },
    new[] { "A", "A1", "Account1" },
    new[] { "A", "A2", "Account 2" },
    new[] { "A", "A3", "Account 3" },
    new[] { "A", "A4", "Account 4" },
    new[] { "A", "A6", "Account 6" }
};

var data2 = new List<string[]>()
{
    new[] { "Parent", "Child", "Description" },
    new[] { "A", "A1", "Account1" },
    new[] { "A", "A2", "Account2" },
    new[] { "A", "a3", "ACCOUNT 3" },
    new[] { "A", "A5", "Account 5" },
    new[] { "B", "A6", "Account 6" },
    new[] { "C", "A6", "Account 6c" }
};

var diff = new CSVDiff(data1, data2, new Dictionary<string, object>
{
    { "parent_field", 0 },
    { "child_field", 1 }
});
```

### Using XML Sources

Data from XML sources, can also be diffed, with the help of the XMLSource class.

```csharp
var keyField = new Dictionary<string, object>
{
  { "key_field", "COL_A" }
};

var xmlSource1 = new XMLSource('The path to the file OR label', keyField);
```

#### Processing XML Documents
Next, we pass XML document(s) to this source, and specify XPath expressions for each
row and column of data to produce via the `Process(source, rowsXPath, fieldMaps)`
method:

* The source parameter is the XML document to be parsed. This can be a string
  containing the XML, or an instance of `System.Xml.XmlDocument`.
* The rowsXPath parameter is an XPath expression that will be used to select
  each row of data from the document. For example, if you wanted to parse an HTML
  table, you might use `'//table/tbody/tr'` to select each `<tr>` element in the
  table.
* The fieldMaps parameter is a `Dictionary` of field names and XPath expressions that
  will be used to select the value for each column in the row. For example, if
  you wanted to parse an HTML table with columns for Name and Age, you might use
  `{ "Name", "./td[0]/text()" }, { "Age", "./td[1]/text() }` to select the content of the
  first `<td>` element in the row for the Name column, and the content of the
  second `<td>` element for the Age column.


```csharp
xmlSource1.Process(source, '//table/tbody/tr', new Dictionary<string, string>
{
    { "COL_A", "./td[0]/text()" },
    { "COL_B", "./td[1]/text()" },
    { "COL_C", "./td[2]/text()" }
});
```

Finally, to diff two XML sources, we create a CSVDiff object with two XMLSource
objects as the source:
```csharp
var diff = new CSVDiff(xmlSource1, xmlSource2, keyField)
```

## Credits

### Inspiration

This library would not be possible without the work of Andrew Gardiner and his project:
https://github.com/agardiner/csv-diff

### Contributors

This project exists thanks to all the people who contribute.

<a href="https://github.com/WillemOpperman/csv-diff-dotnet/graphs/contributors"><img src="https://opencollective.com/csvdiff/contributors.svg?width=890&button=false" /></a>

### Backers

Thank you to all our backers! 🙏 [[Become a backer](https://opencollective.com/csvdiff#backer)]

<a href="https://opencollective.com/csvhelper#backers" target="_blank"><img src="https://opencollective.com/csvdiff/backers.svg?width=890"></a>

### Sponsors

Support this project by becoming a sponsor. Your logo will show up here with a link to your website. [[Become a sponsor](https://opencollective.com/csvdiff#sponsor)]

<a href="https://opencollective.com/csvdiff/sponsor/0/website" target="_blank"><img src="https://opencollective.com/csvdiff/sponsor/0/avatar.svg"></a>