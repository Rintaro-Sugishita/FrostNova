namespace FrostNova.Core.Wpf.Behaviors
{
    /// <summary>
    /// <see cref="WindowLocationMemoryBehavior"/> がウィンドウのサイズ・位置を
    /// JSON で保存/復元するためのデータ。値が無い場合は復元しない。
    /// </summary>
    public class WindowLocationData
    {
        public double? Left { get; set; }
        public double? Top { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
    }
}
