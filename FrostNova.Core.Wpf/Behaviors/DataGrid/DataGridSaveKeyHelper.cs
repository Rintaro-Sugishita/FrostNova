using FrostNova.Core.Wpf.AttachedProperties;
using System.Windows.Controls;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Behaviors
{
    internal static class DataGridSaveKeyHelper
    {
        /// <summary>
        /// グリッドの保存キーを解決する。
        /// 優先順位: ビヘイビア自身の SaveKey → DataGridEx.SaveKey → DataGrid.Name
        /// </summary>
        public static string GetGridKey(DataGrid grid, string? behaviorSaveKey = null)
        {
            if (!string.IsNullOrEmpty(behaviorSaveKey)) return behaviorSaveKey;

            var extKey = DataGridEx.GetSaveKey(grid);
            if (!string.IsNullOrEmpty(extKey)) return extKey;

            if (!string.IsNullOrEmpty(grid.Name)) return grid.Name;

            return string.Empty;
        }

        /// <summary>
        /// 列の保存キーを解決する。
        /// 優先順位: DataGridColumnEx.Key → Binding.Path → Header 文字列
        /// いずれも取得できない場合は空文字を返す（その列は保存スキップ）。
        /// </summary>
        public static string GetColumnKey(DataGridColumn col)
        {
            var key = DataGridColumnEx.GetKey(col);
            if (!string.IsNullOrEmpty(key)) return key;

            if (col is DataGridBoundColumn boundCol && boundCol.Binding is Binding binding)
            {
                var path = binding.Path?.Path;
                if (!string.IsNullOrEmpty(path)) return path;
            }

            return col.Header?.ToString() ?? string.Empty;
        }
    }
}
