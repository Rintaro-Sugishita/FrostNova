using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class ScrollViewerTopLeftItemBehavior : Behavior<ScrollViewer>
    {
        public static readonly DependencyProperty TargetItemsControlProperty =
          DependencyProperty.Register(nameof(TargetItemsControl), typeof(ItemsControl), typeof(ScrollViewerTopLeftItemBehavior));

        public ItemsControl TargetItemsControl
        {
            get => (ItemsControl)GetValue(TargetItemsControlProperty);
            set => SetValue(TargetItemsControlProperty, value);
        }

        public static readonly DependencyProperty TopVisibleItemProperty =
            DependencyProperty.Register(nameof(TopVisibleItem), typeof(object), typeof(ScrollViewerTopLeftItemBehavior));

        public object TopVisibleItem
        {
            get => GetValue(TopVisibleItemProperty);
            set => SetValue(TopVisibleItemProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ScrollChanged += OnScrollChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ScrollChanged -= OnScrollChanged;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (TargetItemsControl == null)
                return;

            var viewer = AssociatedObject;
            var point = new Point(1, 1); // 可視領域の左上近く
            var result = VisualTreeHelper.HitTest(viewer, point);

            if (result == null)
                return;

            // VisualTreeを上に辿って ItemsControl の子要素（ListBoxItemなど）を探す
            DependencyObject current = result.VisualHit;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext != null &&
                    TargetItemsControl.ItemContainerGenerator.IndexFromContainer(current) >= 0)
                {
                    TopVisibleItem = fe.DataContext;
                    return;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            TopVisibleItem = null;
        }
    }
}
