# FrostNova

A collection of .NET utility libraries targeting WPF desktop application development.  
All packages are distributed via [nuget.org](https://www.nuget.org/packages?q=FrostNova).

---

## Packages

| Package | Target | Description |
|---|---|---|
| `FrostNova.Core` | net8.0 / net9.0 | Platform-agnostic utilities (MVVM base, JSON, text, type helpers) |
| `FrostNova.Core.Wpf` | net8.0-windows+ | WPF behaviors, converters, view services, and attached properties |
| `FrostNova.Core.Wpf.Controls` | net8.0-windows+ | Custom WPF controls (ColorPicker, TimePicker, NumericUpDown, …) |
| `FrostNova.Core.TableWork` | net8.0-windows+ | Tabular data abstraction with CSV / clipboard / JSON I/O (no Excel-engine dependency) |
| `FrostNova.Core.TableWork.Excel` | net8.0-windows+ | Excel (.xlsx) I/O for TableWork as `ITableObject` extension methods (NPOI, Apache-2.0) |
| `FrostNova.Core.ExcelWork` | net8.0-windows+ | EPPlus-based Excel file operations and template engine |
| `FrostNova.Core.ExcelWork.Npoi` | net8.0-windows+ | Drop-in NPOI-based alternative to `FrostNova.Core.ExcelWork` (Apache-2.0; EPPlus-free) |
| `FrostNova.Core.Registry` | net8.0-windows+ | Windows Registry helpers (startup registration, file associations) |
| `FrostNova.Core.NativeAOT.Abstractions` | netstandard2.0+ | Marker attributes consumed by the NativeAOT source generator |
| `FrostNova.Scheduling.Engine` | net8.0 / net9.0 / net10.0 | BOM-based production scheduler (forward / backward) |
| `FrostNova.TimeRangeChart` | net8.0 | Time-range data model and helpers |
| `FrostNova.TimeRangeChart.Wpf` | net8.0-windows+ | Gantt-chart WPF control built on `FrostNova.TimeRangeChart` |
| `FrostNova.WindowEventPickers` | net8.0-windows+ | Global Win32 keyboard / mouse / active-window hooks |

---

## FrostNova.Core

Platform-agnostic utilities for MVVM, JSON persistence, text search, and type manipulation.

**Key types**

- `BindableBase` — thin wrapper over CommunityToolkit `ObservableObject` that exposes a public `RaisePropertyChanged` method
- `AbstractDataModel<T>` — base class for JSON-serialized settings files; override `GetSavePath()` and call `Load()` / `Save()`
- `ObservableDictionary<TKey, TValue>` — `IDictionary` that implements `INotifyCollectionChanged` and `INotifyPropertyChanged`
- `FuzzyTextSearch` — culture-aware, kana-type-insensitive, full-width-insensitive string containment check
- `NaturalSortComparer` — lexicographically natural sort (file explorer order)
- `DeepCloneHelper` — reflection-based deep clone utility
- `AppInstanceManager` — single-instance enforcement via named mutex

```csharp
// JSON settings
public class AppSettings : AbstractDataModel<AppSettings>
{
    public override string GetSavePath() => "settings.json";
    public string Theme { get; set; } = "Dark";
}

var settings = AppSettings.LoadSettings();
settings.Theme = "Light";
settings.Save();
```

---

## FrostNova.Core.Wpf

WPF-specific utilities — behaviors, value converters, view services, and attached properties.

**Key areas**

- **Behaviors** — DataGrid (drag-row, edit-on-enter, header-click-select, paste, checkbox, column sync), TextBox (input format, select-on-focus, clear button), Window (drag-move, location memory, closing guard), ScrollViewer, DragDrop, ListBox, Canvas
- **Converters** — 20+ `IValueConverter` implementations including `BoolToVisibilityConverter`, `EnumBoolConverter`, `EnumFlagsBoolConverter`, `ColorToSolidColorBrushConverter`, `DateTimeFormatConverter`, `InverseBoolConverter`
- **AttachedProperties** — `TextBoxInput` (character filter, format type), `DataGridDragSource`, `DataGridColumnHeaderEx`, `ScrollViewerEx`
- **ViewServices** — `IWindowService` / `AbstractWindowService` for dialog and window management from ViewModels
- **Messages** — `IWpfMessageBox` / `FrostNovaMessageBox` for testable message box calls
- **ViewModelBases** — `ViewModelBase`, `DialogWindowViewModelBase`, `ProgressWindowViewModel`

```xml
<!-- DataGrid drag-row reordering -->
<DataGrid>
    <i:Interaction.Behaviors>
        <fn:DataGridDragRowBehavior />
    </i:Interaction.Behaviors>
</DataGrid>

<!-- TextBox that accepts only numeric input -->
<TextBox fn:TextBoxInput.InputFormat="Numeric" />
```

---

## FrostNova.Core.Wpf.Controls

Custom WPF controls with XAML template support.

**Controls**

- `ColorPicker` — color selection button that opens a full `ColorSelectDialog`
- `ColorPalette` — grid of preset swatches
- `ColorSelector` — HSL slider-based color picker
- `TimePicker` — time-of-day selection control with an inline edit box
- `NumericUpDown` — numeric input with increment / decrement buttons
- `FontComboBox` — font family selector pre-populated with system fonts
- `FrostNovaDataGridTextBoxColumn` — `DataGridTextBoxColumn` subclass with extended paste behavior

```xml
xmlns:fnc="clr-namespace:FrostNova.Core.Wpf.Controls;assembly=FrostNova.Core.Wpf.Controls"

<fnc:ColorPicker SelectedColor="{Binding AccentColor}" />
<fnc:TimePicker SelectedTime="{Binding StartTime}" />
<fnc:NumericUpDown Value="{Binding Quantity}" Minimum="0" Maximum="999" />
```

---

## FrostNova.Core.TableWork

Define your columns once with lambda selectors, then move that data to CSV, JSON, and the
clipboard from the same definition. Excel I/O is a separate opt-in package (see below), so the
core package carries **no Excel-engine dependency**.

**Key types**

- `ITableObject` — the row/column container interface
- `ClassTableObject<T>` — wraps an `IList<T>` of POCOs; `TableContainerBase` wraps a `DataTable`
- `TableColumn<TModel, TProperty>` — a column: lambda selector + optional `HeaderText`, `Format`, and per-channel converters (text vs Excel). Get/set use compiled expression delegates, not reflection
- `TableDataProcessor` — static engine for clipboard (TSV / HTML), CSV, and JSON

```csharp
var table = new ClassTableObject<Product>(products);
table.TargetColumns.AddRange(new TableColumn[]
{
    new TableColumn<Product, string>(x => x.Name,  "Name"),
    new TableColumn<Product, int>(x => x.Qty,      "Qty", format: "#,##0"),
    // "Yes"/"No" as text, native bool in Excel:
    new TableColumn<Product, bool>(x => x.InStock, "In Stock",
        converter: v => v ? "Yes" : "No", excelConverter: v => v),
});

TableDataProcessor.OutputToCsv(table, "output.csv");
string json = TableDataProcessor.ToJson(table, indented: true);
```

---

## FrostNova.Core.TableWork.Excel

Excel (.xlsx) import/export for `TableWork`, provided as `ITableObject` extension methods and built
on **NPOI (Apache-2.0)**. Kept separate so table handling never pulls in an Excel engine (or EPPlus's
non-commercial license). Import maps columns **by header name**, so it tolerates reordered / extra columns.

```csharp
using FrostNova.Core.TableWork.Excel;

table.OutputToExcel("report.xlsx", "Segoe UI", 10f, withHeader: true,
    borderColor: Color.Gray, columnBackColor: Color.LightSteelBlue,
    columnForeColor: Color.Black, nullText: "-", isBoldColumnText: true);

table.CanAddRow = true;
table.ImportFromExcel("user-edited.xlsx", startRow: 0, hasHeader: true);
```

> The former `TableDataProcessor.OutputToExcel` / `ImportFromExcel` static methods are `[Obsolete]`
> and now throw; use the extension methods above.

---

## FrostNova.Core.ExcelWork

Wrapper around [EPPlus](https://epplussoftware.com/) for reading and writing Excel (.xlsx) files.

**Key types**

- `ExcelOperator` — main entry point; open, edit, save, and close workbooks
- `Sheet` — worksheet accessor (read/write cells, rows, columns)
- `Range` / `RangeValue` — cell range operations (formatting, borders, merge, fill)
- `ExcelDrawing` / `ExcelDrawingImage` / `ExcelDrawingChart` / `ExcelDrawingShape` — drawing object management
- `TemplateFramework` — `RangeTemplate` + `KeyCell` for filling pre-designed template sheets

```csharp
using var excel = new ExcelOperator("template.xlsx");
var sheet = excel.GetSheet("Report");
sheet.SetValue(1, 1, "Sales Summary");
sheet.SetValue(2, 1, DateTime.Today);
excel.Save();
```

> ### ⚠️ EPPlus licensing — read before use
>
> `FrostNova.Core.ExcelWork` is MIT-licensed, but it **depends on [EPPlus](https://epplussoftware.com/),
> which is not**. From version 5 onward EPPlus is dual-licensed under the
> [Polyform Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0)
> and a paid commercial license. The MIT license here covers **FrostNova's own code only** — it does
> **not** grant any right to use EPPlus. In particular:
>
> - **Non-commercial use** (personal, or a non-profit / educational organization) is free under Polyform NC.
> - **Commercial use requires a paid EPPlus commercial license** — the MIT badge on this package does *not* make commercial use free.
>
> EPPlus 8 also requires the **consuming application** to declare its license once at startup, before any
> Excel operation runs. FrostNova does not declare it on your behalf — pick the one that matches your use:
>
> ```csharp
> // At application startup, choose exactly one:
> ExcelPackage.License.SetNonCommercialPersonal("Your Name");
> ExcelPackage.License.SetNonCommercialOrganization("Your Org");
> ExcelPackage.License.SetCommercial("<commercial license key>");
> ```
>
> See the [EPPlus license FAQ](https://epplussoftware.com/developers/licenseexception).

---

## FrostNova.Core.ExcelWork.Npoi

A **drop-in alternative** to `FrostNova.Core.ExcelWork`, built on **NPOI (Apache-2.0)** instead of
EPPlus — so both non-commercial and commercial use are free, with no startup license declaration.

The public API (`ExcelOperator`, `Sheet`, `Range`, drawings/charts, `TemplateFramework`) shares the
**same namespace** `FrostNova.Core.ExcelWork` as the EPPlus version, so migrating is a matter of
swapping the package reference — do **not** reference both packages in one project (duplicate types).

```csharp
// same code as FrostNova.Core.ExcelWork — only the PackageReference changes
using var excel = new ExcelOperator("template.xlsx");
var sheet = excel.GetSheet("Report");
sheet.SetValue(1, 1, "Sales Summary");
excel.Save();
```

> **NPOI limitations** (throw `NotSupportedException`): column insert/delete, gradient fill/border on
> shapes, R1C1 formulas, password-protected save.

---

## FrostNova.Core.Registry

Helpers for common Windows Registry / shell-integration tasks. File associations and context menus
are **per-user** (`HKCU\Software\Classes`) and need **no administrator rights**; each has a separate
`...ForAllUsers` method for machine-wide registration (requires elevation).

**Key types**

- `StartupManager` — register / unregister an application in `HKCU\…\Run` (auto-start on login)
- `FileAssociationManager` — associate an extension with an app (`Register` / `Unregister` / `IsRegistered`, plus `...ForAllUsers`); calls `SHChangeNotify` so Explorer refreshes icons immediately
- `ContextMenuManager` — add / remove Explorer right-click verbs on files, folders, or the folder background (`Register` / … plus `...ForAllUsers`)

```csharp
// Auto-start with Windows
StartupManager.RegisterStartup("MyApp");                 // current exe
StartupManager.RegisterStartup("MyApp", arguments: "--minimized");

// File association (per-user, no admin) + immediate shell refresh
FileAssociationManager.Register(".myext", "MyApp.Document", exePath, "MyApp document");
bool ok = FileAssociationManager.IsRegistered(".myext", "MyApp.Document");
FileAssociationManager.Unregister(".myext", "MyApp.Document");
// Machine-wide (all users) — requires administrator:
FileAssociationManager.RegisterForAllUsers(".myext", "MyApp.Document", exePath, "MyApp document");

// "Open with MyApp" on the folder-background right-click menu (passes the folder path via %V)
ContextMenuManager.Register(ContextMenuTarget.DirectoryBackground,
    verb: "OpenWithMyApp", menuText: "Open with MyApp", exePath: exePath);
ContextMenuManager.Unregister(ContextMenuTarget.DirectoryBackground, "OpenWithMyApp");
```

---

## FrostNova.Core.NativeAOT.Abstractions

Marker attributes used by the `FrostNova.Core.NativeAOT.Generators` Roslyn source generator.  
Targets `netstandard2.0` so they can be referenced from any .NET project.

**Attributes**

| Attribute | Effect |
|---|---|
| `[GenerateDeepClone]` | Generates a `DeepClone()` method |
| `[GenerateCopyTo]` | Generates a `CopyTo(target)` method |
| `[GenerateJsonSettings]` | Generates AOT-compatible JSON serialization |
| `[DeepCloneIgnore]` / `[IgnoreCopy]` | Exclude a member from generated copy/clone |

```csharp
[GenerateDeepClone]
[GenerateJsonSettings]
public partial class MySettings
{
    public string Name { get; set; } = "";
    [DeepCloneIgnore]
    public object? RuntimeState { get; set; }
}
```

---

## FrostNova.Scheduling.Engine

Constraint-aware forward / backward BOM scheduler for production planning.

Implements a pluggable optimization architecture: the scheduler iterates over product orderings using an `IOptimizationStrategy` to find the schedule with the best score (makespan, lateness, etc.).

**Key types**

- `BomScheduler` — static entry point; call `BomScheduler.Schedule<…>(…)` with your BOM nodes and resource constraints
- `IOptimizationStrategy` — pluggable ordering strategy
- `GeneticAlgorithmStrategy` — OX crossover + 2-point swap mutation GA with elite preservation; best for 10+ products
- `HillClimbingStrategy` — local search via pairwise swaps; fast for medium-sized problems
- `ExhaustiveStrategy` — full permutation search; guaranteed optimal for up to ~8 products
- `RandomStrategy` — random sampling baseline

**Key interfaces** (implement in your domain model)

- `IBomNode<TBomKey, TOperation, TOperationKey>` — a node in the BOM tree
- `IProcessStep<TOperationKey>` — a schedulable operation with resource, lead time, and setup info
- `IResourceConstraint` — per-step resource candidates with a speed multiplier: `(Id, Efficiency)`, where `2.0` halves the duration

```csharp
var result = BomScheduler.Schedule(
    targetNodes: myBomNodes,
    referenceNodes: confirmedOrders,
    nonWorkingTimes: holidays,
    resourceNonWorkingGetter: r => resourceCalendars[r],
    baseDateGetter: op => dueDateMap[op],
    isForward: false,
    unit: TimeSpan.FromMinutes(1),
    strategy: new GeneticAlgorithmStrategy(new GaParameters(
        PopulationSize: 80,
        Generations: 200,
        MutationRate: 0.05)));
```

**Per-date efficiency**

`IResourceConstraint` fixes an efficiency per resource for all time. To vary it by calendar date — a line running at 80 % on one day, a site-wide slowdown during a heatwave — pass the two daily-efficiency delegates. They mirror the two non-working-time inputs: one global, one per resource.

```csharp
var result = BomScheduler.Schedule(
    // …same arguments as above…
    leadTimeOverride: null,
    setupMatrix: null,
    globalDailyEfficiencyGetter: date => heatwaveDays.Contains(date) ? 0.9 : 1.0,
    resourceDailyEfficiencyGetter: (resource, date) => staffingRatio[resource][date]);
```

The effective rate is `(line efficiency × member efficiency) × global(date) × Π resource(key, date)`, and **it varies within a single operation**: an operation running from the evening of one day into the next consumes work at each day's own rate rather than being fixed at its start date. Dates are taken as `DateTime.Date`; when `unit` does not divide a day evenly, a slice straddling midnight uses its starting date's rate.

Values of `0` or less (and `NaN`) are clamped to `0.01`, so an efficiency of `0` means *100× slower*, not *closed*. To make a day non-working, use `nonWorkingTimes` or `resourceNonWorkingGetter` instead. Omitting both delegates leaves the original code path untouched.

---

## FrostNova.TimeRangeChart / FrostNova.TimeRangeChart.Wpf

Gantt-chart-style WPF control for visualizing and interactively editing time ranges.

**Key types**

- `TimeRange` — observable model representing a single task bar (start, end, working span, row index, stackable)
- `TimeRangeChart` — main WPF control; binds to a collection of `TimeRange` items
- `GridTimeRangeChart` — variant with a left-side data grid synchronized to the chart rows
- `BackgroundTimeRange` — read-only background bands (e.g. non-working periods, capacity limits)

**Features**

- Drag-and-drop task move and resize
- Task stacking within rows
- Milestone markers
- Multiple time-axis scales: hour, day, week, month
- Synchronized horizontal scrolling between chart and grid

```xml
xmlns:tr="clr-namespace:FrostNova.TimeRangeChart.Wpf;assembly=FrostNova.TimeRangeChart.Wpf"

<tr:TimeRangeChart
    ItemsSource="{Binding Tasks}"
    StartDate="{Binding ViewStart}"
    EndDate="{Binding ViewEnd}"
    TimeUnit="Day" />
```

---

## FrostNova.WindowEventPickers

Global (system-wide) event hooks using Win32 low-level hook APIs.

**Key types**

- `KeyEventPicker` — global keyboard hook (`WH_KEYBOARD_LL`); fires on any key down or up regardless of focused window; distinguishes numpad Enter from main Enter
- `MouseEventPicker` — global mouse hook (`WH_MOUSE_LL`); provides click, down, up, wheel, and move callbacks
- `ActiveWindowChangedEventPicker` — polls the foreground window via `GetForegroundWindow` and fires a callback when the active application changes

```csharp
// Monitor all keyboard activity system-wide
KeyEventPicker.SetHook(
    downHandler: e => Console.WriteLine($"Key down: {e.Key}"),
    upHandler:   e => Console.WriteLine($"Key up:   {e.Key}"));

// Stop listening
KeyEventPicker.Unhook();
```

```csharp
// Detect active window changes
ActiveWindowChangedEventPicker.SetHook(e =>
    Console.WriteLine($"Active: {e.Title} ({e.ProcessName})"));
```

> **Note:** Low-level hooks must be installed from a thread that runs a message loop (e.g. the WPF UI thread).  
> Keeping a hook installed without processing messages promptly will cause system-wide input latency.
