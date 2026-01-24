using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace FrostNova.Core.Wpf.Behaviors
{

    /// <summary>
    /// Provides a behavior that enables enhanced mouse wheel and horizontal scroll gesture support for a <see
    /// cref="ScrollViewer"/>.
    /// </summary>
    /// <remarks>Attach this behavior to a <see cref="ScrollViewer"/> to enable handling of both vertical and
    /// horizontal mouse wheel gestures, including support for horizontal scrolling via Shift+Mouse Wheel or dedicated
    /// horizontal wheel devices. This behavior is intended for use in WPF applications and should not be used in design
    /// mode.</remarks>
    public class ScrollGestureBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (isDesignMode == false)
            {

                if (AssociatedObject.IsLoaded)
                {
                    SetHook();
                }
                else
                {
                    AssociatedObject.Loaded += OnLoaded;

                }
                AssociatedObject.Unloaded += OnUnloaded;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource = null;
            }
        }



        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource = null;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetHook();
        }


        private void SetHook()
        {
            hwndSource = (HwndSource)PresentationSource.FromVisual(Window.GetWindow(AssociatedObject));
            hwndSource?.AddHook(WndProc);
        }



        private HwndSource? hwndSource;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int WM_MOUSEWHEEL = 0x020A;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEHWHEEL)
            {
                try
                {
                    if (GeneralHelper.IsInMousePoint(AssociatedObject) == false) return IntPtr.Zero;

                    int delta = (short)((((long)wParam) >> 16) & 0xFFFF);
                    AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset + delta);
                    handled = true;
                }
                catch { return IntPtr.Zero; }

            }
            else if (msg == WM_MOUSEWHEEL)
            {
                try
                {

                    if (GeneralHelper.IsInMousePoint(AssociatedObject) == false) return IntPtr.Zero;

                    int delta = (short)((((long)wParam) >> 16) & 0xFFFF);

                    if (Keyboard.Modifiers == ModifierKeys.Shift || delta == 0)
                    {
                        // 横スクロール扱い
                        AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset - delta);
                    }
                    else
                    {
                        // 任意の係数をかけてスクロール速度を調整
                        double adjusted = delta * 0.3;
                        AssociatedObject.ScrollToVerticalOffset(AssociatedObject.VerticalOffset - adjusted);

                    }
                    handled = true;
                }
                catch { return IntPtr.Zero; }
            }

            return IntPtr.Zero;
        }


    }
}
