using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridDragSource
    {
        public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridDragSource),
            new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }


        public static DependencyProperty DragDataProperty = DependencyProperty.RegisterAttached("DragData", typeof(object), typeof(DataGridDragSource));

        public static object GetDragData(DependencyObject obj)
        {
            return (object)obj.GetValue(DragDataProperty);
        }
        public static void SetDragData(DependencyObject obj, object value)
        {
            obj.SetValue(DragDataProperty, value);
        }
        public static DependencyProperty SourceParameterProperty = DependencyProperty.Register("SourceParameter", typeof(object), typeof(DataGridDragSource));

        public static object GetSourceParameter(DependencyObject obj)
        {
            return (object)obj.GetValue(SourceParameterProperty);
        }
        public static void SetSourceParameter(DependencyObject obj, object value)
        {
            obj.SetValue(SourceParameterProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement textBox && (bool)e.NewValue)
            {
                // TextBoxに対して何かする
                textBox.MouseMove += AssociatedObject_MouseMove;
            }
            else if (d is TextBlock textBlock && (bool)e.NewValue)
            {
                // TextBlockに対して何かする
            }
        }


        internal static void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is UIElement source)
            {

                if (source != null && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    var item = new DragItemData();
                    item.DragData = source.GetValue(DataGridDragSource.DragDataProperty);
                    item.SourceParameter = source.GetValue(DataGridDragSource.SourceParameterProperty);

                    DragDrop.DoDragDrop(source, item, DragDropEffects.Move);
                }
            }
        }

    }
}
