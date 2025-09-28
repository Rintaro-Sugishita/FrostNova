using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.VisualHelpers
{
    internal class GeneralHelper
    {
        internal static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static void SetBehavior<T>(DependencyObject d, DependencyPropertyChangedEventArgs e) where T : Behavior, new()
        {
            var isEnable =e.NewValue as bool?;
            if (isEnable != null)
            {
                SetBehavior<T>(d, isEnable.Value);
            }
        }

        public static void SetBehavior<T>(DependencyObject d, bool isEnable) where T:  Behavior, new()
        {
            if (d is UIElement element)
            {
                var behaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(element);

                // 既にアタッチ済みか確認
                var existing = behaviors.OfType<T>().FirstOrDefault();

                if (isEnable)
                {
                    if (existing == null)
                        behaviors.Add(new T()); // 任意の Behavior
                }
                else
                {
                    if (existing != null)
                        behaviors.Remove(existing);
                }
            }
        }


    }
}
