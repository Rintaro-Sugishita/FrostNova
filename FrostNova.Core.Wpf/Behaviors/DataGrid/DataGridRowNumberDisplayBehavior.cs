using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridRowNumberDisplayBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.LoadingRow += AssociatedObject_LoadingRow;
            AssociatedObject.ItemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.LoadingRow -= OnLoadingRow;
            ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= OnCollectionChanged;
            AssociatedObject.ItemContainerGenerator.ItemsChanged -= ItemContainerGenerator_ItemsChanged;
        }

        private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            RefreshRowHeaders();
        }

        private void RefreshRowHeaders()
        {
            foreach (var item in AssociatedObject.Items)
            {
                var row = AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                    row.Header = (row.GetIndex() + 1).ToString();
            }
        }
    }
}
