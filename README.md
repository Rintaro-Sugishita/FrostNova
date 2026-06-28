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
| `FrostNova.Core.TableWork` | net8.0-windows+ | Tabular data abstraction with CSV / clipboard / JSON / Excel I/O |
| `FrostNova.Core.ExcelWork` | net8.0-windows+ | EPPlus-based Excel file operations and template engine |
| `FrostNova.Core.Registry` | net8.0-windows+ | Windows Registry helpers (startup registration, file associations) |
| `FrostNova.Core.NativeAOT.Abstractions` | netstandard2.0+ | Marker attributes consumed by the NativeAOT source generator |
| `FrostNova.Scheduling.Engine` | net8.0 / net9.0 | BOM-based production scheduler (forward / backward) |
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

Abstraction layer over tabular data (`DataTable` or custom collections) with unified import / export.

**Key types**

- `ITableObject` — common interface implemented by all table containers
- `TableContainerBase` — `DataTable`-backed implementation of `ITableObject`
- `TableColumn` / `TableColumn<T>` — column descriptor with type information
- `TableDataProcessor` — static methods for clipboard paste (TSV), CSV read/write, JSON import/export, and Excel sheet I/O

```csharp
var table = new TableContainerBase(myDataTable);

// Paste TSV from clipboard
TableDataProcessor.PasteFromClipboard(table, Clipboard.GetText(), 0, 0);

// Export to CSV
TableDataProcessor.ExportToCsv(table, "output.csv");
```

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

---

## FrostNova.Core.Registry

Helpers for common Windows Registry operations.

**Key types**

- `StartupManager` — register or unregister an application in `HKCU\…\Run` (auto-start on login)
- `FileAssociationManager` — associate a file extension with an application via `HKEY_CLASSES_ROOT`

```csharp
// Register this app to start with Windows
StartupManager.RegisterStartup("MyApp");

// Check if already registered
bool isRegistered = StartupManager.IsRegistered("MyApp");

// Remove from startup
StartupManager.UnregisterStartup("MyApp");
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
