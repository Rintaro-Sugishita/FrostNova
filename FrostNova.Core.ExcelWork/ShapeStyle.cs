using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public enum ShapeStyle
    {

        /// <summary>
        /// Callout: with border and accent bar
        /// </summary>
        AccentBorderCallout1,
        /// <summary>
        /// Callout: with bent line and accent bar
        /// </summary>

        AccentBorderCallout2,
        /// <summary>
        /// Callout: with double bent line and accent bar
        /// </summary>
        AccentBorderCallout3,

        /// <summary>
        /// Callout: with line
        /// </summary>
        AccentCallout1,

        /// <summary>
        /// Callout: with bent line
        /// </summary>
        AccentCallout2,

        /// <summary>
        /// Callout: with double bent line
        /// </summary>
        AccentCallout3,

        /// <summary>
        /// Action button: Back &lt;
        /// </summary>
        ActionButtonBackPrevious,

        /// <summary>
        /// Action button: Beginning |&lt;
        /// </summary>
        ActionButtonBeginning,
        /// <summary>
        /// Action button: Blank
        /// </summary>
        ActionButtonBlank,

        /// <summary>
        /// Action button: with document icon
        /// </summary>
        ActionButtonDocument,

        /// <summary>
        /// Action button: End &gt;|
        /// </summary>
        ActionButtonEnd,

        /// <summary>
        /// Action button: Next &gt;
        /// </summary>
        ActionButtonForwardNext,

        /// <summary>
        /// Action button: Help ?
        /// </summary>
        ActionButtonHelp,

        /// <summary>
        /// Action button: Home icon
        /// </summary>
        ActionButtonHome,

        /// <summary>
        /// Action button: Information 🛈
        /// </summary>
        ActionButtonInformation,

        /// <summary>
        /// Action button: Camera icon
        /// </summary>
        ActionButtonMovie,

        /// <summary>
        /// Action button: U-turn icon.
        /// </summary>
        ActionButtonReturn,

        /// <summary>
        /// Action button: Speaker icon
        /// </summary>
        ActionButtonSound,

        /// <summary>
        /// Arc: Quarter circle
        /// </summary>
        Arc,

        /// <summary>
        /// A bent arrow
        /// </summary>
        BentArrow,

        /// <summary>
        /// Bent connector 2
        /// </summary>
        BentConnector2,

        /// <summary>
        /// Bent connector 3
        /// </summary>
        BentConnector3,

        /// <summary>
        /// Bent connector 4
        /// </summary>
        BentConnector4,

        /// <summary>
        /// Bent connector 5
        /// </summary>
        BentConnector5,

        /// <summary>
        /// A bent up arrow
        /// </summary>
        BentUpArrow,

        /// <summary>
        /// Bevel
        /// </summary>
        Bevel,

        /// <summary>
        /// Block arc: Half circle
        /// </summary>
        BlockArc,



















        /// <summary>
        /// Callout: Line
        /// </summary>
        BorderCallout1,

        /// <summary>
        /// Callout: Bent line with border
        /// </summary>
        BorderCallout2,

        /// <summary>
        /// Callout: Double bent line with border
        /// </summary>
        BorderCallout3,

        /// <summary>
        /// Brace pair: { }
        /// </summary>
        BracePair,

        /// <summary>
        /// Bracket pair: ( )
        /// </summary>
        BracketPair,

        /// <summary>
        /// Callout: Line
        /// </summary>
        Callout1,

        /// <summary>
        /// Callout: Bent line
        /// </summary>
        Callout2,

        /// <summary>
        /// Callout: Double bent line
        /// </summary>
        Callout3,

        /// <summary>
        /// Can: A cylinder shape
        /// </summary>
        Can,

        /// <summary>
        /// A plus within a rectangle
        /// </summary>
        ChartPlus,

        /// <summary>
        /// A star within a rectangle
        /// </summary>
        ChartStar,

        /// <summary>
        /// An X within a rectangle
        /// </summary>
        ChartX,

        /// <summary>
        /// Chevron: &gt;
        /// </summary>
        Chevron,

        /// <summary>
        /// Chord: A quarter of a circle
        /// </summary>
        Chord,

        /// <summary>
        /// A circular arrow
        /// </summary>
        CircularArrow,

        /// <summary>
        /// A cloud
        /// </summary>
        Cloud,

        /// <summary>
        /// Callout: Cloud
        /// </summary>
        CloudCallout,

        /// <summary>
        /// Corner: L shape
        /// </summary>
        Corner,

        /// <summary>
        /// Corner Tabs: Triangles in the corners
        /// </summary>
        CornerTabs,

        /// <summary>
        /// A 3D cube shape
        /// </summary>
        Cube,

        /// <summary>
        /// Curved Connector 2
        /// </summary>
        CurvedConnector2,

        /// <summary>
        /// Curved Connector 3
        /// </summary>
        CurvedConnector3,

        /// <summary>
        /// Curved Connector 4
        /// </summary>
        CurvedConnector4,

        /// <summary>
        /// Curved Connector 5
        /// </summary>
        CurvedConnector5,

        /// <summary>
        /// Curved Arrow: Down
        /// </summary>
        CurvedDownArrow,

        /// <summary>
        /// Curved Arrow: Left
        /// </summary>
        CurvedLeftArrow,

        /// <summary>
        /// Curved Arrow: Right
        /// </summary>
        CurvedRightArrow,

        /// <summary>
        /// Curved Arrow: Up
        /// </summary>
        CurvedUpArrow,






        /// <summary>
        /// A decagon: 10 corners
        /// </summary>
        Decagon,

        /// <summary>
        /// A diagonal stripe
        /// </summary>
        DiagStripe,

        /// <summary>
        /// A diamond shape
        /// </summary>
        Diamond,

        /// <summary>
        /// A dodecagon: 12 corners
        /// </summary>
        Dodecagon,

        /// <summary>
        /// A donut shape
        /// </summary>
        Donut,

        /// <summary>
        /// Double wave
        /// </summary>
        DoubleWave,

        /// <summary>
        /// A down arrow
        /// </summary>
        DownArrow,

        /// <summary>
        /// Callout: Down arrow
        /// </summary>
        DownArrowCallout,

        /// <summary>
        /// An ellipse
        /// </summary>
        Ellipse,

        /// <summary>
        /// Ellipse ribbon: Points up
        /// </summary>
        EllipseRibbon,

        /// <summary>
        /// Ellipse ribbon: Points down
        /// </summary>
        EllipseRibbon2,

        /// <summary>
        /// Flowchart: Alternate Process
        /// </summary>
        FlowChartAlternateProcess,

        /// <summary>
        /// Flowchart: Collate
        /// </summary>
        FlowChartCollate,

        /// <summary>
        /// Flowchart: Connector
        /// </summary>
        FlowChartConnector,

        /// <summary>
        /// Flowchart: Decision
        /// </summary>
        FlowChartDecision,

        /// <summary>
        /// Flowchart: Delay
        /// </summary>
        FlowChartDelay,

        /// <summary>
        /// Flowchart: Display
        /// </summary>
        FlowChartDisplay,

        /// <summary>
        /// Flowchart: Document
        /// </summary>
        FlowChartDocument,

        /// <summary>
        /// Flowchart: Extract
        /// </summary>
        FlowChartExtract,

        /// <summary>
        /// Flowchart: Input/Output Data
        /// </summary>
        FlowChartInputOutput,

        /// <summary>
        /// Flowchart: Internal Storage
        /// </summary>
        FlowChartInternalStorage,

        /// <summary>
        /// Flowchart: Magnetic Disk
        /// </summary>
        FlowChartMagneticDisk,

        /// <summary>
        /// Flowchart: Magnetic Drum
        /// </summary>
        FlowChartMagneticDrum,

        /// <summary>
        /// Flowchart: Magnetic Tape
        /// </summary>
        FlowChartMagneticTape,

        /// <summary>
        /// Flowchart: Manual Input
        /// </summary>
        FlowChartManualInput,

        /// <summary>
        /// Flowchart: Manual Operation
        /// </summary>
        FlowChartManualOperation,

        /// <summary>
        /// Flowchart: Merge
        /// </summary>
        FlowChartMerge,

        /// <summary>
        /// Flowchart: Multidocument
        /// </summary>
        FlowChartMultidocument,

        /// <summary>
        /// Flowchart: Offline Storage
        /// </summary>
        FlowChartOfflineStorage,

        /// <summary>
        /// Flowchart: Off-page Connector
        /// </summary>
        FlowChartOffpageConnector,

        /// <summary>
        /// Flowchart: Online Storage
        /// </summary>
        FlowChartOnlineStorage,

        /// <summary>
        /// Flowchart: Or
        /// </summary>
        FlowChartOr,

        /// <summary>
        /// Flowchart: Predefined Process
        /// </summary>
        FlowChartPredefinedProcess,

        /// <summary>
        /// Flowchart: Preparation
        /// </summary>
        FlowChartPreparation,

        /// <summary>
        /// Flowchart: Process
        /// </summary>
        FlowChartProcess,

        /// <summary>
        /// Flowchart: Punched Card
        /// </summary>
        FlowChartPunchedCard,

        /// <summary>
        /// Flowchart: Punched Tape
        /// </summary>
        FlowChartPunchedTape,

        /// <summary>
        /// Flowchart: Sort
        /// </summary>
        FlowChartSort,

        /// <summary>
        /// Flowchart: Summing Junction
        /// </summary>
        FlowChartSummingJunction,





        /// <summary>     Flow chart: Terminator</summary>
        FlowChartTerminator,


        /// <summary>     Folded corner, right bottom</summary>
        FoldedCorner,


        /// <summary>     A frame</summary>
        Frame,


        /// <summary>     A Funnel</summary>
        Funnel,


        /// <summary>     Gear, six cogs</summary>
        Gear6,


        /// <summary>     Gear, nine cogs</summary>
        Gear9,


        /// <summary>     Half frame.</summary>
        HalfFrame,


        /// <summary>     A heart</summary>
        Heart,


        /// <summary>     A Heptagon, 7 corners</summary>
        Heptagon,


        /// <summary>     A Hexagon, 6 corners</summary>
        Hexagon,


        /// <summary>     Home plate</summary>
        HomePlate,


        /// <summary>     A horizontal scroll</summary>
        HorizontalScroll,


        /// <summary>     Explosion 12</summary>
        IrregularSeal1,


        /// <summary>     Explosion 14</summary>
        IrregularSeal2,


        /// <summary>     Left arrow</summary>
        LeftArrow,


        /// <summary>     Callout: Left arrow</summary>
        LeftArrowCallout,


        /// <summary>     Left brace: {</summary>
        LeftBrace,


        /// <summary>     Left bracket: (</summary>
        LeftBracket,


        /// <summary>     Left circular arrow</summary>
        LeftCircularArrow,


        /// <summary>     Left Right arrow</summary>
        LeftRightArrow,


        /// <summary>     Callout: Left rigth arrow</summary>
        LeftRightArrowCallout,


        /// <summary>     Left right circular arrow</summary>
        LeftRightCircularArrow,


        /// <summary>     Left right ribbon</summary>
        LeftRightRibbon,


        /// <summary>     Left right up arrow</summary>
        LeftRightUpArrow,


        /// <summary>     Left up arrow</summary>
        LeftUpArrow,


        /// <summary>     Lightning bold</summary>
        LightningBolt,


        /// <summary>     A line, from top-left to bottom-right.</summary>
        Line,


        /// <summary>     An inverted line, from top-right to bottom-left.</summary>
        LineInv,


        /// <summary>     Math: Divide ÷</summary>
        MathDivide,


        /// <summary>     Math: Equal =</summary>
        MathEqual,


        /// <summary>     Math: Minus -</summary>
        MathMinus,


        /// <summary>     Math: Multiply x</summary>
        MathMultiply,


        /// <summary>     Math: Not equal ≠</summary>
        MathNotEqual,


        /// <summary>     Math: Plus +</summary>
        MathPlus,


        /// <summary>     Half moon</summary>
        Moon,


        /// <summary>     Non Isosceles Trapezoid</summary>
        NonIsoscelesTrapezoid,


        /// <summary>     No smoking, circle with line</summary>
        NoSmoking,


        /// <summary>     Notched Right Arrow</summary>
        NotchedRightArrow,


        /// <summary>     Octagon, 8 corners</summary>
        Octagon,


        /// <summary>     Parallelogram</summary>
        Parallelogram,


        /// <summary>     Pentagon, 5 corners</summary>
        Pentagon,


        /// <summary>     Pie</summary>
        Pie,


        /// <summary>     Pie wedge</summary>
        PieWedge,


        /// <summary>     Plaque</summary>
        Plaque,


        /// <summary>     PlaqueTabs, inverted Plaque</summary>
        PlaqueTabs,


        /// <summary>     A plus</summary>
        Plus,


        /// <summary>     Quad Arrow</summary>
        QuadArrow,


        /// <summary>     Callout: Quad Arrow</summary>
        QuadArrowCallout,


        /// <summary>     A rectangle</summary>
        Rect,


        /// <summary>     A ribbon - up</summary>
        Ribbon,


        /// <summary>     Ribbon - down</summary>
        Ribbon2,


        /// <summary>     Right arrow</summary>
        RightArrow,


        /// <summary>     Callout: Right arrow</summary>
        RightArrowCallout,


        /// <summary>     Right Brace }</summary>
        RightBrace,


        /// <summary>     Right bracket )</summary>
        RightBracket,


        /// <summary>     Rectangle - rounded top-right</summary>
        Round1Rect,


        /// <summary>     Rectangle - Round top-left and bottom-right</summary>
        Round2DiagRect,


        /// <summary>     Rectangle - Round top corners</summary>
        Round2SameRect,


        /// <summary>     Rectangle with rounded corners</summary>
        RoundRect,


        /// <summary>     Right triangle</summary>
        RtTriangle,


        /// <summary>     Smiley face</summary>
        SmileyFace,


        /// <summary>     Rectangle, snipped top-right</summary>
        Snip1Rect,


        /// <summary>     Rectangle, snipped top-right bottom-left</summary>
        Snip2DiagRect,


        /// <summary>     Rectangle, snipped top</summary>
        Snip2SameRect,


        /// <summary>     Rectangle, snipped top-left, rounded top-right</summary>
        SnipRoundRect,


        /// <summary>     Square, tabs</summary>
        SquareTabs,


        /// <summary>     Star, 10</summary>
        Star10,


        /// <summary>     Star, 12</summary>
        Star12,


        /// <summary>     Star, 16</summary>
        Star16,


        /// <summary>     Star, 24</summary>
        Star24,


        /// <summary>     Star, 32</summary>
        Star32,


        /// <summary>     Star, 4</summary>
        Star4,


        /// <summary>     Star, 5</summary>
        Star5,


        /// <summary>     Star, 6</summary>
        Star6,


        /// <summary>     Star, 7</summary>
        Star7,


        /// <summary>     Star, 8</summary>
        Star8,


        /// <summary>     Streight connector</summary>
        StraightConnector1,


        /// <summary>     Striped right arrow</summary>
        StripedRightArrow,


        /// <summary>     Sun</summary>
        Sun,


        /// <summary>     Swoosh arrow</summary>
        SwooshArrow,


        /// <summary>     A tear drop</summary>
        Teardrop,


        /// <summary>     Trapezoid</summary>
        Trapezoid,


        /// <summary>     Triangle</summary>
        Triangle,


        /// <summary>     Up Arrow</summary>
        UpArrow,


        /// <summary>     Callout: Up arrow</summary>
        UpArrowCallout,


        /// <summary>     Up-down arrow</summary>
        UpDownArrow,


        /// <summary>     Callout: Up-down arrow</summary>
        UpDownArrowCallout,


        /// <summary>     U-turn arrow</summary>
        UturnArrow,


        /// <summary>     A wave</summary>
        Wave,


        /// <summary>     Callout Wedge: Ellipse</summary>
        WedgeEllipseCallout,


        /// <summary>     Callout Wedge: Rectangle</summary>
        WedgeRectCallout,


        /// <summary>     Callout Wedge: Rounded rectangle</summary>
        WedgeRoundRectCallout,


        /// <summary>     Vertical scroll</summary>
        VerticalScroll

    }
}
