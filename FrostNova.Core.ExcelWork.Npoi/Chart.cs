using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.SS.UserModel.Charts;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A chart drawing backed by NPOI's XSSFChart. Supported kinds: line, bar/column, scatter.
    /// Configure the kind with <see cref="Configure"/> (done for you by <c>Sheet.AddChart</c>),
    /// then add one or more series with <see cref="AddSeries"/>.
    /// </summary>
    public class ExcelDrawingChart : ExcelDrawing
    {
        private readonly XSSFChart _chart;
        private readonly ISheet _sheet;
        private ChartType _type = ChartType.Line;
        private IChartAxis? _categoryAxis;
        private IChartAxis? _valueAxis;
        private string _title = "";

        internal ExcelDrawingChart(XSSFChart chart, XSSFClientAnchor anchor, ISheet sheet) : base(anchor)
        {
            _chart = chart;
            _sheet = sheet;
        }

        public string Title
        {
            get => _title;
            set { _title = value; _chart.SetTitle(value); }
        }

        /// <summary>Set the chart kind and create its axes and legend.</summary>
        public void Configure(ChartType chartType)
        {
            _type = chartType;

            var legend = _chart.GetOrCreateLegend();
            legend.Position = LegendPosition.Bottom;

            _categoryAxis = chartType == ChartType.Scatter
                ? _chart.ChartAxisFactory.CreateValueAxis(AxisPosition.Bottom)
                : _chart.ChartAxisFactory.CreateCategoryAxis(AxisPosition.Bottom);
            _valueAxis = _chart.ChartAxisFactory.CreateValueAxis(AxisPosition.Left);
        }

        // Kept for API parity with the EPPlus-based wrapper.
        public void AddChart(ChartType chartType) => Configure(chartType);

        /// <summary>Add one or more value series (<paramref name="yRanges"/>) against a shared category/x range.</summary>
        public void AddSeries(Range xRange, IEnumerable<Range> yRanges)
        {
            if (_categoryAxis == null || _valueAxis == null)
            {
                Configure(_type);
            }

            var xSource = DataSources.FromNumericCellRange(_sheet, ToCellRange(xRange));
            var factory = _chart.ChartDataFactory;

            switch (_type)
            {
                case ChartType.Scatter:
                {
                    var data = factory.CreateScatterChartData<double, double>();
                    foreach (var y in yRanges)
                    {
                        data.AddSeries(xSource, DataSources.FromNumericCellRange(_sheet, ToCellRange(y)));
                    }
                    _chart.Plot(data, _categoryAxis, _valueAxis);
                    break;
                }
                case ChartType.Bar:
                case ChartType.Column:
                {
                    var data = factory.CreateBarChartData<double, double>();
                    foreach (var y in yRanges)
                    {
                        data.AddSeries(xSource, DataSources.FromNumericCellRange(_sheet, ToCellRange(y)));
                    }
                    _chart.Plot(data, _categoryAxis, _valueAxis);
                    break;
                }
                case ChartType.Line:
                default:
                {
                    var data = factory.CreateLineChartData<double, double>();
                    foreach (var y in yRanges)
                    {
                        data.AddSeries(xSource, DataSources.FromNumericCellRange(_sheet, ToCellRange(y)));
                    }
                    _chart.Plot(data, _categoryAxis, _valueAxis);
                    break;
                }
            }
        }

        private static CellRangeAddress ToCellRange(Range range)
            => new CellRangeAddress(range.Row - 1, range.ToRow - 1, range.Column - 1, range.ToColumn - 1);
    }
}
