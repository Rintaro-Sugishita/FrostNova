using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DualDataGridSizeSyncBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            //DataGrid1.LoadingRow += DataGrid1_LoadingRow;
            //DataGrid2.LoadingRow += DataGrid2_LoadingRow;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            DataGrid1.LoadingRow -= DataGrid1_LoadingRow;
            DataGrid2.LoadingRow -= DataGrid1_LoadingRow;
        }


        private  void DataGrid1_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            SetColumnWidth();
        }
        private void DataGird1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetColumnWidth();
        }

        public DataGrid DataGrid1
        {
            get { return (DataGrid)GetValue(DataGrid1Property); }
            set { SetValue(DataGrid1Property, value); }
        }
        public static DependencyProperty DataGrid1Property = DependencyProperty.Register(nameof(DataGrid1), typeof(DataGrid), typeof(DualDataGridSizeSyncBehavior), new PropertyMetadata(propertyChangedCallback: propertyChangedCallback));

        private static PropertyChangedCallback propertyChangedCallback = SetDatagrid1;

        public DataGrid DataGrid2
        {
            get { return (DataGrid)GetValue(DataGrid2Property); }
            set { SetValue(DataGrid2Property, value); }
        }
        public static DependencyProperty DataGrid2Property = DependencyProperty.Register(nameof(DataGrid2), typeof(DataGrid), typeof(DualDataGridSizeSyncBehavior), new PropertyMetadata(propertyChangedCallback: propertyChangedCallback));

        private static void SetDatagrid1(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var grid = sender.GetValue(DataGrid1Property) as DataGrid;
            var syncBehavior = sender as DualDataGridSizeSyncBehavior;

            if (grid != null && syncBehavior != null)
            {
                grid.LoadingRow += syncBehavior.DataGrid1_LoadingRow;
                grid.SizeChanged += syncBehavior.DataGird1_SizeChanged;
               

            }

        }


        private void SetColumnWidth()
        {

            //一番大きいカラムはStarにする
            var list = new List<(DataGridColumn, DataGridColumn, double)>();

            var c = Math.Min(DataGrid1.Columns.Count, DataGrid2.Columns.Count);
            if (c == 0) { return; }
            for (int i = 0; i < c; i++)
            {
                string key = DataGridColumnKey.GetKey(DataGrid1.Columns[i]);
                bool can = DataGridColumnKey.GetUseSync(DataGrid1.Columns[i]);

                var dataGrid1Column = DataGrid1.Columns[i];
                var dataGrid2Column = DataGrid2.Columns.FirstOrDefault(x => key == DataGridColumnKey.GetKey(x));
                if (dataGrid2Column == null) { continue; }
                bool can2 = DataGridColumnKey.GetUseSync(dataGrid2Column);
                if (!can && !can2) { continue; }

                var maxColumn1Width = 0.0;
                if (dataGrid1Column.Visibility == Visibility.Visible && dataGrid2Column.Visibility == Visibility.Visible)
                {
                    foreach (var item in DataGrid1.Items)
                    {
                        if (item is DataRowView)
                        {


                            var cellContent = DataGrid1.Columns[i].GetCellContent(item) as TextBlock;
                            if (cellContent != null)
                            {
                                var width = MeasureStringWidth(cellContent.Text, cellContent);
                                if (width > maxColumn1Width)
                                {
                                    maxColumn1Width = width;
                                }
                            }
                        }
                    }

                    foreach (var item in DataGrid2.Items)
                    {
                        if (item is DataRowView)
                        {
                            var cellContent = dataGrid2Column.GetCellContent(item) as TextBlock;
                            if (cellContent != null)
                            {
                                var width = MeasureStringWidth(cellContent.Text, cellContent);
                                if (width > maxColumn1Width)
                                {
                                    maxColumn1Width = width;
                                }
                            }
                        }
                    }
                }


                // 最大幅を各DataGridのColumn1に設定
                //DataGrid1.Columns[i].Width = new DataGridLength(maxColumn1Width, DataGridLengthUnitType.Pixel);
                //dataGrid2Column.Width = new DataGridLength(maxColumn1Width, DataGridLengthUnitType.Pixel);
                list.Add((dataGrid1Column, dataGrid2Column, maxColumn1Width));
            }

            if (list.Count > 0) { return; }

            var maxColumnWidth = list.Select(x => x.Item3).Max();
            if (maxColumnWidth > 0)
            {
                var item = list.First(x=> x.Item3 == maxColumnWidth);
                list.Remove(item);

                item.Item1.Width = DataGridLength.Auto;
                item.Item2.Width = DataGridLength.Auto;
            }

            foreach (var item in list)
            {
                item.Item1.Width = new DataGridLength(item.Item3, DataGridLengthUnitType.Pixel);
                item.Item2.Width = new DataGridLength(item.Item3, DataGridLengthUnitType.Pixel);

            }



        }

        private double MeasureStringWidth(string text, TextBlock textBlock)
        {
            //FormattedText formattedText = new FormattedText(text,CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface());
            //var width = formattedText.Width;


            //var formattedText = new FormattedText(
            //    text,
            //    System.Globalization.CultureInfo.CurrentCulture,
            //    FlowDirection.LeftToRight,
            //    new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            //    textBlock.FontSize,
            //    Brushes.Black,
            //    VisualTreeHelper.GetDpi(new Visual()).PixelsPerDip);

            //return formattedText.WidthIncludingTrailingWhitespace;
            var textBlocka = new TextBlock
            {
                Text = text,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                FontStretch = textBlock.FontStretch,
                FontStyle = textBlock.FontStyle,
                FontWeight = textBlock.FontWeight
            };

            textBlocka.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlocka.DesiredSize.Width;
        }

    }
}
