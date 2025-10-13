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
        /// OnAttached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ScrollChanged += OnScrollChanged;
        }

        /// <summary>
        /// OnDetaching
        /// </summary>
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
