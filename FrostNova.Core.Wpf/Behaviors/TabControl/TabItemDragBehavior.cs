using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class TabItemDragBehavior : Behavior<TabControl>
    {
        private TabItem? _draggedTab;
        private Point _dragStartPoint;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.AllowDrop = true;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.Drop -= OnDrop;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _draggedTab = GetParentOfType<TabItem>((DependencyObject)e.OriginalSource);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedTab == null)
                return;

            var position = e.GetPosition(null);
            if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(_draggedTab, _draggedTab.DataContext, DragDropEffects.Move);
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (_draggedTab == null)
                return;

            var droppedData = e.Data.GetData(_draggedTab.DataContext.GetType());
            var targetTab = GetParentOfType<TabItem>((DependencyObject)e.OriginalSource);

            if (targetTab == null || targetTab == _draggedTab)
                return;

            var items = AssociatedObject.ItemsSource as IList;
            if (items == null) return;

            var oldIndex = items.IndexOf(droppedData);
            var newIndex = items.IndexOf(targetTab.DataContext);

            if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
            {
                items.RemoveAt(oldIndex);
                items.Insert(newIndex, droppedData);
            }
        }

        private T? GetParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null && !(element is T))
                element = VisualTreeHelper.GetParent(element);
            return element as T;
        }
    }
}
