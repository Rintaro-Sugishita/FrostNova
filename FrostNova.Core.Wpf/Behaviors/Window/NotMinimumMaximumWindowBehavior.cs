using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FrostNova.Core.Wpf.Behaviors
{
    internal class NotMinimumMaximumWindowBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SourceInitialized += AssociatedObject_SourceInitialized;        
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SourceInitialized -= AssociatedObject_SourceInitialized;
        }

        private void AssociatedObject_SourceInitialized(object? sender, EventArgs e)
        {
            var w = sender as Window;

            nint handle = new WindowInteropHelper(w).Handle;
            int style = GetWindowLong(handle, GWL_STYLE);
            style = style & ~WS_SYSMENU;
            SetWindowLong(handle, GWL_STYLE, style);

        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x80000;



    }
}
