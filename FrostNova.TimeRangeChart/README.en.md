# FrostNova.TimeRangeChart

A WPF Gantt chart library featuring a row-header grid layout, drag-and-drop task movement and resizing, milestone markers, and stacked (overlapping) task display.

---

## Project Structure

| Project | Target | Description |
|---|---|---|
| `FrostNova.TimeRangeChart` | net8.0 / net9.0 | Core interfaces and data models |
| `FrostNova.TimeRangeChart.Wpf` | net8.0-windows7.0 | WPF controls |
| `FrostNova.TimeRangeChart.Wpf.Sample` | net8.0-windows7.0 | Sample application |

---

## Quick Start

### 1. Define your data model

Task data must implement `ITimeRangeRowIndex`. The easiest way is to extend the `TimeRange` base class provided by the library.

```csharp
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

public partial class MyTask : TimeRange
{
    // Create from explicit start and end times
    public MyTask(DateTime start, DateTime end) : base(start, end) { }

    // Create from start time + working duration (non-working times are taken into account)
    public MyTask(DateTime start, TimeSpan workingSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        : base(start, workingSpan, nonWorkingTimes) { }

    // Add any display properties you need
    [ObservableProperty] private Color _color = Colors.SteelBlue;
    [ObservableProperty] private string _label = "";
}
```

Row data can be any class that holds a collection of tasks.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

public partial class MyRow : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private ObservableCollection<MyTask> _tasks = new();
}
```

Populate data in a ViewModel.

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<MyRow> _rows = new();
    [ObservableProperty] private DateTime _chartStart = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _chartEnd   = DateTime.Today.AddDays(30);

    public MyViewModel()
    {
        for (int i = 0; i < 5; i++)
        {
            var row = new MyRow { Name = $"Member {i + 1}" };
            var task = new MyTask(DateTime.Today.AddDays(i), TimeSpan.FromDays(3))
            {
                RowIndex = i,
                Label = $"Task {i + 1}"
            };
            row.Tasks.Add(task);
            Rows.Add(row);
        }
    }
}
```

---

### 2. Add to XAML

```xml
<Window xmlns:chart="clr-namespace:FrostNova.TimeRangeChart.Wpf;assembly=FrostNova.TimeRangeChart.Wpf"
        xmlns:local="clr-namespace:MyApp">
    <Window.Resources>
        <!-- Load the theme files -->
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/FrostNova.TimeRangeChart.Wpf;component/Themes/TimeRangeChartStyle.xaml" />
                <ResourceDictionary Source="/FrostNova.TimeRangeChart.Wpf;component/Themes/TimeRangeControlStyle.xaml" />
                <ResourceDictionary Source="/FrostNova.TimeRangeChart.Wpf;component/Themes/GridTimeRangeChartStyle.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <chart:GridTimeRangeChart
        StartTime="{Binding ChartStart}"
        EndTime="{Binding ChartEnd}"
        ItemsSource="{Binding Rows}"
        TasksSourcePath="Tasks"
        HeaderFormat="MM/dd"
        WidthPerRuledLine="100"
        MinRowWidth="150">

        <!-- Row header content -->
        <chart:GridTimeRangeChart.RowHeaderTemplate>
            <DataTemplate DataType="{x:Type local:MyRow}">
                <TextBlock Text="{Binding Name}" VerticalAlignment="Center" Margin="4,0" />
            </DataTemplate>
        </chart:GridTimeRangeChart.RowHeaderTemplate>

        <!-- Task bar content -->
        <chart:GridTimeRangeChart.ItemTemplate>
            <DataTemplate DataType="{x:Type local:MyTask}">
                <TextBlock Text="{Binding Label}" Foreground="White" Margin="4,2" />
            </DataTemplate>
        </chart:GridTimeRangeChart.ItemTemplate>

    </chart:GridTimeRangeChart>
</Window>
```

---

## Key Properties

### Time Axis

| Property | Type | Description |
|---|---|---|
| `StartTime` | `DateTime` | Chart start time |
| `EndTime` | `DateTime` | Chart end time |
| `ScaleIncrement` | `TimeSpan` | Major tick interval (e.g. `TimeSpan.FromDays(1)`) |
| `SubScaleFormat` | `string` | DateTime format for minor ticks (e.g. `"HH:mm"`) |
| `HeaderFormat` | `string` | DateTime format for the header (e.g. `"MM/dd"`, `"yyyy/MM"`) |
| `MinSpan` | `TimeSpan` | Minimum task duration (drag lower bound) |
| `TimeUnit` | `TimeUnitType` | `Normal` (hours/days) or `Month` (calendar months) |
| `MonthSpan` | `int` | Number of months per tick in Month mode |

### Appearance & Layout

| Property | Type | Default | Description |
|---|---|---|---|
| `WidthPerRuledLine` | `double` | `100` | Pixels per major tick |
| `RuledLineThickness` | `double` | `1` | Grid line thickness |
| `RuledLineFill` | `Brush` | — | Grid line color |
| `ChartHeight` | `double` | `80` | Base row height |
| `MinRowWidth` | `double` | — | Minimum row header width |

### Data

| Property | Type | Description |
|---|---|---|
| `ItemsSource` | `IList` | List of row objects |
| `TasksSourcePath` | `string` | Name of the task collection property on each row object (e.g. `"Tasks"`) |
| `BackgroundTimes` | `IList<IBackgroundTimeRange>` | Colored background ranges |
| `NonWorkingTimes` | `IList<ITimeRangeRowIndex>` | Non-working periods (rendered with a dimmed overlay) |

### Stacking (overlapping tasks)

| Property | Type | Default | Description |
|---|---|---|---|
| `CanStackRange` | `bool` | `false` | Allow overlapping task display |
| `StackHeight` | `double` | `24` | Height per task when stacked |

### Drag Operations

| Property | Type | Default | Description |
|---|---|---|---|
| `AllowMultiSelection` | `bool` | `true` | Allow selecting multiple tasks |
| `AllowMultiDrag` | `bool` | `true` | Allow dragging multiple selected tasks simultaneously |

### Viewport (read-only)

Updated automatically as the user scrolls.

| Property | Type | Description |
|---|---|---|
| `CurrentFirstRow` | `object?` | Topmost visible row object |
| `CurrentLastRow` | `object?` | Bottommost visible row object |
| `CurrentLeftTime` | `DateTime` | Left edge of the visible area |
| `CurrentCenterTime` | `DateTime` | Center of the visible area |
| `CurrentRightTime` | `DateTime` | Right edge of the visible area |

---

## Template Customization

### Task bar appearance (ItemTemplate)

Assign a `DataTemplate` to `ItemTemplate`. The DataContext is the task object.

```xml
<chart:GridTimeRangeChart.ItemTemplate>
    <DataTemplate DataType="{x:Type local:MyTask}">
        <Border Background="{Binding Color, Converter={...}}" CornerRadius="3">
            <TextBlock Text="{Binding Label}" Foreground="White" Margin="4,2" />
        </Border>
    </DataTemplate>
</chart:GridTimeRangeChart.ItemTemplate>
```

To switch templates based on the task type, use `ItemTemplateSelector`.

```csharp
public class MyTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NormalTemplate { get; set; }
    public DataTemplate? SpecialTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is SpecialTask ? SpecialTemplate : NormalTemplate;
}
```

```xml
<chart:GridTimeRangeChart ItemTemplateSelector="{StaticResource mySelector}" />
```

### TimeRangeControl style (TimeRangeStyle)

Use `TimeRangeStyle` to control properties of the task bar control (`TimeRangeControl`) directly.

```xml
<chart:GridTimeRangeChart.TimeRangeStyle>
    <Style TargetType="{x:Type chart:TimeRangeControl}"
           BasedOn="{StaticResource {x:Type chart:TimeRangeControl}}">
        <Setter Property="Height" Value="36" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="CanDrag" Value="True" />
        <Setter Property="CanExpandSpan" Value="True" />
        <!-- Change appearance based on task data with a DataTrigger -->
        <Style.Triggers>
            <DataTrigger Binding="{Binding IsUrgent}" Value="True">
                <Setter Property="Background" Value="Red" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</chart:GridTimeRangeChart.TimeRangeStyle>
```

#### Key TimeRangeControl properties

| Property | Type | Description |
|---|---|---|
| `CanDrag` | `bool` | Allow moving the task by dragging |
| `CanExpandSpan` | `bool` | Show left/right resize handles on hover |
| `MinSpan` | `TimeSpan?` | Per-task minimum duration |
| `Height` | `double` | Bar height |
| `SelectedBorderBrush` | `Brush` | Border color when selected (default: Red) |
| `SelectedBorderThickness` | `Thickness` | Border thickness when selected |

---

## Milestone Display

Set `IsMilestone="True"` on a `TimeRangeControl` to render it as a milestone icon instead of a bar.

```xml
<chart:GridTimeRangeChart.TimeRangeStyle>
    <Style TargetType="{x:Type chart:TimeRangeControl}"
           BasedOn="{StaticResource {x:Type chart:TimeRangeControl}}">
        <Setter Property="IsMilestone" Value="True" />
        <Setter Property="MilestoneType" Value="Diamond" />
        <Setter Property="SymbolSize" Value="20" />
        <Setter Property="LabelPosition" Value="Bottom" />
        <Setter Property="Height" Value="40" />
        <!-- Switch icon per row -->
        <Style.Triggers>
            <DataTrigger Binding="{Binding RowIndex}" Value="0">
                <Setter Property="MilestoneType" Value="Circle" />
            </DataTrigger>
            <DataTrigger Binding="{Binding RowIndex}" Value="1">
                <Setter Property="MilestoneType" Value="Star" />
                <Setter Property="LabelPosition" Value="Right" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</chart:GridTimeRangeChart.TimeRangeStyle>
```

#### Available MilestoneType values

`None` / `Circle` / `Diamond` / `Square` / `Triangle` / `InvertedTriangle` / `Cross` / `X` / `Star` / `Hexagon` / `ArrowUp` / `ArrowDown`

#### Available LabelPosition values

`Left` / `Right` / `Top` / `Bottom`

---

## Background Ranges (BackgroundTimeRange)

Use `BackgroundTimeRange` to shade a time span in a specific row with a colored background.

```csharp
using FrostNova.TimeRangeChart.Wpf;
using System.Windows.Media;

var bgRange = new BackgroundTimeRange
{
    RowIndex   = 0,
    StartTime  = new DateTime(2024, 6, 1),
    EndTime    = new DateTime(2024, 6, 3),
    Background = new SolidColorBrush(Color.FromArgb(80, 255, 200, 0))
};
BackgroundList.Add(bgRange);
```

```xml
<chart:GridTimeRangeChart BackgroundTimes="{Binding BackgroundList}" />
```

### Hover popup

Assign a `DataTemplate` to `BackgroundTimeRangePopupTemplate` to show a popup when the user hovers over a background range. The DataContext of the template is the `IBackgroundTimeRange` instance.

```xml
<chart:GridTimeRangeChart BackgroundTimes="{Binding BackgroundList}">
    <chart:GridTimeRangeChart.BackgroundTimeRangePopupTemplate>
        <DataTemplate>
            <Border Background="White" Padding="8"
                    BorderBrush="#AAAAAA" BorderThickness="1"
                    CornerRadius="4">
                <StackPanel>
                    <TextBlock Text="{Binding StartTime, StringFormat='Start: {0:MM/dd}'}" />
                    <TextBlock Text="{Binding EndTime,   StringFormat='End:   {0:MM/dd}'}" />
                </StackPanel>
            </Border>
        </DataTemplate>
    </chart:GridTimeRangeChart.BackgroundTimeRangePopupTemplate>
</chart:GridTimeRangeChart>
```

---

## Non-Working Times (NonWorkingTimes)

Add periods such as weekends or holidays as `FixedTimeRange` entries. The affected row/time combinations are shaded with `NonWorkingRangeBackground`.

```csharp
using FrostNova.TimeRangeChart.TimeRangeHelpers;

// Mark weekends as non-working
for (var day = chartStart; day <= chartEnd; day = day.AddDays(1))
{
    if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
    {
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            NonWorkingTimes.Add(new FixedTimeRange(day.Date, TimeSpan.FromDays(1))
            {
                RowIndex = rowIndex
            });
        }
    }
}
```

```xml
<chart:GridTimeRangeChart
    NonWorkingTimes="{Binding NonWorkingTimes}"
    NonWorkingRangeBackground="#1A000000" />
```

---

## Drag & Drop Events

### Event overview

| Event | Argument type | Fires when |
|---|---|---|
| `TaskDragStart` | `DragDropRangeEventArgs` | A drag operation begins |
| `TaskDragEnd` | `DragDropRangeEventArgs` | A task is moved or resized |
| `PreviewTaskDrop` | `DropOnRowEventArgs` | Before a cross-row drop is committed (cancellable) |
| `TaskDrop` | `DropOnRowEventArgs` | After a cross-row drop is committed (cancellable) |

### Subscribing in XAML (Microsoft.Xaml.Behaviors)

```xml
<i:Interaction.Triggers>
    <i:EventTrigger EventName="TaskDragEnd">
        <i:InvokeCommandAction Command="{Binding TaskDragEndCommand}" PassEventArgsToCommand="True" />
    </i:EventTrigger>
    <i:EventTrigger EventName="PreviewTaskDrop">
        <i:InvokeCommandAction Command="{Binding PreviewTaskDropCommand}" PassEventArgsToCommand="True" />
    </i:EventTrigger>
    <i:EventTrigger EventName="TaskDrop">
        <i:InvokeCommandAction Command="{Binding TaskDropCommand}" PassEventArgsToCommand="True" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

### DragDropRangeEventArgs properties

| Property | Description |
|---|---|
| `DragData` | The task being dragged (`ITimeRangeRowIndex`) |
| `DragStartTime` / `DragEndTime` | Times at the start of the drag |
| `DropStartTime` | Start time after the drop |
| `DropWorkingSpan` | Working duration after the drop |
| `Cancel` | Set to `true` to reject the change |

### DropOnRowEventArgs properties

| Property | Description |
|---|---|
| `TaskObject` | The task being moved |
| `OldRowIndex` | Source row index |
| `NewRowIndex` | Destination row index |
| `DropTime` | Time position where the task was dropped |
| `Cancel` | Set to `true` to reject the row change |

```csharp
[RelayCommand]
private void PreviewTaskDrop(DropOnRowEventArgs e)
{
    // Prevent dropping onto row 0
    if (e.NewRowIndex == 0)
        e.Cancel = true;
}
```

---

## Month View

Set `TimeUnit="Month"` to switch to a calendar-month layout.

```xml
<chart:GridTimeRangeChart
    TimeUnit="Month"
    WidthPerRuledLine="180"
    HeaderFormat="yyyy/MM"
    StartTime="{Binding ChartStart}"
    EndTime="{Binding ChartEnd}"
    ItemsSource="{Binding Rows}"
    TasksSourcePath="Tasks"
    CanStackRange="True" />
```

---

## Header Color Customization

Assign a function to `HeaderBrushSelector` to color header cells based on their date.

```csharp
HeaderBrushSelector = (chart, date) =>
    date.DayOfWeek switch
    {
        DayOfWeek.Sunday   => Brushes.LightCoral,
        DayOfWeek.Saturday => Brushes.LightBlue,
        _                  => Brushes.White
    };
```

---

## Viewport Monitoring

The `ViewPortChanged` event fires whenever the visible area changes. The `CurrentFirstRow` / `CurrentLastRow` properties tell you which rows are currently on screen.

```xml
<chart:GridTimeRangeChart x:Name="chart"
    CurrentFirstRow="{Binding FirstVisibleRow, Mode=OneWayToSource}"
    CurrentCenterTime="{Binding CenterTime, Mode=OneWayToSource}" />
```
