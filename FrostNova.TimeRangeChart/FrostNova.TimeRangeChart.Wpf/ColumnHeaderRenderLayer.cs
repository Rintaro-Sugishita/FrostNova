using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class ColumnHeaderRenderLayer : FrameworkElement
    {
        public GridTimeRangeChart? ParentChart
        {
            get; internal set;
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ParentChart == null) return;

            var borderPen = new Pen(ParentChart.RuledLineFill, ParentChart.RuledLineThickness);
            borderPen.Freeze();

            if (ParentChart.TimeTicks.Count > 0)
            {
                foreach (var tick in ParentChart.TimeTicks)
                {
                    var t = tick as RuledLineData;
                    if (t != null)
                    {
                        DrawHeaderText(drawingContext, t.FormattedText, t.LeftPoint, t.Rect, borderPen);
                    }
                }
            }

        }

        private void DrawHeaderText(DrawingContext dc, FormattedText text, double x, Rect rect, Pen pen)
        {
            var textYPoint = 0.0;
            Rect frame = rect;

            if (Parent is FrameworkElement fe)
            {
                textYPoint = fe.ActualHeight / 2 - text.Height/2;
                frame = new Rect(rect.X, rect.Y, rect.Width, fe.ActualHeight);
            }
            else
            {
                textYPoint = rect.Bottom - text.Height;
            }

            //date text 
            dc.DrawText(text,
                new Point(x + rect.Width / 2 - text.Width / 2, textYPoint)
                );
            //border
            dc.DrawRectangle(Brushes.Transparent, pen, frame);
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            if (ParentChart == null)
            {
                return base.MeasureOverride(availableSize);
            }


            if (Parent is FrameworkElement fe)
            {
                return new Size(ParentChart.ChartWidth, fe.ActualHeight);
            }
            else
            {
                return new Size(ParentChart.ChartWidth, ParentChart.FontSize);
            }

        }

    }
}
