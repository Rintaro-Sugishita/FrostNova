using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// DataGrid.Columns に ObservableCollection&lt;DataGridColumn&gt; をバインドするための添付プロパティ。
/// 各行の DataContext は RowInternalContainer であるため、列のバインディングには "RawData.PropertyName" を使用してください。
/// </summary>
public static class DataGridColumnsBehavior
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.RegisterAttached(
            "Columns",
            typeof(ObservableCollection<DataGridColumn>),
            typeof(DataGridColumnsBehavior),
            new PropertyMetadata(null, OnColumnsChanged));

    public static ObservableCollection<DataGridColumn>? GetColumns(DependencyObject obj)
        => (ObservableCollection<DataGridColumn>?)obj.GetValue(ColumnsProperty);

    public static void SetColumns(DependencyObject obj, ObservableCollection<DataGridColumn>? value)
        => obj.SetValue(ColumnsProperty, value);

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid) return;

        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += (_, _) => RebuildColumns(dataGrid);

        RebuildColumns(dataGrid);
    }

    private static void RebuildColumns(DataGrid dataGrid)
    {
        dataGrid.Columns.Clear();
        var source = GetColumns(dataGrid);
        if (source == null) return;
        foreach (var col in source)
            dataGrid.Columns.Add(col);
    }
}
