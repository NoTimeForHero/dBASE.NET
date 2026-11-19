# dBASE.NET - Read and write DBF files with .NET

This is the fork of [henck/dBASE.NET](https://github.com/henck/dBASE.NET).\
Deeply thankful to him for awesome library.

The primary target of this fork is fast reading of DBF tables created by [Harbour](https://harbour.github.io/).\
It is based on [dBase Level 7 Specification](https://www.dbf2002.com/dbf-file-format.html) with possible additions.

For Harbour tables, use `FastDbf.ReadFile` with the encoding used by the source table and `readOnly: true`. Harbour fields `@` and `=` are read as `DateTime` with `DateTimeKind.Unspecified` and millisecond precision. Empty timestamps return `null`. The enum member `DbfFieldType.UnixTime` still identifies the `@` field. [Harbour fixtures and benchmarks](dBASE.NET.Tests/fixtures/harbour/README.md)

`FastDbf.VisitRows` visits physical records synchronously and decodes selected values on demand. Pass zero-based schema indexes to select columns. Inside the callback, use zero-based positions in that selection. `GetNumber` reads `N`, `GetText` reads `C`, and `GetValue` uses the regular decoder for the selected field. The row reader is valid for the duration of the callback.

```csharp
table.VisitRows(row =>
{
    if (row.IsDeleted || row.GetNumber(0) != desiredCity) return;
    Console.WriteLine(row.GetText(1));
}, cityColumn, nameColumn);
```

The callback can check deletion and filter fields before decoding the rest of the selection. [Filtered scan and benchmark plan](docs/plans/20261119-filtered-dbf-scan.md)

Current progress:
1. Perfomance
   - [x] Lazy manipulation with the file (without reading it fully to the memory and rewriting the full file on the drive)
2. Extra fields
   - [x] Reading extra fields +/^/@/?
   - [ ] Full support for extra fields (like AutoIncrement)
3. Memo fields
   - [x] DBT format
   - [x] FPT format
   - [x] Both
      - [x] Read and write memo fields
      - [x] Creating new memo files
      - [x] Supports write memo blocks larger than %BlockSize
      - [x] Move memo block to end of file on overflow  
      - [x] Variable memo block size (not only 64 byte)
      - [x] Packing file (remove all unused blocks)
     
4. Indexes support
   - [ ] Under construction...

Useful links:
- [xBase Format](https://www.clicketyclick.dk/databases/xbase/format/)   

## Base information

__dBASE.NET__ is a .NET class library used to read FoxBase, dBASE III and dBASE IV .dbf files. Data read
from a file is returned as a list of typed fields and a list of records. This library is useful to add
data import from dBASE sources to a .NET project.

This code has been tested against a number of dBASE files found in the wild, including FoxBase and dBASE III/IV
files with and without memo files. A .NET unit test project is part of this repository and new test files
may be added to it over time.

There is [an article describing the dBASE file format](http://www.independent-software.com/dbase-dbf-dbt-file-format.html).

## Opening a DBF file

```c#
using dBASE.NET;

dbf.Read("database.dbf");
```

This returns an instance of the `Dbf` class. With this, you can iterate over fields found in the table:

```c#
foreach(DbfField field in dbf.Fields) {
	Console.WriteLine(field.Name);
}
```

You can also iterate over records:

```c#
foreach(DbfRecord record in dbf.Records) {
	for(int i = 0;  i < dbf.Fields.Count; i++) {
		Console.WriteLine(record[i]);
	}
}
```

Count the records:

```c#
Console.WriteLine(dbf.Records.Count);
```

## Working with memo files

When memo file accompanying the `.dbf` file is found (either `.dbt` or `.fpt`), with the same base name as the table file, then 
dBASE.NET will load the memo file's contents. 

## Writing a DBF file

To write DBF data, you can either create a new instance of `Dbf`, then create fields and records, or load an existing table and modify its fields or records.

This sample code creates a new table with a single character field, then saves the .dbf file:

```c#
dbf = new Dbf();
DbfField field = new DbfField("TEST", DbfFieldType.Character, 12);
dbf.Fields.Add(field);
DbfRecord record = dbf.CreateRecord();
record.Data[0] = "HELLO";
dbf.Write("test.dbf", DbfVersion.VisualFoxPro);
```

## Supported Field types

| Code | Field type   | .NET counterpart |
|:-----|:-------------|:-----------------|
| `C`  | Character string | String |
| `D`  | Date             | DateTime |
| `I`  | Integer          | Int32 |
| `L`  | Logical          | Bool |
| `M`  | Memo             | String |
| `N`  | Numeric          | Double |
| `T`  | DateTime         | DateTime |
| `Y`  | Currency         | Float |


## Class diagram

![Class diagram](http://yuml.me/1cc9f823.png)

_yuml:_

```
http://yuml.me/diagram/scruffy/class/edit/[Dbf]+->[DbfRecord], [Dbf]+->[DbfField], [DbfRecord]+->[DbfField], [Dbf]->[DbfHeader], [DbfHeader]^-[Dbf4Header]
```` 

## Versions

* Version 1.2.2: Fix reading DbfField from file with `invalid` name.
* Version 1.2.1: Fixed buffer overflow on write operation.
  Fixed trimming and padding on write operation.
  Performance improve on write operation.
* Version 1.2.0: Sign assembly.
* Version 1.1.0: Add support for custom character encoding
* Version 1.0.0: Initial release
