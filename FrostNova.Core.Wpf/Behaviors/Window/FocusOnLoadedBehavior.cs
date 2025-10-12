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
    internal class FocusOnLoadedBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // ItemsPresenter がロードされるまで遅延実行
            AssociatedObject.Dispatcher.BeginInvoke(() =>
            {
                //var container = AssociatedObject.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                //if (container == null) return;

                // Button などのフォーカス可能な子を探す
                var focusable = FindFocusableChild(AssociatedObject);
                focusable?.Focus();
            });
        }

        private FrameworkElement? FindFocusableChild(DependencyObject parent)
        {
            // 再帰的にフォーカス可能な要素を探す
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe)
                {
                    if (fe.Focusable && fe.IsEnabled && fe.Visibility == Visibility.Visible)
                        return fe;

                    var found = FindFocusableChild(child);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
    }
}
