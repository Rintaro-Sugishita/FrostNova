using Microsoft.VisualBasic;
using Microsoft.Xaml.Behaviors;
using FrostNova.Core.Wpf.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FrostNova.Core.Wpf.AttachedProperties;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridColumnSizeSavingBehavior : Behavior<DataGrid>
    {
        private bool _userHasReordered = false;
        private bool _originalCanUserReorderColumns;

        // ロード時に読み込んだDisplayIndex値。ユーザーが並び替えしなかった場合にそのまま再保存するために保持。
        private Dictionary<string, int> _loadedDisplayIndices = new();

        // 動的カラムが後から追加されたときに幅を復元するためのキャッシュ
        private Dictionary<string, DataGridColumnData>? _savedDataDict;

        // LoadData実行前のXAML定義時の初期幅。ResetWidths()で参照する。
        private List<(string key, DataGridLength width)> _initialWidths = new();

        // RebuildColumnsで複数カラムが連続追加される際に、DisplayIndex復元を一度だけ遅延実行するためのフラグ
        private bool _pendingDisplayIndexRestore = false;

        protected override void OnAttached()
        {
            base.OnAttached();

            _originalCanUserReorderColumns = AssociatedObject.CanUserReorderColumns;
            AssociatedObject.CanUserReorderColumns = true;

            AssociatedObject.Initialized += AssociatedObject_Initialized;
            AssociatedObject.Unloaded += DataGridColumnSizeSavingBehavior_Closed;
            AssociatedObject.Columns.CollectionChanged += OnColumnsCollectionChanged;

            var window = System.Windows.Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.Closed += DataGridColumnSizeSavingBehavior_Closed;
            }
            else
            {
                var dis = AssociatedObject.Dispatcher;
                dis.BeginInvoke((Action)(() =>
                                {
                                    while (true)
                                    {
                                        try
                                        {
                                            var window = Window.GetWindow(AssociatedObject);
                                            if (window != null)
                                            {
                                                window.Closed += DataGridColumnSizeSavingBehavior_Closed;
                                                return;
                                            }
                                            else
                                            {
                                                System.Threading.Thread.Sleep(200);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex);
                                        }
                                    }
                                }));

            }


        }

        protected override void OnDetaching()
        {
            var window = System.Windows.Window.GetWindow(AssociatedObject);
            if (window != null)
                window.Closed -= DataGridColumnSizeSavingBehavior_Closed;
            AssociatedObject.Initialized -= AssociatedObject_Initialized;
            AssociatedObject.Unloaded -= DataGridColumnSizeSavingBehavior_Closed;
            AssociatedObject.ColumnReordered -= OnColumnReordered;
            AssociatedObject.Columns.CollectionChanged -= OnColumnsCollectionChanged;
            AssociatedObject.CanUserReorderColumns = _originalCanUserReorderColumns;
            base.OnDetaching();

        }

        private void AssociatedObject_Initialized(object? sender, EventArgs e)
        {
            if (sender is DataGrid grid)
            {
                // LoadData前に初期状態を保存（XAML定義値がリセット基準になる）
                CaptureInitialWidths(grid);
                LoadData(grid);
                // ロード完了後にサブスクライブ。ロード中の DisplayIndex 変化を「ユーザー操作」と誤判定しないため。
                grid.ColumnReordered += OnColumnReordered;
            }
        }

        private void CaptureInitialWidths(DataGrid grid)
        {
            _initialWidths = grid.Columns
                .Select(col => (key: DataGridSaveKeyHelper.GetColumnKey(col), width: col.Width))
                .Where(x => !string.IsNullOrEmpty(x.key))
                .ToList();
        }

        /// <summary>
        /// 列幅をXAML定義時の初期値にリセットする。
        /// </summary>
        public void ResetWidths()
        {
            // 動的カラムの幅キャッシュもクリア（次回追加時に保存済み幅ではなくXAMLデフォルトが使われる）
            _savedDataDict = null;

            foreach (var (key, width) in _initialWidths)
            {
                var col = AssociatedObject.Columns
                    .FirstOrDefault(c => DataGridSaveKeyHelper.GetColumnKey(c) == key);
                if (col != null)
                    col.Width = width;
            }
        }

        /// <summary>
        /// 列の表示順をXAML定義順（Columnsコレクション順）にリセットする。
        /// </summary>
        public void ResetOrder()
        {
            _userHasReordered = false;
            _loadedDisplayIndices.Clear();

            // Columns コレクションの挿入順 = XAML定義順 に DisplayIndex を揃える
            for (int i = 0; i < AssociatedObject.Columns.Count; i++)
                AssociatedObject.Columns[i].DisplayIndex = i;
        }

        private void OnColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            _userHasReordered = true;
        }

        private void DataGridColumnSizeSavingBehavior_Closed(object? sender, EventArgs e)
        {
            SaveData(AssociatedObject);
        }

        /// <summary>
        /// save key
        /// </summary>
        public string SaveKey
        {
            get { return (string)GetValue(SaveKeyProperty); }
            set { SetValue(SaveKeyProperty, value); }
        }
        public static readonly DependencyProperty SaveKeyProperty = DependencyProperty.Register(nameof(SaveKey), typeof(string), typeof(DataGridColumnSizeSavingBehavior));

        /// <summary>
        /// string
        /// </summary>
        public string SavePath
        {
            get { return (string)GetValue(SavePathProperty); }
            set { SetValue(SavePathProperty, value); }
        }
        public static readonly DependencyProperty SavePathProperty = DependencyProperty.Register(nameof(SavePath), typeof(string), typeof(DataGridColumnSizeSavingBehavior));

        private string GetSavePath()
        {
            var key = DataGridSaveKeyHelper.GetGridKey(AssociatedObject, SaveKey);
            if (string.IsNullOrEmpty(key)) { return string.Empty; }

            string path = SavePath;
            if (string.IsNullOrEmpty(path))
            {
                // 単一ファイル発行では Assembly.Location が空になるため AppContext.BaseDirectory を使う
                path = AppContext.BaseDirectory;
            }
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return System.IO.Path.Combine(path, "windowSizeData", "gridColumnData", key + ".json");
        }


        private void OnColumnsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add) return;
            if (_savedDataDict == null || e.NewItems == null) return;

            // 幅はすぐに適用（RebuildColumnsが複数カラムを追加する間も各カラムに即反映）
            foreach (DataGridColumn col in e.NewItems)
            {
                string key = DataGridSaveKeyHelper.GetColumnKey(col);
                if (!string.IsNullOrEmpty(key) && _savedDataDict.TryGetValue(key, out var saved) && saved.Width >= 0)
                    col.Width = saved.Width;
            }

            // DisplayIndex の復元は全カラム追加後にまとめて実行する。
            // RebuildColumns が複数カラムを連続追加するため、1カラムずつ適用すると相互シフトで順序が崩れる。
            // BeginInvoke により同一ディスパッチサイクル内の追加が全て完了してから一括再適用する。
            if (!_pendingDisplayIndexRestore)
            {
                _pendingDisplayIndexRestore = true;
                AssociatedObject.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    _pendingDisplayIndexRestore = false;
                    ApplySavedDisplayIndices();
                });
            }
        }

        private void ApplySavedDisplayIndices()
        {
            if (_savedDataDict == null) return;

            // LoadData と同じロジックで静的＋動的列が揃った現在の全列に DisplayIndex を再適用する
            var withSavedIndex = new List<(DataGridColumn col, int index)>();
            var withoutSavedIndex = new List<DataGridColumn>();

            foreach (var col in AssociatedObject.Columns)
            {
                string key = DataGridSaveKeyHelper.GetColumnKey(col);
                if (!string.IsNullOrEmpty(key) && _savedDataDict.TryGetValue(key, out var saved) && saved.DisplayIndex != -1)
                    withSavedIndex.Add((col, saved.DisplayIndex));
                else
                    withoutSavedIndex.Add(col);
            }

            var orderedCols = withSavedIndex
                .OrderBy(x => x.index)
                .Select(x => x.col)
                .Concat(withoutSavedIndex)
                .ToList();

            for (int i = 0; i < orderedCols.Count; i++)
                orderedCols[i].DisplayIndex = i;
        }

        private void LoadData(DataGrid grid)
        {
            if (grid == null) { return; }
            string fullPath = GetSavePath();

            var data = FrostNova.Core.JsonData.JsonDataHelper.LoadFromFile<List<DataGridColumnData>>(fullPath);
            if (data == null || data.Count == 0) { return; }

            var dataDict = data.ToDictionary(x => x.Key);

            // ロード済みの DisplayIndex を保持（未操作時の再保存用）
            _loadedDisplayIndices = data
                .Where(x => x.DisplayIndex != -1)
                .ToDictionary(x => x.Key, x => x.DisplayIndex);

            // 動的カラムが後から追加されたときに幅を復元するためにキャッシュ
            _savedDataDict = data.ToDictionary(x => x.Key);

            // 幅を適用しつつ、保存済みの順序情報と新規列を分類
            var withSavedIndex = new List<(DataGridColumn col, int index)>();
            var withoutSavedIndex = new List<DataGridColumn>();

            foreach (var col in grid.Columns)
            {
                string key = DataGridSaveKeyHelper.GetColumnKey(col);

                if (!string.IsNullOrEmpty(key) && dataDict.TryGetValue(key, out var saved))
                {
                    if (saved.Width >= 0)
                        col.Width = saved.Width;
                    if (saved.DisplayIndex != -1)
                        withSavedIndex.Add((col, saved.DisplayIndex));
                    else
                        withoutSavedIndex.Add(col);
                }
                else
                {
                    // 保存データに存在しない新規列 → 末尾へ
                    withoutSavedIndex.Add(col);
                }
            }

            // 保存済み列を保存順で並べ、その後ろに新規列を配置
            var orderedCols = withSavedIndex
                .OrderBy(x => x.index)
                .Select(x => x.col)
                .Concat(withoutSavedIndex)
                .ToList();

            // 連番で DisplayIndex を設定（範囲外エラーを防ぎ、重複も避ける）
            for (int i = 0; i < orderedCols.Count; i++)
            {
                orderedCols[i].DisplayIndex = i;
            }
        }

        private void SaveData(DataGrid grid)
        {

            string fullPath = GetSavePath();
            var saveData = new List<DataGridColumnData>();
            foreach (var item in grid.Columns)
            {
                string key = DataGridSaveKeyHelper.GetColumnKey(item);
                if (string.IsNullOrEmpty(key)) continue;
                bool canSave = DataGridColumnEx.GetCanSaveWidth(item);
                DataGridColumnData columnData = new DataGridColumnData();
                columnData.Key = key;
                // Width=-1 は「幅は復元しない」マーカー（CanSaveWidth=False 列の DisplayIndex を保存するため）
                columnData.Width = canSave ? item.ActualWidth : -1;

                if (_userHasReordered)
                {
                    // ユーザーが意図的に並び替えた → 現在の順序を保存
                    columnData.DisplayIndex = item.DisplayIndex;
                }
                else if (_loadedDisplayIndices.TryGetValue(key, out var loadedIdx))
                {
                    // 並び替えなし、前回の順序データがあればそのまま維持
                    columnData.DisplayIndex = loadedIdx;
                }
                else
                {
                    // 並び替えなし、初回保存 → -1（次回ロード時に順序を変更しない）
                    columnData.DisplayIndex = -1;
                }

                saveData.Add(columnData);
            }

            FrostNova.Core.JsonData.JsonDataHelper.SaveToFile(fullPath, saveData, true);
        }



    }
}
