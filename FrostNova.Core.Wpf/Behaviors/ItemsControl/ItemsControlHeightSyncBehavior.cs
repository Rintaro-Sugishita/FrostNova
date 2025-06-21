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
            AttachHandlers();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            DetachHandlers();
        }

        private static void OnTargetItemsControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControlHeightSyncBehavior behavior)
            {
                behavior.DetachHandlers();
                behavior.AttachHandlers();
                behavior.SynchronizeHeights();
            }
        }

        private void AttachHandlers()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
                _sourceCollection = AssociatedObject.ItemsSource as INotifyCollectionChanged;
                if (_sourceCollection != null)
                    _sourceCollection.CollectionChanged += Source_CollectionChanged;
            }

            if (TargetItemsControl != null)
            {
                TargetItemsControl.ItemContainerGenerator.StatusChanged += Target_ItemContainerGenerator_StatusChanged;
                _targetCollection = TargetItemsControl.ItemsSource as INotifyCollectionChanged;
                if (_targetCollection != null)
                    _targetCollection.CollectionChanged += Target_CollectionChanged;
            }
        }

        private void DetachHandlers()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                if (_sourceCollection != null)
                    _sourceCollection.CollectionChanged -= Source_CollectionChanged;
                _sourceCollection = null;
            }

            if (TargetItemsControl != null)
            {
                TargetItemsControl.ItemContainerGenerator.StatusChanged -= Target_ItemContainerGenerator_StatusChanged;
                if (_targetCollection != null)
                    _targetCollection.CollectionChanged -= Target_CollectionChanged;
                _targetCollection = null;
            }
        }

        private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                SynchronizeHeights();
            }
        }

        private void Target_ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (TargetItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                SynchronizeHeights();
            }
        }

        private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeHeights();
        }

        private void Target_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeHeights();
        }

        private void SynchronizeHeights()
        {
            if (AssociatedObject == null || TargetItemsControl == null)
                return;

            int count = Math.Min(AssociatedObject.Items.Count, TargetItemsControl.Items.Count);

            for (int i = 0; i < count; i++)
            {
                var container = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                var targetContainer = TargetItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

                if (container != null && targetContainer != null)
                {
                    double maxHeight = Math.Max(container.ActualHeight, targetContainer.ActualHeight);
                    if (maxHeight > 0)
                    {
                        container.Height = maxHeight;
                        targetContainer.Height = maxHeight;
                    }
                }
            }
        }
    }
}
