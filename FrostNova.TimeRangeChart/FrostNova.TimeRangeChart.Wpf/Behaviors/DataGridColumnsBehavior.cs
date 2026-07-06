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

    // 購読中のハンドラを DataGrid ごとに保持し、コレクション差し替え時に解除できるようにする。
    private static readonly DependencyProperty HandlerProperty =
        DependencyProperty.RegisterAttached(
            "Handler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(DataGridColumnsBehavior),
            new PropertyMetadata(null));

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid) return;

        // 旧コレクションの購読を解除（未解除だと古いコレクションが DataGrid を参照し続けリークする）
        if (e.OldValue is INotifyCollectionChanged oldColl
            && dataGrid.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler oldHandler)
        {
            oldColl.CollectionChanged -= oldHandler;
        }

        if (e.NewValue is INotifyCollectionChanged newColl)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => RebuildColumns(dataGrid);
            dataGrid.SetValue(HandlerProperty, handler);
            newColl.CollectionChanged += handler;
        }
        else
        {
            dataGrid.ClearValue(HandlerProperty);
        }

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
