using FrostNova.Core.TypeToolkit;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class ExcelDrawingChart : ExcelDrawing
    {
        internal ExcelDrawingChart(OfficeOpenXml.Drawing.ExcelDrawing shape) : base(shape)
        {
            
        }

        private OfficeOpenXml.Drawing.Chart.ExcelChart _chart
        {
            get => (OfficeOpenXml.Drawing.Chart.ExcelChart)_drawing;
        }

        public string Title
        {
            get => _chart.Title.Text;
            set
            {
                _chart.Title.Text = value;
            }
        }

        public void AddSeries(Range xRange, IEnumerable<Range> yRanges)
        {
            foreach (var yRange in yRanges)
            {
                _chart.Series.Add(xRange._range, yRange._range);
            }
        }

        public void AddChart(ChartType chartType)
        {
            //_chart.ChartType = OfficeOpenXml.Drawing.Chart.eChartType.XYScatter;

            var type = ConvertHelper.ConvToEnum<OfficeOpenXml.Drawing.Chart.eChartType>(chartType.ToString());
            if (type == null)
            {
                throw new ArgumentException($"Invalid value : {chartType.ToString()}", nameof(chartType));
            }





            _chart.Title.Text = "売上と利益";
            _chart.SetPosition(6, 0, 0, 0);
            _chart.SetSize(800, 400);

            // 棒グラフの系列（積み上げ）
            //_chart.Series.Add(ws.Cells["B2:B4"], ws.Cells["A2:A4"]).Header = "売上A";
            //_chart.Series.Add(ws.Cells["C2:C4"], ws.Cells["A2:A4"]).Header = "売上B";

            //// 折れ線グラフを追加（軸は共有）

            //var lineChart = _chart.PlotArea.ChartTypes.Add(type.Value) as ExcelLineChart;
            //lineChart.Series.Add(ws.Cells["D2:D4"], ws.Cells["A2:A4"]).Header = "利益";


            _chart.Legend.Position = eLegendPosition.Bottom;
            // グラフに凡例を追加
            _chart.Legend.Position = eLegendPosition.Bottom;

        }



    }
}
