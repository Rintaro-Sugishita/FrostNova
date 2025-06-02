using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class WindowClosingBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Closing += OnWindowClosing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Closing -= OnWindowClosing;

        }


        //public ICommand Command 
        //{
        //    get => (ICommand)GetValue(CommandProperty); 
        //    set => SetValue(CommandProperty, value);
        //}

        //public static DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(WindowClosingBehavior));

        public bool CanClose
        {
            get => (bool)GetValue(CanCloseProperty);
            set => SetValue(CanCloseProperty, value);
        }

        private static DependencyProperty CanCloseProperty = DependencyProperty.Register(nameof(CanClose), typeof(bool), typeof(WindowClosingBehavior), new PropertyMetadata(false));


        private void OnWindowClosing(object? sender , CancelEventArgs e)
        {
            if (!CanClose)
            {
                e.Cancel = true;
                if (sender is Window window)
                {
                    window.WindowState = WindowState.Minimized;
                }
            }
        }

    }
}
