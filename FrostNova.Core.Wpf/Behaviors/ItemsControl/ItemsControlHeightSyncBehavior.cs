using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FrostNova.Core.Wpf.Behaviors;

/// <summary>
/// two ItemsControls to synchronize the heights of their items.
/// This behavior listens for changes in the item containers of both ItemsControls and updates the MinHeight of each item container to match the tallest corresponding item. This ensures that items in both lists have the same height, which is particularly useful for side-by-side lists where you want each row to align visually.
/// </summary>
public class ItemsControlHeightSyncBehavior : Behavior<ItemsControl>
{
    /// <summary>
    /// <inheritdoc cref="TargetItemsControl"/>
    /// </summary>
    public static readonly DependencyProperty TargetItemsControlProperty =
           DependencyProperty.Register(
               nameof(TargetItemsControl),
               typeof(ItemsControl),
               typeof(ItemsControlHeightSyncBehavior),
               new PropertyMetadata(null, OnTargetItemsControlChanged));

    /// <summary>
    /// target ItemsControl to synchronize with. When set, the behavior will ensure that the heights of corresponding items in the associated ItemsControl and the target ItemsControl are kept in sync. This is useful for scenarios like side-by-side lists where you want each row to have the same height across both lists.
    /// </summary>
    public ItemsControl TargetItemsControl
    {
        get => (ItemsControl)GetValue(TargetItemsControlProperty);
        set => SetValue(TargetItemsControlProperty, value);
    }

    /// <summary>
    /// <inheritdoc cref="DefaultMinHeight"/>
    /// </summary>
    public static readonly DependencyProperty DefaultMinHeightProperty =
            DependencyProperty.Register(nameof(DefaultMinHeight), typeof(double), typeof(ItemsControlHeightSyncBehavior),
                new PropertyMetadata(0.0));

    /// <summary>
    /// default minimum height for items. This property sets the default MinHeight for each item container in both the associated ItemsControl and the target ItemsControl. If the calculated height for an item is less than this value, the MinHeight will be set to this default value instead. This ensures that items do not become too small, maintaining a consistent appearance across both lists.
    /// </summary>
    public double DefaultMinHeight
    {
        get => (double)GetValue(DefaultMinHeightProperty);
        set => SetValue(DefaultMinHeightProperty, value);
    }

    private INotifyCollectionChanged? _sourceCollection;
    private INotifyCollectionChanged? _targetCollection;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        RegisterAll(AssociatedObject, TargetItemsControl);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        UnregisterAll(AssociatedObject, TargetItemsControl);
    }

    private static void OnTargetItemsControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemsControlHeightSyncBehavior behavior)
        {
            if (e.OldValue is ItemsControl oldTarget) behavior.UnregisterAll(behavior.AssociatedObject,oldTarget);
            if (e.NewValue is ItemsControl newTarget) behavior.RegisterAll(behavior.AssociatedObject, newTarget);
            behavior.SynchronizeHeights();
        }
    }
    private bool _isSynchronizing = false;
    //private void AttachHandlers()
    //{
    //    if (AssociatedObject != null)
    //    {
    //        AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
    //        _sourceCollection = AssociatedObject.ItemsSource as INotifyCollectionChanged;
    //        if (_sourceCollection != null)
    //            _sourceCollection.CollectionChanged += Source_CollectionChanged;

    //    }

    //    if (TargetItemsControl != null)
    //    {
    //        TargetItemsControl.ItemContainerGenerator.StatusChanged += Target_ItemContainerGenerator_StatusChanged;
    //        _targetCollection = TargetItemsControl.ItemsSource as INotifyCollectionChanged;
    //        if (_targetCollection != null)
    //            _targetCollection.CollectionChanged += Target_CollectionChanged;
    //    }
    //}

    //private void DetachHandlers()
    //{
    //    if (AssociatedObject != null)
    //    {
    //        AssociatedObject.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
    //        if (_sourceCollection != null)
    //            _sourceCollection.CollectionChanged -= Source_CollectionChanged;
    //        _sourceCollection = null;
    //    }

    //    if (TargetItemsControl != null)
    //    {
    //        TargetItemsControl.ItemContainerGenerator.StatusChanged -= Target_ItemContainerGenerator_StatusChanged;
    //        if (_targetCollection != null)
    //            _targetCollection.CollectionChanged -= Target_CollectionChanged;
    //        _targetCollection = null;
    //    }
    //}
    //private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
    //{
    //    if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
    //    {
    //        SynchronizeHeights();
    //    }
    //}

    //private void Target_ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
    //{
    //    if (TargetItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
    //    {
    //        SynchronizeHeights();
    //    }
    //}

    //private static void OnTargetItemsControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //    if (d is ItemsControlHeightSyncBehavior b)
    //    {
    //        b.UnregisterAll(b.AssociatedObject, e.OldValue as ItemsControl);
    //        b.RegisterAll(b.AssociatedObject, e.NewValue as ItemsControl);
    //    }
    //}
    private void RegisterAll(ItemsControl? source, ItemsControl? target)
    {
        RegisterControl(source, ref _sourceCollection);
        RegisterControl(target, ref _targetCollection);
        SynchronizeHeights();
    }

    private void UnregisterAll(ItemsControl? source, ItemsControl? target)
    {
        UnregisterControl(source, ref _sourceCollection);
        UnregisterControl(target, ref _targetCollection);
    }

    private void RegisterControl(ItemsControl? ic, ref INotifyCollectionChanged? field)
    {
        if (ic == null) return;
        ic.ItemContainerGenerator.StatusChanged += OnStatusChanged;

        // ItemsSourceの変更を監視
        if (ic.ItemsSource is INotifyCollectionChanged newColl)
        {
            field = newColl;
            field.CollectionChanged += OnCollectionChanged;
        }
    }

    private void UnregisterControl(ItemsControl? ic, ref INotifyCollectionChanged? field)
    {
        if (ic == null) return;
        ic.ItemContainerGenerator.StatusChanged -= OnStatusChanged;
        if (field != null)
        {
            field.CollectionChanged -= OnCollectionChanged;
            field = null;
        }
        // コンテナのイベントは個別に解除する必要があるが、
        // コンテナ自体が破棄されるため通常はリークしない。厳密には弱イベント検討。
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SynchronizeHeights();
    private void OnStatusChanged(object? sender, EventArgs e)
    {
        if (sender is ItemContainerGenerator g && g.Status == GeneratorStatus.ContainersGenerated)
            SynchronizeHeights();
    }

    //private void OnStatusChanged(object? sender, EventArgs e)
    //{
    //    if (sender is ItemContainerGenerator gen && gen.Status == GeneratorStatus.ContainersGenerated)
    //    {
    //        //TrackNewContainers();
    //        SynchronizeHeights();
    //    }
    //}
    //private void TrackNewContainers()
    //{
    //    // 両方の ItemsControl の全コンテナを走査
    //    var controls = new[] { AssociatedObject, TargetItemsControl };
    //    foreach (var ic in controls)
    //    {
    //        if (ic == null) continue;
    //        for (int i = 0; i < ic.Items.Count; i++)
    //        {
    //            if (ic.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
    //            {
    //                if (!_trackedContainers.Contains(container))
    //                {
    //                    container.SizeChanged += OnContainerSizeChanged;
    //                    _trackedContainers.Add(container);
    //                }
    //            }
    //        }
    //    }
    //}
    //private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    //{
    //    SynchronizeHeights();
    //}

    //private void Target_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    //{
    //    SynchronizeHeights();
    //}
    private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 高さの変化（Widthのみの変化は無視）を確認して同期
        if (e.HeightChanged)
        {
            SynchronizeHeights();
        }
    }
    private void SynchronizeHeights()
    {
        if (_isSynchronizing || AssociatedObject == null || TargetItemsControl == null) return;

        _isSynchronizing = true;
        try
        {
            int count = Math.Min(AssociatedObject.Items.Count, TargetItemsControl.Items.Count);

            for (int i = 0; i < count; i++)
            {
                var container1 = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                var container2 = TargetItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

                if (container1 != null && container2 != null)
                {
                    var content1 = FindActualContent(container1);
                    var content2 = FindActualContent(container2);

                    if (content1 != null && content2 != null)
                    {
                        // イベント購読（既存のロジック）
                        content1.SizeChanged -= OnContainerSizeChanged;
                        content1.SizeChanged += OnContainerSizeChanged;
                        content2.SizeChanged -= OnContainerSizeChanged;
                        content2.SizeChanged += OnContainerSizeChanged;

                        // --- 【追加】サイズダウン対応ロジック ---

                        container1.MinHeight = DefaultMinHeight;
                        container2.MinHeight = DefaultMinHeight;
                        // 各コンテンツに「理想のサイズ」を計算させる（Measure）
                        var availableSize = new Size(content1.ActualWidth, double.PositiveInfinity);
                        content1.Measure(availableSize);
                        content2.Measure(availableSize);

                        // DesiredSize（理想のサイズ）から、大きい方を採用する
                        double h1 = content1.DesiredSize.Height;
                        double h2 = content2.DesiredSize.Height;

                        double maxHeight = Math.Max(DefaultMinHeight, Math.Max(h1, h2));
                       
                            UpdateMinHeight(container1, maxHeight);
                            UpdateMinHeight(container2, maxHeight);
                       
                    }
                }
            }
        }
        finally
        {
            _isSynchronizing = false;
        }
    }
    /// <summary>
    /// Get content from items container
    /// </summary>
    /// <param name="container"></param>
    /// <returns></returns>
    private FrameworkElement? FindActualContent(FrameworkElement container)
    {
        // ContentPresenterを探す
        var cp = GeneralHelper.FindVisualChild<ContentPresenter>(container);
        if (cp != null && System.Windows.Media.VisualTreeHelper.GetChildrenCount(cp) > 0)
        {
            // ContentPresenterの最初の子（DataTemplateのルート）を返す
            return System.Windows.Media.VisualTreeHelper.GetChild(cp, 0) as FrameworkElement;
        }
        return null;
    }


    private void UpdateMinHeight(FrameworkElement element, double height)
    {
        // 無限ループ防止: 差分が微小なら更新しない
        if (Math.Abs(element.MinHeight - height) > 0.01)
        {
            element.MinHeight = height;
        }
    }
}
