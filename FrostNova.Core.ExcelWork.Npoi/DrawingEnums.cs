namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A practical subset of preset auto-shapes supported by the NPOI backend.
    /// (The EPPlus backend exposed the full eShapeStyle set; NPOI maps these to OOXML preset geometries.)
    /// </summary>
    public enum ShapeStyle
    {
        Rectangle,
        RoundRectangle,
        Ellipse,
        Triangle,
        RightTriangle,
        Diamond,
        Parallelogram,
        Trapezoid,
        Hexagon,
        Octagon,
        Plus,
        Pentagon,
        Chevron,
        Star5,
        RightArrow,
        LeftArrow,
        UpArrow,
        DownArrow,
        Heart,
        Cloud,
        Can,
        Cube,
    }

    /// <summary>Chart kinds supported by the NPOI backend.</summary>
    public enum ChartType
    {
        Line,
        Bar,
        Column,
        Scatter,
    }
}
