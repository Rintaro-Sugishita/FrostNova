using FrostNova.Core.TypeToolkit;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class WindowLocationMemoryBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Closed += AssociatedObject_Closed;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.Closed -= AssociatedObject_Closed;
        }


        public WindowLocationMemoryPath SavePath
        {
            get { return (WindowLocationMemoryPath)GetValue(SavePathProperty); }
            set { SetValue(SavePathProperty, value); }
        }
        public static readonly DependencyProperty SavePathProperty = DependencyProperty.Register(nameof(SavePath), typeof(WindowLocationMemoryPath), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(WindowLocationMemoryPath.StartUpDirectory));

        public bool IsKeepSize
        {
            get { return (bool)GetValue(IsKeepSizeProperty); }
            set { SetValue(IsKeepSizeProperty, value); }
        }
        public static readonly DependencyProperty IsKeepSizeProperty = DependencyProperty.Register(nameof(IsKeepSize), typeof(bool), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(true));

        public bool IsKeepLocation
        {
            get { return (bool)GetValue(IsKeepLocationProperty); }
            set { SetValue(IsKeepLocationProperty, value); }
        }
        public static readonly DependencyProperty IsKeepLocationProperty = DependencyProperty.Register(nameof(IsKeepLocation), typeof(bool), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(true));

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            var w = sender as Window;
            if (w == null) { return; }

            var p = GetSavePath();
            if (!System.IO.File.Exists(p)) { return; }

            var lines = System.IO.File.ReadAllLines(p);
            if (lines.Length == 0) { return; }
            if (lines.Length > 1 && IsKeepLocation)
            {
                w.Left = ConvertHelper.ConvToDbl(lines[0]);
                w.Top = ConvertHelper.ConvToDbl(lines[1]);
                if (System.Windows.SystemParameters.VirtualScreenHeight < w.Top || System.Windows.SystemParameters.VirtualScreenWidth < w.Left)
                {
                    w.Top = System.Windows.SystemParameters.VirtualScreenHeight - w.Top;
                    w.Left = System.Windows.SystemParameters.VirtualScreenWidth - w.Left;
                }



            }
            if (lines.Length > 3 && IsKeepSize)
            {
                var width = ConvertHelper.ConvToDblNull(lines[2]);
                if (width != null) w.Width = width.Value;
                var height = ConvertHelper.ConvToDblNull(lines[3]);
                if (height != null) w.Height = height.Value;
            }

        }
        private void AssociatedObject_Closed(object? sender, EventArgs e)
        {
            var w = sender as Window;
            if (w == null) { return; }
            var p = GetSavePath();

            var d = System.IO.Path.GetDirectoryName(p) ?? "";
            if (!System.IO.Directory.Exists(d)) 
            {
                System.IO.Directory.CreateDirectory(d);
            }

            var lines = new List<string>();
            lines.Add(w.Left.ToString());
            lines.Add(w.Top.ToString());
            lines.Add(w.ActualWidth.ToString());
            lines.Add(w.ActualHeight.ToString());
            System.IO.File.WriteAllLines(p, lines);

        }

        private string GetSavePath()
        {
            string basicPath = "";

            if (SavePath == WindowLocationMemoryPath.StartUpDirectory)
            {
                basicPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "";
            }
            else if (SavePath == WindowLocationMemoryPath.UserDirectory)
            {
                basicPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            }

            return System.IO.Path.Combine(basicPath, "windowSizeData", AssociatedObject.GetType().Name + ".dat");

        }

    }
}
