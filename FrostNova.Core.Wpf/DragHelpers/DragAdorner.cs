using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace FrostNova.Core.Wpf.DragHelpers
{
    public class DragAdorner : Adorner
    {
        private readonly VisualBrush _visualBrush;
        private Point _offset;

        public DragAdorner(UIElement adornedElement, UIElement visual, double opacity = 0.5)
            : base(adornedElement)
        {
            _visualBrush = new VisualBrush(visual)
            {
                Opacity = opacity
            };
            IsHitTestVisible = false;
        }

        public void SetOffset(Point offset)
        {
            _offset = offset;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = ((FrameworkElement)AdornedElement).RenderSize;
            drawingContext.DrawRectangle(_visualBrush, null, new Rect(_offset, size));
        }
    }
}
