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

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ItemsControlHeightSyncBehavior : Behavior<ItemsControl>
    {
        public static readonly DependencyProperty TargetItemsControlProperty =
               DependencyProperty.Register(
                   nameof(TargetItemsControl),
                   typeof(ItemsControl),
                   typeof(ItemsControlHeightSyncBehavior),
                   new PropertyMetadata(null, OnTargetItemsControlChanged));

        public ItemsControl TargetItemsControl
        {
            get => (ItemsControl)GetValue(TargetItemsControlProperty);
            set => SetValue(TargetItemsControlProperty, value);
        }

        private INotifyCollectionChanged? _sourceCollection;
        private INotifyCollectionChanged? _targetCollection;

        protected override void OnAttached()
        {
            base.OnAttached();
            RegisterAll(AssociatedObject, TargetItemsControl);
        }

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
            //if (AssociatedObject == null || TargetItemsControl == null)
            //    return;

            //int count = Math.Min(AssociatedObject.Items.Count, TargetItemsControl.Items.Count);

            //for (int i = 0; i < count; i++)
            //{
            //    var container = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            //    var targetContainer = TargetItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

            //    if (container != null && targetContainer != null)
            //    {
            //        double maxHeight = Math.Max(container.ActualHeight, targetContainer.ActualHeight);
            //        if (maxHeight > 0)
            //        {
            //            container.Height = maxHeight;
            //            targetContainer.Height = maxHeight;
            //        }
            //    }
            //}
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
                        RegisterContentSizeChanged(container1);
                        RegisterContentSizeChanged(container2);
                        container1.SizeChanged -= OnContainerSizeChanged;
                        container1.SizeChanged += OnContainerSizeChanged;
                        container2.SizeChanged -= OnContainerSizeChanged;
                        container2.SizeChanged += OnContainerSizeChanged;
                        // 1. まず現在の MinHeight をリセットして、本来の ActualHeight を測定できるようにする
                        // (これを行わないと、一度大きくなった後に小さくなれない)
                        // ただし、頻繁なリセットはガタつきの原因になるため、
                        // 必要に応じて「前回同期した値」を保持するなどの工夫も検討してください。

                        double h1 = GetContentHeight(container1);
                        double h2 = GetContentHeight(container2);

                        double maxHeight = Math.Max(h1, h2);

                        if (maxHeight > 0)
                        {
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


        

        private double GetContentHeight(FrameworkElement element)
        {
            // MinHeightの影響を除外した、現在のコンテンツそのものの高さを取得
            // 既にMinHeightが設定されている場合、ActualHeightはMinHeight以下にならないため
            return element.ActualHeight;
        }

        private void UpdateMinHeight(FrameworkElement element, double height)
        {
            // 無限ループ防止: 差分が微小なら更新しない
            if (Math.Abs(element.MinHeight - height) > 0.01)
            {
                element.MinHeight = height;
            }
        }
        private void RegisterContentSizeChanged(FrameworkElement container)
        {
            if (container == null) return;

            // ListBoxItem 等の直下の子供（通常は ContentPresenter を含むグリッド等）を取得
            if (System.Windows.Media.VisualTreeHelper.GetChildrenCount(container) > 0)
            {
                //var child = System.Windows.Media.VisualTreeHelper.GetChild(container, 0) as FrameworkElement;
                var child = GeneralHelper.FindVisualChild<ContentPresenter>(container) as FrameworkElement;

                if (child != null)
                {
                    var child2 = System.Windows.Media.VisualTreeHelper.GetChild(child, 0) as FrameworkElement;
                    if (child2 != null)
                    {
                        // 重複登録を避けて購読
                        child2.SizeChanged -= OnContainerSizeChanged;
                        child2.SizeChanged += OnContainerSizeChanged;
                    }
                }
            }
        }
    }
}
