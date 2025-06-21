using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;
using System.Net.Mime;
using FrostNova.Core.Wpf.AttachedProperties;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class CanvasHelperBehavior : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        private ContentPresenter? _presenter;

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (FindParent<ContentPresenter>(AssociatedObject) is ContentPresenter presenter && AssociatedObject is UIElement control)
            {
                // ViewModel -> Canvas.Left 初期設定
                _presenter = presenter;

                if (BindLeft != double.NaN)
                {
                    Canvas.SetLeft(_presenter, BindLeft);
                }
                if (BindTop != double.NaN)
                {
                    Canvas.SetTop(_presenter, BindTop);
                }


                // Canvas.Leftが変わったらViewModelに書き戻す処理を仕込む
                AssociatedObject.LayoutUpdated += (s, _) =>
                {
                    double canvasLeft = Canvas.GetLeft(_presenter);
                    double canvasTop = Canvas.GetTop(_presenter);

                    if (BindLeft != double.NaN && canvasLeft != BindLeft)
                    {
                        Canvas.SetLeft(_presenter, BindLeft);
                    }
                    if (BindTop != double.NaN && canvasTop != BindTop)
                    {
                        Canvas.SetTop(_presenter, BindTop);
                    }
                };
            }
        }


        /// <summary>
        /// string
        /// </summary>
        public double BindLeft
        {
            get { return (double)GetValue(BindLeftProperty); }
            set { SetValue(BindLeftProperty, value); }
        }
        public static readonly DependencyProperty BindLeftProperty = DependencyProperty.Register(nameof(BindLeft), typeof(double), typeof(CanvasHelperBehavior), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// string
        /// </summary>
        public double BindTop
        {
            get { return (double)GetValue(BindTopProperty); }
            set { SetValue(BindTopProperty, value); }
        }
        public static readonly DependencyProperty BindTopProperty = DependencyProperty.Register(nameof(BindTop), typeof(double), typeof(CanvasHelperBehavior), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));



        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T parentAsT)
                    return parentAsT;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }


        public VerticalAlignment CanvasVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(CanvasVerticalAlignmentProperty);
            set => SetValue(CanvasVerticalAlignmentProperty, value);
        }

        public static readonly DependencyProperty CanvasVerticalAlignmentProperty =
            DependencyProperty.Register(
                nameof(CanvasVerticalAlignment),
                typeof(VerticalAlignment),
                typeof(CanvasHelperBehavior),
                new PropertyMetadata(VerticalAlignment.Top, OnCanvasVerticalAlignmentChanged));

        private static void OnCanvasVerticalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CanvasHelperBehavior b && b.AssociatedObject is FrameworkElement fe && fe.Parent is Canvas canvas)
            {
                fe.Loaded += (_, __) => UpdateTop(fe, canvas);
                fe.SizeChanged += (_, __) => UpdateTop(fe, canvas);

                UpdateTop(fe, canvas);
            }
        }

        private static void UpdateTop(FrameworkElement element, Canvas canvas)
        {
            var alignment = element.GetValue(CanvasVerticalAlignmentProperty);
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
