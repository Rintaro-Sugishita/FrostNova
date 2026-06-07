# FrostNova.TimeRangeChart

WPF 向けのガントチャートライブラリです。行ヘッダ付きのグリッド表示、ドラッグ&ドロップによるタスク移動・リサイズ、マイルストーン表示、スタック（重ね表示）などに対応しています。

---

## プロジェクト構成

| プロジェクト | ターゲット | 説明 |
|---|---|---|
| `FrostNova.TimeRangeChart` | net8.0 / net9.0 | コアインターフェース・データモデル |
| `FrostNova.TimeRangeChart.Wpf` | net8.0-windows7.0 | WPF コントロール群 |
| `FrostNova.TimeRangeChart.Wpf.Sample` | net8.0-windows7.0 | サンプルアプリ |

---

## クイックスタート

### 1. データモデルの定義

タスクデータは `ITimeRangeRowIndex` を実装します。ライブラリが提供する `TimeRange` 基底クラスを継承するのが最も簡単です。

```csharp
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

public partial class MyTask : TimeRange
{
    // 開始・終了を指定して作成
    public MyTask(DateTime start, DateTime end) : base(start, end) { }

    // 開始＋稼働時間で作成（非稼働時間を考慮）
    public MyTask(DateTime start, TimeSpan workingSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        : base(start, workingSpan, nonWorkingTimes) { }

    // 表示用プロパティを自由に追加
    [ObservableProperty] private Color _color = Colors.SteelBlue;
    [ObservableProperty] private string _label = "";
}
```

行データは、タスクのコレクションを持つ任意のクラスで構いません。

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

public partial class MyRow : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private ObservableCollection<MyTask> _tasks = new();
}
```

ViewModel でデータを用意します。

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
            var row = new MyRow { Name = $"担当者 {i + 1}" };
            var task = new MyTask(DateTime.Today.AddDays(i), TimeSpan.FromDays(3))
            {
                RowIndex = i,
                Label = $"タスク {i + 1}"
            };
            row.Tasks.Add(task);
            Rows.Add(row);
        }
    }
}
```

---

### 2. XAML への配置

```xml
<Window xmlns:chart="clr-namespace:FrostNova.TimeRangeChart.Wpf;assembly=FrostNova.TimeRangeChart.Wpf"
        xmlns:local="clr-namespace:MyApp">
    <Window.Resources>
        <!-- テーマファイルを読み込む -->
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

        <!-- 行ヘッダの表示内容 -->
        <chart:GridTimeRangeChart.RowHeaderTemplate>
            <DataTemplate DataType="{x:Type local:MyRow}">
                <TextBlock Text="{Binding Name}" VerticalAlignment="Center" Margin="4,0" />
            </DataTemplate>
        </chart:GridTimeRangeChart.RowHeaderTemplate>

        <!-- タスクバーの表示内容 -->
        <chart:GridTimeRangeChart.ItemTemplate>
            <DataTemplate DataType="{x:Type local:MyTask}">
                <TextBlock Text="{Binding Label}" Foreground="White" Margin="4,2" />
            </DataTemplate>
        </chart:GridTimeRangeChart.ItemTemplate>

    </chart:GridTimeRangeChart>
</Window>
```

---

## 主要プロパティ一覧

### 時間軸

| プロパティ | 型 | 説明 |
|---|---|---|
| `StartTime` | `DateTime` | チャートの開始時刻 |
| `EndTime` | `DateTime` | チャートの終了時刻 |
| `ScaleIncrement` | `TimeSpan` | 主目盛の間隔（例: `TimeSpan.FromDays(1)`） |
| `SubScaleFormat` | `string` | 副目盛のフォーマット文字列（例: `"HH:mm"`） |
| `HeaderFormat` | `string` | ヘッダの日時フォーマット（例: `"MM/dd"`、`"yyyy/MM"`） |
| `MinSpan` | `TimeSpan` | タスクの最小幅（ドラッグ時の下限） |
| `TimeUnit` | `TimeUnitType` | `Normal`（時間・日単位）/ `Month`（月単位）|
| `MonthSpan` | `int` | 月表示モード時の1目盛あたりの月数 |

### 表示・レイアウト

| プロパティ | 型 | 既定値 | 説明 |
|---|---|---|---|
| `WidthPerRuledLine` | `double` | `100` | 1目盛あたりのピクセル幅 |
| `RuledLineThickness` | `double` | `1` | グリッド線の太さ |
| `RuledLineFill` | `Brush` | — | グリッド線の色 |
| `ChartHeight` | `double` | `80` | 行の基本高さ |
| `MinRowWidth` | `double` | — | 行ヘッダの最小幅 |

### データ

| プロパティ | 型 | 説明 |
|---|---|---|
| `ItemsSource` | `IList` | 行オブジェクトのリスト |
| `TasksSourcePath` | `string` | 行オブジェクト上のタスクコレクションのプロパティ名（例: `"Tasks"`） |
| `BackgroundTimes` | `IList<IBackgroundTimeRange>` | 背景色付き範囲のリスト |
| `NonWorkingTimes` | `IList<ITimeRangeRowIndex>` | 非稼働時間のリスト（グレーアウト表示） |

### スタック（重ね表示）

| プロパティ | 型 | 既定値 | 説明 |
|---|---|---|---|
| `CanStackRange` | `bool` | `false` | タスクの重ね表示を許可する |
| `StackHeight` | `double` | `24` | スタック時の1タスクあたりの高さ |

### ドラッグ操作

| プロパティ | 型 | 既定値 | 説明 |
|---|---|---|---|
| `AllowMultiSelection` | `bool` | `true` | 複数タスクの選択を許可 |
| `AllowMultiDrag` | `bool` | `true` | 複数タスクの同時ドラッグを許可 |

### ビューポート（読み取り専用）

スクロール中に自動更新されます。

| プロパティ | 型 | 説明 |
|---|---|---|
| `CurrentFirstRow` | `object?` | 表示中の最上行オブジェクト |
| `CurrentLastRow` | `object?` | 表示中の最下行オブジェクト |
| `CurrentLeftTime` | `DateTime` | 表示中の左端時刻 |
| `CurrentCenterTime` | `DateTime` | 表示中の中央時刻 |
| `CurrentRightTime` | `DateTime` | 表示中の右端時刻 |

---

## テンプレートカスタマイズ

### タスクバーの見た目（ItemTemplate）

`ItemTemplate` に `DataTemplate` を設定します。DataContext はタスクオブジェクトです。

```xml
<chart:GridTimeRangeChart.ItemTemplate>
    <DataTemplate DataType="{x:Type local:MyTask}">
        <Border Background="{Binding Color, Converter={...}}" CornerRadius="3">
            <TextBlock Text="{Binding Label}" Foreground="White" Margin="4,2" />
        </Border>
    </DataTemplate>
</chart:GridTimeRangeChart.ItemTemplate>
```

複数の型に応じてテンプレートを切り替えたい場合は `ItemTemplateSelector` を使います。

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

### TimeRangeControl のスタイル（TimeRangeStyle）

タスクバー自体（`TimeRangeControl`）のプロパティをまとめて制御したい場合は `TimeRangeStyle` を使います。

```xml
<chart:GridTimeRangeChart.TimeRangeStyle>
    <Style TargetType="{x:Type chart:TimeRangeControl}"
           BasedOn="{StaticResource {x:Type chart:TimeRangeControl}}">
        <Setter Property="Height" Value="36" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="CanDrag" Value="True" />
        <Setter Property="CanExpandSpan" Value="True" />
        <!-- DataTrigger でタスクの値に応じてスタイルを変える -->
        <Style.Triggers>
            <DataTrigger Binding="{Binding IsUrgent}" Value="True">
                <Setter Property="Background" Value="Red" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</chart:GridTimeRangeChart.TimeRangeStyle>
```

#### TimeRangeControl の主なプロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| `CanDrag` | `bool` | 移動ドラッグを許可 |
| `CanExpandSpan` | `bool` | 左右のリサイズハンドルを表示 |
| `MinSpan` | `TimeSpan?` | タスク個別の最小幅 |
| `Height` | `double` | バーの高さ |
| `SelectedBorderBrush` | `Brush` | 選択時の枠色（既定: 赤） |
| `SelectedBorderThickness` | `Thickness` | 選択時の枠幅 |

---

## マイルストーン表示

`TimeRangeControl` の `IsMilestone` を `true` にするとマイルストーンアイコンとして描画されます。

```xml
<chart:GridTimeRangeChart.TimeRangeStyle>
    <Style TargetType="{x:Type chart:TimeRangeControl}"
           BasedOn="{StaticResource {x:Type chart:TimeRangeControl}}">
        <Setter Property="IsMilestone" Value="True" />
        <Setter Property="MilestoneType" Value="Diamond" />
        <Setter Property="SymbolSize" Value="20" />
        <Setter Property="LabelPosition" Value="Bottom" />
        <Setter Property="Height" Value="40" />
        <!-- 行ごとにアイコンを切り替える例 -->
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

#### MilestoneType の選択肢

`None` / `Circle` / `Diamond` / `Square` / `Triangle` / `InvertedTriangle` / `Cross` / `X` / `Star` / `Hexagon` / `ArrowUp` / `ArrowDown`

#### LabelPosition の選択肢

`Left` / `Right` / `Top` / `Bottom`

---

## 背景色付き範囲（BackgroundTimeRange）

特定の期間に背景色を表示したい場合は `BackgroundTimeRange` を使います。

```csharp
using FrostNova.TimeRangeChart.Wpf;
using System.Windows.Media;

var bgRange = new BackgroundTimeRange
{
    RowIndex  = 0,
    StartTime = new DateTime(2024, 6, 1),
    EndTime   = new DateTime(2024, 6, 3),
    Background = new SolidColorBrush(Color.FromArgb(80, 255, 200, 0))
};
BackgroundList.Add(bgRange);
```

```xml
<chart:GridTimeRangeChart BackgroundTimes="{Binding BackgroundList}" />
```

### ホバー時のポップアップ

`BackgroundTimeRangePopupTemplate` に `DataTemplate` を設定すると、マウスオーバー時にポップアップが表示されます。DataContext は `IBackgroundTimeRange` インスタンスです。

```xml
<chart:GridTimeRangeChart BackgroundTimes="{Binding BackgroundList}">
    <chart:GridTimeRangeChart.BackgroundTimeRangePopupTemplate>
        <DataTemplate>
            <Border Background="White" Padding="8"
                    BorderBrush="#AAAAAA" BorderThickness="1"
                    CornerRadius="4">
                <StackPanel>
                    <TextBlock Text="{Binding StartTime, StringFormat='開始: {0:MM/dd}'}" />
                    <TextBlock Text="{Binding EndTime,   StringFormat='終了: {0:MM/dd}'}" />
                </StackPanel>
            </Border>
        </DataTemplate>
    </chart:GridTimeRangeChart.BackgroundTimeRangePopupTemplate>
</chart:GridTimeRangeChart>
```

---

## 非稼働時間（NonWorkingTimes）

土日や休日などを `FixedTimeRange` でリストに追加すると、該当行・時間帯に `NonWorkingRangeBackground` の色が塗られます。

```csharp
using FrostNova.TimeRangeChart.TimeRangeHelpers;

// 土日を非稼働時間に設定
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

## ドラッグ&ドロップイベント

### イベント一覧

| イベント | 引数型 | タイミング |
|---|---|---|
| `TaskDragStart` | `DragDropRangeEventArgs` | ドラッグ開始時 |
| `TaskDragEnd` | `DragDropRangeEventArgs` | ドラッグ完了時（時間移動・リサイズ） |
| `PreviewTaskDrop` | `DropOnRowEventArgs` | 行をまたぐドロップ確定前（キャンセル可） |
| `TaskDrop` | `DropOnRowEventArgs` | 行をまたぐドロップ確定後（キャンセル可） |

### XAML での購読（Microsoft.Xaml.Behaviors 使用例）

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

### DragDropRangeEventArgs の主なプロパティ

| プロパティ | 説明 |
|---|---|
| `DragData` | ドラッグ対象タスク（`ITimeRangeRowIndex`） |
| `DragStartTime` / `DragEndTime` | ドラッグ開始時点の時刻 |
| `DropStartTime` | ドロップ後の開始時刻 |
| `DropWorkingSpan` | ドロップ後の稼働時間 |
| `Cancel` | `true` にすると変更を取り消す |

### DropOnRowEventArgs の主なプロパティ

| プロパティ | 説明 |
|---|---|
| `TaskObject` | 移動対象タスク |
| `OldRowIndex` | 移動前の行インデックス |
| `NewRowIndex` | 移動先の行インデックス |
| `DropTime` | ドロップした時刻 |
| `Cancel` | `true` にすると行移動を取り消す |

```csharp
[RelayCommand]
private void PreviewTaskDrop(DropOnRowEventArgs e)
{
    // 特定の行へのドロップを禁止する例
    if (e.NewRowIndex == 0)
        e.Cancel = true;
}
```

---

## 月単位表示（Month ビュー）

`TimeUnit="Month"` に設定すると、月カレンダー形式のチャートになります。

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

## ヘッダ色のカスタマイズ

`HeaderBrushSelector` に関数を設定すると、日付に応じてヘッダ色を変えられます。

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

## ビューポートの監視

表示範囲が変わるたびに `ViewPortChanged` イベントが発火します。`CurrentFirstRow` / `CurrentLastRow` プロパティを読み取ることで、現在表示中の行を把握できます。

```xml
<chart:GridTimeRangeChart x:Name="chart"
    CurrentFirstRow="{Binding FirstVisibleRow, Mode=OneWayToSource}"
    CurrentCenterTime="{Binding CenterTime, Mode=OneWayToSource}" />
```
