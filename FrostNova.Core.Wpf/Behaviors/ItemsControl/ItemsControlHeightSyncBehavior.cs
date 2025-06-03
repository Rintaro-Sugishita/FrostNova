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

        private INotifyCollectionChanged? _notifyCollection;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            TrySubscribeToCollectionChanged();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
            UnsubscribeFromCollectionChanged();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
            SyncHeightsIfPossible();
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncHeightsIfPossible();
        }

        private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                SyncHeightsIfPossible();
            }
        }

        private void SyncHeightsIfPossible()
        {
            if (TargetItemsControl == null)
                return;

            SetSync(AssociatedObject, TargetItemsControl);
        }

        private void TrySubscribeToCollectionChanged()
        {
            if (AssociatedObject.ItemsSource is INotifyCollectionChanged notify)
            {
                _notifyCollection = notify;
                _notifyCollection.CollectionChanged += ItemsSource_CollectionChanged;
            }
        }

        private void UnsubscribeFromCollectionChanged()
        {
            if (_notifyCollection != null)
            {
                _notifyCollection.CollectionChanged -= ItemsSource_CollectionChanged;
                _notifyCollection = null;
            }
        }

        private void ItemsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SyncHeightsIfPossible();
        }

        private static void SetSync(ItemsControl src, ItemsControl dest)
        {
            if (src == null || dest == null) return;
            int count = Math.Min(src?.Items.Count ?? 0, dest?.Items.Count ?? 0);

            for (int i = 0; i < count; i++)
            {
                var itemSrc = src?.Items[i];
                var itemDest = dest?.Items[i];
                if (itemSrc != null && itemDest != null)
                {
                    if (src?.ItemContainerGenerator.ContainerFromItem(itemSrc) is FrameworkElement containerSrc 
                        && dest?.ItemContainerGenerator.ContainerFromItem(itemDest) is FrameworkElement containerDest)
                    {
                        void UpdateHeight()
                        {
                            containerDest.Height = containerSrc.ActualHeight;
                        }

                        containerSrc.LayoutUpdated += (s, e) => UpdateHeight();
                        UpdateHeight(); // 初回反映
                    }
                }
            }
        }

        public ItemsControl TargetItemsControl
        {
            get => (ItemsControl)GetValue(TargetItemsControlProperty);
            set => SetValue(TargetItemsControlProperty, value);
        }

        public static readonly DependencyProperty TargetItemsControlProperty =
            DependencyProperty.Register(nameof(TargetItemsControl), typeof(ItemsControl), typeof(ItemsControlHeightSyncBehavior), new PropertyMetadata(null, OnTargetChanged));

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControlHeightSyncBehavior behavior)
            {
                behavior.SyncHeightsIfPossible();
            }
        }
    }
}
