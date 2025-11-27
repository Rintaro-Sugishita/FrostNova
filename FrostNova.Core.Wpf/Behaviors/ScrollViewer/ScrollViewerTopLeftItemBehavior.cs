using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FrostNova.Core.Wpf.Behaviors
{

    /// <summary>
    /// Represents a behavior that tracks the topmost visible item in a <see cref="ScrollViewer"/>  and updates the <see cref="TopVisibleItem"/> property accordingly.
    /// </summary>
    public class ScrollViewerTopLeftItemBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// Dependency property of the <see cref="ItemsControl"/> that serves as the target for this operation.
        /// </summary>
        public static readonly DependencyProperty TargetItemsControlProperty =
          DependencyProperty.Register(nameof(TargetItemsControl), typeof(ItemsControl), typeof(ScrollViewerTopLeftItemBehavior));

        /// <summary>
        /// Gets or sets the <see cref="ItemsControl"/> that serves as the target for this operation.
        /// </summary>
        public ItemsControl TargetItemsControl
        {
            get => (ItemsControl)GetValue(TargetItemsControlProperty);
            set => SetValue(TargetItemsControlProperty, value);
        }

        /// <summary>
        /// Dependency property of the topmost visible item in the current view.
        /// </summary>
        public static readonly DependencyProperty TopVisibleItemProperty =
            DependencyProperty.Register(nameof(TopVisibleItem), typeof(object), typeof(ScrollViewerTopLeftItemBehavior), new PropertyMetadata(null));



        /// <summary>
        /// Gets or sets the topmost visible item in the current view.
        /// </summary>
        public object? TopVisibleItem
        {
            get => GetValue(TopVisibleItemProperty);
            set => SetValue(TopVisibleItemProperty, value);
        }

        /// <summary>
        /// Gets or sets the bottom visible item in the current view.
        /// </summary>
        public object? BottomVisibleItem
        {
            get { return GetValue(BottomVisibleItemProperty); }
            set { SetValue(BottomVisibleItemProperty, value); }
        }
        /// <summary>
        /// Dependency property of the bottom visible item in the current view.
        /// </summary>
        public static DependencyProperty BottomVisibleItemProperty = DependencyProperty.Register(nameof(BottomVisibleItem), typeof(object), typeof(ScrollViewerTopLeftItemBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the center visible item in the current view.
        /// </summary>
        public object? CenterVisibleItem
        {
            get { return (object?)GetValue(CenterVisibleItemProperty); }
            set { SetValue(CenterVisibleItemProperty, value); }
        }
        /// <summary>
        /// Dependency property of the center visible item in the current view.
        /// </summary>
        public static DependencyProperty CenterVisibleItemProperty = DependencyProperty.Register(nameof(CenterVisibleItem), typeof(object), typeof(ScrollViewerTopLeftItemBehavior), new PropertyMetadata(null));



        /// <summary>
        /// OnAttached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ScrollChanged += OnScrollChanged;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
        }


        /// <summary>
        /// OnDetaching
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ScrollChanged -= OnScrollChanged;
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
        }
        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TargetItemsControl == null)
                return;
            var scrollContent = GetScrollContentPresenter(AssociatedObject);
            //SetTopItem(scrollContent);
            SetCenterItem(scrollContent);
            SetBottomItem(scrollContent);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (TargetItemsControl == null)
                return;

            var scrollContent = GetScrollContentPresenter(AssociatedObject);
            SetTopItem(scrollContent);
            SetCenterItem(scrollContent);
            SetBottomItem(scrollContent);
        }


        private void SetTopItem(FrameworkElement obj)
        {
            var point = new Point(1, 1); // 可視領域の左上近く
            TopVisibleItem = GetItem(obj, point);
        }

        private void SetBottomItem(FrameworkElement obj)
        {
            var point = new Point(obj.ActualWidth, obj.ActualHeight); // 可視領域の右下近く
            BottomVisibleItem = GetItem(obj, point);

        }

        private void SetCenterItem(FrameworkElement obj)
        {
            var point = new Point(obj.ActualWidth / 2.0, obj.ActualHeight / 2.0);
            CenterVisibleItem = GetItem(obj, point);
        }


        private object? GetItem(Visual viewer, Point point)
        {
            var result = VisualTreeHelper.HitTest(viewer, point);
            return GetDataContext(result); 
        }


        private ScrollContentPresenter? GetScrollContentPresenter(DependencyObject root)
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is ScrollContentPresenter scp) return scp;
                var found = GetScrollContentPresenter(child);
                if (found != null) return found;
            }
            return null;
        }
        private object? GetDataContext(HitTestResult? result)
        {
            // VisualTreeを上に辿って ItemsControl の子要素（ListBoxItemなど）を探す
            if (result == null) return null;
            DependencyObject current = result.VisualHit;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext != null &&
                    TargetItemsControl.ItemContainerGenerator.IndexFromContainer(current) >= 0)
                {
                    return fe.DataContext;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            return null;

        }
    }
}
