using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class WindowDragMoveBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseDoubleClick += AssociatedObject_MouseDoubleClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;

            AssociatedObject.MouseDoubleClick -= AssociatedObject_MouseDoubleClick;
           
        }

        public bool IsCloseDoubleClick
        {
            get { return (bool)GetValue(IsCloseDoubleClickProperty); }
            set { SetValue(IsCloseDoubleClickProperty, value); }
        }
        public static readonly DependencyProperty IsCloseDoubleClickProperty = DependencyProperty.Register(nameof(IsCloseDoubleClickProperty), typeof(bool), typeof(WindowDragMoveBehavior), new PropertyMetadata(true));



        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Window w)
            {
                w.DragMove();
            }
        }
        private void AssociatedObject_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsCloseDoubleClick && sender is Window w)
            {
                w.Close();
            }
        }
    }
}
