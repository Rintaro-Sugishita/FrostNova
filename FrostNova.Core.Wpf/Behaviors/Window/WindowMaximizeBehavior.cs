using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace FrostNova.Core.Wpf.Behaviors;

public class WindowMaximizeBehavior : Behavior<System.Windows.Window>
{
    private WindowState lastState = WindowState.Maximized;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += AssociatedObject_KeyDown;
    }

    private void AssociatedObject_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {

        if (e.Key == System.Windows.Input.Key.F11)
        {
            if (AssociatedObject.WindowStyle == WindowStyle.None)
            {
                AssociatedObject.WindowStyle = WindowStyle.SingleBorderWindow;
                AssociatedObject.WindowState = lastState;

            }
            else
            {
                lastState = AssociatedObject.WindowState;
                AssociatedObject.WindowStyle = WindowStyle.None;
                AssociatedObject.WindowState = WindowState.Maximized;
            }

        }


    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.KeyDown -= AssociatedObject_KeyDown;

    }



}
