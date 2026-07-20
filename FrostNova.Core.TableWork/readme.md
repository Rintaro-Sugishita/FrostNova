# FrostNova.Core.TableWork

**Define your columns once — then move that data to Excel, CSV, JSON, and the clipboard, all from the same definition.**

Most libraries solve one channel: CsvHelper does CSV, MiniExcel/EPPlus do Excel, `System.Text.Json` does JSON. `TableWork` gives you a single, strongly-typed **column model** (`x => x.Property` selectors with a header, a format, and per-channel converters) and fans it out to every format — including Excel-friendly clipboard HTML for copy-paste. It's aimed at WPF / WinForms line-of-business apps where a `DataGrid` bound to `List<T>` needs to round-trip to spreadsheets and back.

```
┌──────────────┐      OutputToCsv / ToCsvText
│              │ ───► OutputToExcel  (FrostNova.Core.TableWork.Excel)
│  TableColumn │ ───► ToJson
│  definitions │ ───► ToClipboardTextAsHtml  (paste into Excel with styles)
│  (define     │ ◄─── ImportFromExcel  (map by header name, reorder-safe)
│   once)      │ ◄─── ImportFromFile / PasteFromText  (CSV / TSV)
│              │ ◄─── PasteFromJson
└──────────────┘
```

## Why this instead of CsvHelper / MiniExcel?

| | CsvHelper | MiniExcel / EPPlus | **TableWork** |
|---|:---:|:---:|:---:|
| Lambda column mapping | ✅ | attribute-based | ✅ |
| CSV / TSV | ✅ | — | ✅ |
| Excel `.xlsx` in/out | — | ✅ | ✅ (opt-in package) |
| JSON | — | — | ✅ |
| **Excel-friendly clipboard HTML** | — | — | ✅ |
| **One definition for all channels** | — | — | ✅ |
| **Per-channel converters** (text vs Excel) | — | — | ✅ |

The point isn't "lambda mapping" (CsvHelper does that well) — it's that **one** column model drives **every** channel, and each channel can render a value differently (e.g. a `bool` shows `"Yes"/"No"` in text but stays a native boolean in Excel).

## Install

```
dotnet add package FrostNova.Core.TableWork          # core: CSV / TSV / JSON / clipboard
dotnet add package FrostNova.Core.TableWork.Excel     # optional: .xlsx in/out (NPOI, Apache-2.0)
```

The core package has **no Excel-engine dependency**. Excel I/O lives in the separate `FrostNova.Core.TableWork.Excel` package, which is built on **NPOI (Apache-2.0)** — so pulling in table handling never drags in EPPlus's non-commercial license.

## Quick start

```csharp
using FrostNova.Core.TableWork;

public class Product
{
    public string  Name    { get; set; } = "";
    public int     Qty     { get; set; }
    public decimal Price   { get; set; }
    public bool    InStock { get; set; }
}

var products = new List<Product>
{
    new() { Name = "Apple",  Qty = 3, Price = 120.50m, InStock = true  },
    new() { Name = "Banana", Qty = 5, Price =  80.00m, InStock = false },
};

// Define the columns ONCE.
var table = new ClassTableObject<Product>(products);
table.TargetColumns.AddRange(new TableColumn[]
{
    new TableColumn<Product, string>(x => x.Name,  "Name"),
    new TableColumn<Product, int>(x => x.Qty,      "Qty",   format: "#,##0"),
    new TableColumn<Product, decimal>(x => x.Price,"Price", format: "#,##0.00"),

    // Different representation per channel:
    //   text/CSV/clipboard -> "Yes"/"No";  Excel -> native bool
    new TableColumn<Product, bool>(x => x.InStock, "In Stock",
        converter:      v => v ? "Yes" : "No",
        excelConverter: v => v),
});
```

Now the same `table` feeds every format:

```csharp
// CSV
TableDataProcessor.OutputToCsv(table, "products.csv");
string csv = TableDataProcessor.ToCsvText(table);

// JSON
string json = TableDataProcessor.ToJson(table, indented: true);

// Excel-friendly clipboard (paste into Excel keeps colors + types)
string html = TableDataProcessor.ToClipboardTextAsHtml(
    table,
    columnBackColor: Color.Navy, columnForeColor: Color.White,
    nullForeColor: Color.Red, nullText: "N/A",
    isWrapColumnText: false, isBoldColumnText: true);
Clipboard.SetText(html, TextDataFormat.Html);
```

## Excel I/O (`FrostNova.Core.TableWork.Excel`)

Excel export/import are **extension methods** on `ITableObject`. Add the package and a `using`:

```csharp
using FrostNova.Core.TableWork.Excel;

// Export a styled workbook
table.OutputToExcel(
    "report.xlsx", fontFamily: "Segoe UI", fontSize: 10f, withHeader: true,
    borderColor: Color.Gray, columnBackColor: Color.LightSteelBlue,
    columnForeColor: Color.Black, nullText: "-", isBoldColumnText: true);

// Import a user-edited workbook.
// Columns are matched by HEADER NAME, so reordered / extra columns are handled;
// set CanAddRow to let the import grow the collection.
table.CanAddRow = true;
table.ImportFromExcel("user-edited.xlsx", startRow: 0, hasHeader: true);
```

> The old `TableDataProcessor.OutputToExcel(...)` / `ImportFromExcel(...)` static methods are `[Obsolete]` and now throw — they moved here to keep the core package Excel-free. Switch to the extension-method form above.

## API surface (`TableDataProcessor`, core package)

| Method | Direction | Format |
|---|---|---|
| `PasteFromClipboard` / `PasteFromText` | in | TSV / delimited text |
| `CopyToClipboard` / `ToClipboardTextAsHtml` | out | clipboard HTML (CF_HTML) |
| `OutputToCsv` / `ToCsvText` | out | CSV / TSV |
| `ImportFromFile` | in | CSV / TSV file |
| `ToJson` / `PasteFromJson` | out / in | JSON |

Excel (`FrostNova.Core.TableWork.Excel`): `ITableObject.OutputToExcel(...)`, `ITableObject.ImportFromExcel(...)`.

## How it works

- **`ITableObject`** — the row/column container. `ClassTableObject<T>` wraps an `IList<T>` of POCOs; `TableContainerBase` wraps a `DataTable`.
- **`TableColumn<TModel, TProperty>`** — a column described by a lambda selector plus optional `HeaderText`, `Format`, and converters. Get/set use **compiled expression delegates** (built once per column), so per-cell access is a direct delegate call, not reflection.
- **`TableDataProcessor`** — the static engine for the non-Excel channels.

Per-channel converters on each column:

| Channel | Read | Write |
|---|---|---|
| Text / CSV / clipboard | `SetValueFromText` (`backConverter`) | `GetTextValue` (`converter`) |
| Excel | `SetValueFromExcel` (`excelBackConverter`) | `GetExcelValue` (`excelConverter`) |

## License

MIT. The core package depends only on `FrostNova.Core`. The optional `FrostNova.Core.TableWork.Excel` package uses **NPOI (Apache-2.0)** — free for both non-commercial and commercial use.
