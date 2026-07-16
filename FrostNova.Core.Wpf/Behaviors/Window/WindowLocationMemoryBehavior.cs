using FrostNova.Core.JsonData;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Behaviors
{
    /// <summary>
    /// ウィンドウのサイズ・位置を記憶し、次回起動時に復元する Behavior。
    /// 復元は SourceInitialized(最初のレイアウト前)で行う。Loaded で復元すると
    /// レイアウト後になり、特に Width が反映されないことがあるため。
    /// 保存は JSON 形式。
    /// </summary>
    public class WindowLocationMemoryBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SourceInitialized += AssociatedObject_SourceInitialized;
            AssociatedObject.Closed += AssociatedObject_Closed;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SourceInitialized -= AssociatedObject_SourceInitialized;
            AssociatedObject.Closed -= AssociatedObject_Closed;
            base.OnDetaching();
        }

        /// <summary>保存先の基準ディレクトリ種別(SaveDirectory 未指定時に使用)。</summary>
        public WindowLocationMemoryPath SavePath
        {
            get { return (WindowLocationMemoryPath)GetValue(SavePathProperty); }
            set { SetValue(SavePathProperty, value); }
        }
        public static readonly DependencyProperty SavePathProperty = DependencyProperty.Register(nameof(SavePath), typeof(WindowLocationMemoryPath), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(WindowLocationMemoryPath.StartUpDirectory));

        /// <summary>
        /// 保存先ディレクトリを明示指定する(例: アプリのデータフォルダ)。
        /// 指定した場合は SavePath より優先される。
        /// </summary>
        public string? SaveDirectory
        {
            get { return (string?)GetValue(SaveDirectoryProperty); }
            set { SetValue(SaveDirectoryProperty, value); }
        }
        public static readonly DependencyProperty SaveDirectoryProperty = DependencyProperty.Register(nameof(SaveDirectory), typeof(string), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(null));

        public bool IsKeepSize
        {
            get { return (bool)GetValue(IsKeepSizeProperty); }
            set { SetValue(IsKeepSizeProperty, value); }
        }
        public static readonly DependencyProperty IsKeepSizeProperty = DependencyProperty.Register(nameof(IsKeepSize), typeof(bool), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(true));

        public bool IsKeepLocation
        {
            get { return (bool)GetValue(IsKeepLocationProperty); }
            set { SetValue(IsKeepLocationProperty, value); }
        }
        public static readonly DependencyProperty IsKeepLocationProperty = DependencyProperty.Register(nameof(IsKeepLocation), typeof(bool), typeof(WindowLocationMemoryBehavior), new PropertyMetadata(true));

        private void AssociatedObject_SourceInitialized(object? sender, EventArgs e)
        {
            var w = AssociatedObject;
            if (w == null) return;

            var data = LoadData();
            if (data == null) return;

            // サイズを先に適用してから位置を決める(クランプで幅高さを使うため)
            if (IsKeepSize)
            {
                if (data.Width is double width && width > 0) w.Width = width;
                if (data.Height is double height && height > 0) w.Height = height;
            }
            if (IsKeepLocation && data.Left is double left && data.Top is double top)
            {
                double width = double.IsNaN(w.Width) ? w.ActualWidth : w.Width;
                double height = double.IsNaN(w.Height) ? w.ActualHeight : w.Height;

                // 保存位置が現在のモニタ構成で本当に表示できるか検証する。
                // マルチモニタからモニタ数が減った等でウィンドウが画面外に残ると
                // 操作不能になるため、その場合はプライマリスクリーン中央へ復帰させる。
                if (IsBoundsUsable(left, top, width, height))
                {
                    w.Left = left;
                    w.Top = top;
                }
                else
                {
                    CenterOnPrimaryScreen(w, width, height);
                }
            }
        }

        private void AssociatedObject_Closed(object? sender, EventArgs e)
        {
            var w = AssociatedObject;
            if (w == null) return;

            // 最大化/最小化時でも通常状態の矩形が得られる RestoreBounds を優先して保存する
            var b = w.RestoreBounds;
            var data = new WindowLocationData();
            if (!b.IsEmpty)
            {
                data.Left = b.Left;
                data.Top = b.Top;
                data.Width = b.Width;
                data.Height = b.Height;
            }
            else
            {
                data.Left = w.Left;
                data.Top = w.Top;
                data.Width = w.ActualWidth;
                data.Height = w.ActualHeight;
            }

            SaveData(data);
        }

        /// <summary>
        /// 保存された位置が現在のモニタ構成で「操作可能な程度に表示できるか」を判定する。
        /// タイトルバー(上端)が画面の縦範囲内にあり、かつ仮想スクリーンと十分に重なって
        /// いれば true。モニタが取り外された等で画面外に出ている場合は false。
        /// </summary>
        private static bool IsBoundsUsable(double left, double top, double width, double height)
        {
            double vsLeft = SystemParameters.VirtualScreenLeft;
            double vsTop = SystemParameters.VirtualScreenTop;
            double vsWidth = SystemParameters.VirtualScreenWidth;
            double vsHeight = SystemParameters.VirtualScreenHeight;

            if (double.IsNaN(width) || width <= 0) width = 1;
            if (double.IsNaN(height) || height <= 0) height = 1;

            var win = new Rect(left, top, width, height);
            var virt = new Rect(vsLeft, vsTop, vsWidth, vsHeight);
            var inter = Rect.Intersect(win, virt);
            if (inter.IsEmpty) return false;

            const double minVisible = 100;   // これ以上見えていないと掴めない
            const double titleBarHeight = 30; // タイトルバーを掴むのに必要な下側の余地

            // タイトルバー(上端)が画面の縦範囲内にあること
            bool titleBarReachable = top >= vsTop && top <= vsTop + vsHeight - titleBarHeight;

            return titleBarReachable && inter.Width >= minVisible && inter.Height >= minVisible;
        }

        /// <summary>
        /// プライマリスクリーンのワークエリア中央にウィンドウを配置する。
        /// </summary>
        private static void CenterOnPrimaryScreen(Window w, double width, double height)
        {
            var wa = SystemParameters.WorkArea; // プライマリのワークエリア(タスクバー除く)

            if (double.IsNaN(width) || width <= 0) width = wa.Width / 2;
            if (double.IsNaN(height) || height <= 0) height = wa.Height / 2;

            w.Left = wa.Left + Math.Max(0, (wa.Width - width) / 2);
            w.Top = wa.Top + Math.Max(0, (wa.Height - height) / 2);
        }

        private WindowLocationData? LoadData()
        {
            var path = GetSavePath();
            if (!System.IO.File.Exists(path)) return null;
            return JsonDataHelper.LoadFromFile<WindowLocationData>(path);
        }

        private void SaveData(WindowLocationData data)
        {
            var path = GetSavePath();
            var dir = System.IO.Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            JsonDataHelper.SaveToFile(path, data, true);
        }

        private string GetSavePath()
        {
            string basicPath;

            if (!string.IsNullOrEmpty(SaveDirectory))
            {
                basicPath = SaveDirectory!;
            }
            else if (SavePath == WindowLocationMemoryPath.UserDirectory)
            {
                basicPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                            + System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            }
            else
            {
                // 単一ファイル発行では Assembly.Location が空になるため AppContext.BaseDirectory を使う
                basicPath = AppContext.BaseDirectory;
            }

            return System.IO.Path.Combine(basicPath, "windowSizeData", AssociatedObject.GetType().Name + ".json");
        }
    }
}
