using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridColumnHeaderEx
    {

        public static readonly DependencyProperty IsSelectedHeaderProperty =
            DependencyProperty.RegisterAttached(
                "IsSelectedHeader",
                typeof(bool),
                typeof(DataGridColumnHeaderEx),
                new PropertyMetadata(false, OnIsSelectedHeaderChanged));

        public static bool GetIsSelectedHeader(UIElement element) => (bool)element.GetValue(IsSelectedHeaderProperty);

        public static void SetIsSelectedHeader(UIElement element, bool value) => element.SetValue(IsSelectedHeaderProperty, value);

        private static void OnIsSelectedHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {

            }
        }

        public static readonly DependencyProperty IsAllSelectedHeaderProperty =
                    DependencyProperty.RegisterAttached(
                        "IsAllSelectedHeader",
                        typeof(bool),
                        typeof(DataGridColumnHeaderEx),
                        new PropertyMetadata(false, OnIsAllSelectedHeaderChanged));

        public static bool GetIsAllSelectedHeader(UIElement element) => (bool)element.GetValue(IsAllSelectedHeaderProperty);

        public static void SetIsAllSelectedHeader(UIElement element, bool value) => element.SetValue(IsAllSelectedHeaderProperty, value);

        private static void OnIsAllSelectedHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {

            }
        }




    }
}
