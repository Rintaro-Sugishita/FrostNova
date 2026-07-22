using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NPOI.XDDF.UserModel;
using NPOI.XDDF.UserModel.Chart;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A chart drawing backed by NPOI's XSSFChart (XDDF-based since NPOI 2.8.0).
    /// Supported kinds: line, bar/column, scatter. Configure the kind with <see cref="Configure"/>
    /// (done for you by <c>Sheet.AddChart</c>), then add one or more series with <see cref="AddSeries"/>.
    /// </summary>
    public class ExcelDrawingChart : ExcelDrawing
    {
        private readonly XSSFChart _chart;
        private readonly ISheet _sheet;
        private ChartType _type = ChartType.Line;
        private XDDFChartAxis? _categoryAxis;
        private XDDFValueAxis? _valueAxis;
        private string _title = "";

        internal ExcelDrawingChart(XSSFChart chart, XSSFClientAnchor anchor, ISheet sheet) : base(anchor)
        {
            _chart = chart;
            _sheet = sheet;
        }

        public string Title
        {
            get => _title;
            set { _title = value; _chart.SetTitleText(value); }
        }

        /// <summary>Set the chart kind and create its axes and legend.</summary>
        public void Configure(ChartType chartType)
        {
            _type = chartType;

            var legend = _chart.GetOrAddLegend();
            legend.Position = LegendPosition.Bottom;

            // Scatter uses a value axis on the bottom; the others use a category axis.
            _categoryAxis = chartType == ChartType.Scatter
                ? _chart.CreateValueAxis(AxisPosition.Bottom)
                : _chart.CreateCategoryAxis(AxisPosition.Bottom);

            _valueAxis = _chart.CreateValueAxis(AxisPosition.Left);
            _valueAxis.Crosses = AxisCrosses.AutoZero;
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

            var xSource = XDDFDataSourcesFactory.FromNumericCellRange((XSSFSheet)_sheet, ToCellRange(xRange));

            var data = _chart.CreateData<double, double>(ToChartTypes(_type), _categoryAxis!, _valueAxis!);
            foreach (var y in yRanges)
            {
                var ySource = XDDFDataSourcesFactory.FromNumericCellRange((XSSFSheet)_sheet, ToCellRange(y));
                data.AddSeries(xSource, ySource);
            }
            _chart.Plot(data);
        }

        private static ChartTypes ToChartTypes(ChartType type) => type switch
        {
            ChartType.Bar => ChartTypes.BAR,
            ChartType.Column => ChartTypes.BAR,
            ChartType.Scatter => ChartTypes.SCATTER,
            _ => ChartTypes.LINE,
        };

        private static CellRangeAddress ToCellRange(Range range)
            => new CellRangeAddress(range.Row - 1, range.ToRow - 1, range.Column - 1, range.ToColumn - 1);
    }
}
