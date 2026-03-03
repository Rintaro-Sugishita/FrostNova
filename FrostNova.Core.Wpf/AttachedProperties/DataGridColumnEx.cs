using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class DataGridColumnEx
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key", typeof(string), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(string.Empty));

        public static string GetKey(DependencyObject target)
        {
            return (string)target.GetValue(KeyProperty);
        }

        public static void SetKey(DependencyObject target, string value)
        {
            target.SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty CanSaveWidthProperty =
            DependencyProperty.RegisterAttached("CanSaveWidth", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(true));


        public static bool GetCanSaveWidth(DependencyObject target)
        {
            return (bool)target.GetValue(CanSaveWidthProperty);
        }

        public static void SetCanSaveWidth(DependencyObject target, bool value)
        {
            target.SetValue(CanSaveWidthProperty, value);
        }


        public static readonly DependencyProperty CanDragRowProperty =
            DependencyProperty.RegisterAttached("CanDragRow", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(true));

        public static bool GetCanDragRow(DependencyObject target)
        {
            return (bool)target.GetValue(CanDragRowProperty);
        }

        public static void SetCanDragRow(DependencyObject target, bool value)
        {
            target.SetValue(CanDragRowProperty, value);
        }

        public static readonly DependencyProperty UseSyncProperty =
            DependencyProperty.RegisterAttached("UseSync", typeof(bool), typeof(DataGridColumnEx), new FrameworkPropertyMetadata(true));

        public static bool GetUseSync(DependencyObject target)
        {
            return (bool)target.GetValue(UseSyncProperty);
        }

        public static void SetUseSync(DependencyObject target, bool value)
        {
            target.SetValue(UseSyncProperty, value);
        }


        public static readonly DependencyProperty IsGeneratedProperty =
             DependencyProperty.RegisterAttached(
                 "IsGenerated",
                 typeof(bool),
                 typeof(DataGridColumnEx),
                 new PropertyMetadata(false, OnIsGeneratedChanged));

        public static bool GetIsGenerated(DependencyObject element) => (bool)element.GetValue(IsGeneratedProperty);

        public static void SetIsGenerated(DependencyObject element, bool value) => element.SetValue(IsGeneratedProperty, value);

        private static void OnIsGeneratedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DependencyObject ctrl)
            {

            }
        }



    }
}
