# FrostNova.Core.TableWork

A robust, high-performance C# library designed to synchronize data between application objects and various external formats (Excel, CSV, JSON, HTML).

Unlike standard serializers, this library is built around a **Column-Mapping Architecture**, allowing for safe data round-trips even when users modify file structures or formats.

## Key Features

* **Smart Excel Integration**:
* **Export**: Generates styled Excel files or HTML snippets (CF_HTML) for pixel-perfect copy-pasting.
* **Import**: Maps Excel columns to application properties by **Header Name**, making it resilient to column reordering by users.


* **Universal Formats**: Full support for CSV, TSV, and JSON.
* **Type-Safe Conversion**: Uses a centralized `TableColumn` system to handle type conversion, formatting, and validation across all input/output channels.
* **Memory Efficient**: Utilizes stream-based parsing for large CSV/TSV files to maintain a low memory footprint.

---

## Core Architecture

The library operates on a three-tier abstraction:

1. **`ITableObject`**: The data container interface. It manages rows and defines which columns are targeted.
2. **`TableColumn`**: The brain of the operation. It defines how a specific property is converted to text (for CSV), formatted for Excel (as an object), or parsed back from a string.
3. **`TableDataProcessor`**: The static engine that executes the import/export logic.

---

## Usage Examples

### 1. Exporting to Excel with Styles

```csharp
TableDataProcessor.OutputToExcel(
    myTable, 
    "Report.xlsx", 
    fontFamily: "Segoe UI", 
    fontSize: 10,
    withHeader: true,
    borderColor: Color.Gray,
    columnBackColor: Color.LightBlue,
    columnForeColor: Color.Black,
    nullText: "-",
    isBoldColumnText: true
);

```

### 2. Importing from a User-Edited Excel File

This method identifies columns by their header names, automatically skipping unknown columns or handling reordered ones.

```csharp
TableDataProcessor.ImportFromExcel(
    myTable, 
    "UserEditedData.xlsx", 
    startRow: 0, 
    hasHeader: true
);

```

### 3. Copy-Paste Support (Excel-Friendly HTML)

Generate a clipboard-compatible string that maintains colors and data types when pasted into Excel.

```csharp
string htmlData = TableDataProcessor.CreateCopyTextAsHtml(
    myTable,
    columnBackColor: Color.Navy,
    columnForeColor: Color.White,
    nullForeColor: Color.Red,
    nullText: "N/A",
    isWrapColumnText: false,
    isBoldColumnText: true
);
Clipboard.SetText(htmlData);

```

### 4. JSON Serialization

```csharp
string json = TableDataProcessor.ToJson(myTable, indented: true);

```


---

## Implementation Notes

When implementing `TableColumn`, ensure you provide logic for:

* `GetTextValue`: For flat text formats (CSV/TSV).
* `GetExcelValue`: For typed Excel cells (Preserves `DateTime`, `int`, etc.).
* `SetValueFromText`: For parsing clipboard or CSV strings.
* `SetValueFromExcel`: For processing raw objects from Excel cells.

---

Would you like me to **add a specific "Getting Started" guide for developers who need to implement the `ITableObject` interface?**
