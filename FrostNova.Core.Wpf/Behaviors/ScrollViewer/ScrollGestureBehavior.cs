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
    public class ScrollGestureBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (isDesignMode == false)
            {
                AssociatedObject.Loaded += OnLoaded;
                AssociatedObject.Unloaded += OnUnloaded;
            }
        }
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
                // スクリーン座標を取得（WPF方式）
                Point screenPoint = GetMouseScreenPosition();

                // ScrollViewer内のローカル座標に変換
                Point localPoint = AssociatedObject.PointFromScreen(screenPoint);

                if (IsPointInsideAssociatedObject(localPoint))
                {
                    int delta = (short)((((long)wParam) >> 16) & 0xFFFF);
                    AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset + delta);
                    handled = true;
                }
            }
            else if (msg == WM_MOUSEWHEEL)
            {
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

            return IntPtr.Zero;
        }
        private Point GetMouseScreenPosition()
        {
            // Get mouse position relative to the entire screen (using WPF only)
            var position = Mouse.GetPosition(Application.Current.MainWindow);
            return Application.Current.MainWindow.PointToScreen(position);
        }
        private bool IsPointInsideAssociatedObject(Point point)
        {
            return point.X >= 0 && point.Y >= 0 &&
                   point.X < AssociatedObject.ActualWidth &&
                   point.Y < AssociatedObject.ActualHeight;
        }

    }
}
