using Microsoft.VisualBasic;
using Microsoft.Xaml.Behaviors;
using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;


namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridColumnKey
    {

        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key", typeof(string), typeof(DataGridColumnKey), new FrameworkPropertyMetadata(string.Empty));

        public static string GetKey(DependencyObject target)
        {
            return (string)target.GetValue(KeyProperty);
        }

        public static void SetKey(DependencyObject target, string value)
        {
            target.SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty CanSaveWidthProperty =
            DependencyProperty.RegisterAttached("CanSaveWidth", typeof(bool), typeof(DataGridColumnKey), new FrameworkPropertyMetadata(true));


        public static bool GetCanSaveWidth(DependencyObject target)
        {
            return (bool)target.GetValue(CanSaveWidthProperty);
        }

        public static void SetCanSaveWidth(DependencyObject target, bool value)
        {
            target.SetValue(CanSaveWidthProperty, value);
        }


        public static readonly DependencyProperty UseSyncProperty =
            DependencyProperty.RegisterAttached("UseSync", typeof(bool), typeof(DataGridColumnKey), new FrameworkPropertyMetadata(true));

        public static bool GetUseSync(DependencyObject target)
        {
            return (bool)target.GetValue(UseSyncProperty);
        }

        public static void SetUseSync(DependencyObject target, bool value)
        {
            target.SetValue (UseSyncProperty, value);
        }

    }
}
