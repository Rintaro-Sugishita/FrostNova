using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class CanvasVerticalEx
    {

        public static readonly DependencyProperty CanvasVerticalAlignmentProperty =
            DependencyProperty.RegisterAttached(
                "CanvasVerticalAlignment",
                typeof(VerticalAlignment),
                typeof(CanvasVerticalEx),
                new PropertyMetadata(VerticalAlignment.Top, OnCanvasVerticalAlignmentChanged));

        public static VerticalAlignment GetCanvasVerticalAlignment(UIElement element) =>
            (VerticalAlignment)element.GetValue(CanvasVerticalAlignmentProperty);

        public static void SetCanvasVerticalAlignment(UIElement element, VerticalAlignment value) =>
            element.SetValue(CanvasVerticalAlignmentProperty, value);

        private static void OnCanvasVerticalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && fe.Parent is Canvas canvas)
            {
                fe.Loaded += (_, __) => UpdateTop(fe, canvas);
                fe.SizeChanged += (_, __) => UpdateTop(fe, canvas);
            }
        }

        private static void UpdateTop(FrameworkElement element, Canvas canvas)
        {
            var alignment = GetCanvasVerticalAlignment(element);
            var height = element.ActualHeight;
            var canvasHeight = canvas.ActualHeight;

            double top = 0;
            switch (alignment)
            {
                case VerticalAlignment.Top:
                    top = 0;
                    break;
                case VerticalAlignment.Center:
                    top = (canvasHeight - height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    top = canvasHeight - height;
                    break;
                case VerticalAlignment.Stretch:
                    // ストレッチ時は上寄せで高さをCanvasに合わせる
                    top = 0;
                    element.Height = canvasHeight;
                    break;
            }

            Canvas.SetTop(element, top);
        }
    }
}
