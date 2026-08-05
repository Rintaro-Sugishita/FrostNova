using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.Controls;
using FrostNova.Core.Wpf.Interfaces;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridTablePasteBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Initialized += AssociatedObject_Initialized;
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            AssociatedObject.KeyDown += AssociatedObject_KeyDown;
            AssociatedObject.CopyingRowClipboardContent += AssociatedObject_CopyingRowClipboardContent;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Initialized -= AssociatedObject_Initialized;
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            AssociatedObject.KeyDown -= AssociatedObject_KeyDown;
            AssociatedObject.CopyingRowClipboardContent -= AssociatedObject_CopyingRowClipboardContent;

            // Initialized 時に各列へ張った PastingCellClipboardContent を解除する
            foreach (var item in AssociatedObject.Columns)
            {
                item.PastingCellClipboardContent -= Item_PastingCellClipboardContent;
            }
        }


        /// <summary>
        /// string
        /// </summary>
        public ICommand PasteFromClipboardCommand
        {
            get { return (ICommand)GetValue(PasteFromClipboardCommandProperty); }
            set { SetValue(PasteFromClipboardCommandProperty, value); }
        }
        public static DependencyProperty PasteFromClipboardCommandProperty = DependencyProperty.Register(nameof(PasteFromClipboardCommand), typeof(ICommand), typeof(DataGridTablePasteBehavior), new PropertyMetadata(null));

        /// <summary>
        /// string
        /// </summary>
        public ICommand NewItemNeededWhenPaste
        {
            get { return (ICommand)GetValue(NewItemNeededWhenPasteProperty); }
            set { SetValue(NewItemNeededWhenPasteProperty, value); }
        }
        public static DependencyProperty NewItemNeededWhenPasteProperty = DependencyProperty.Register(nameof(NewItemNeededWhenPaste), typeof(ICommand), typeof(DataGridTablePasteBehavior));

        private void AssociatedObject_Initialized(object? sender, EventArgs e)
        {
            foreach (var item in AssociatedObject.Columns)
            {
                if (item is DataGridPasteTemplateColumn templateColumn)
                {
                    templateColumn.PastingCellClipboardContent += Item_PastingCellClipboardContent;
                }
                else
                {
                    item.PastingCellClipboardContent += Item_PastingCellClipboardContent;
                }
            }
        }

        private void Item_PastingCellClipboardContent(object? sender, DataGridCellClipboardEventArgs e)
        {
            if (PasteFromClipboardCommand != null)
            {
                if (PasteFromClipboardCommand.CanExecute(e))
                {
                    PasteFromClipboardCommand.Execute(e);
                }
            }
        }

        private void AssociatedObject_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var dataGrid = AssociatedObject as DataGrid;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.V))
            {
                //CTRL + V
                OnPaste();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.X))
            {
                //CTRL + X（切り取り = コピー後にクリア）
                OnCut();
                e.Handled = true;
            }
        }

        private void OnCut()
        {
            // まず標準のコピー（CopyingRowClipboardContent 経由で値が注入される）
            if (ApplicationCommands.Copy.CanExecute(null, AssociatedObject))
            {
                ApplicationCommands.Copy.Execute(null, AssociatedObject);
            }

            // 選択セルをクリア
            foreach (var cell in AssociatedObject.SelectedCells)
            {
                if (!cell.Column.IsReadOnly)
                {
                    cell.Column.OnPastingCellClipboardContent(cell.Item, null);
                }
            }
        }
        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Delete))
            {
                OnDelete(e);
            }
        }


        private void OnPaste()
        {
            // 張り付け開始位置設定
            var startRowIndex = AssociatedObject.ItemContainerGenerator.IndexFromContainer(
                (DataGridRow)AssociatedObject.ItemContainerGenerator.ContainerFromItem
                (AssociatedObject.SelectedCells[0].Item));
            var startColIndex = AssociatedObject.SelectedCells[0].Column.DisplayIndex;

            // クリップボード文字列から行を取得

            if (!Clipboard.ContainsData(DataFormats.Text))
            {
                return;
            }
            var clipboartText = (string)Clipboard.GetData(DataFormats.Text);

#if NET48
            var pasteRows = clipboartText.Replace("\r", "")
                .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

#else
            var pasteRows = clipboartText.Replace("\r", "")
                .Split(new string[] { "\n" }, StringSplitOptions.TrimEntries).ToList();
#endif


            if (clipboartText.EndsWith("\n"))
            {
                pasteRows.Remove(pasteRows.Last());
            }

            var rowCount = AssociatedObject.Items.Count;



            var maxRowCount = pasteRows.Count();
            var needRowCount = startRowIndex + maxRowCount;
            var addRowCount = (pasteRows.Count) - (rowCount - startRowIndex);

            if (addRowCount > 0)
            {
                if (NewItemNeededWhenPaste?.CanExecute(addRowCount) == true)
                {
                    NewItemNeededWhenPaste.Execute(addRowCount);
                }

            }

            for (int i = 0; i < maxRowCount; i++)
            {
                var rowIndex = startRowIndex + i;

                // タブ区切りでセル値を取得
                var pasteCells = pasteRows[i].Split('\t');

                if (rowIndex >= AssociatedObject.Items.Count) { break; }
                var targetItem = AssociatedObject.Items[rowIndex];

                // 選択位置から列数繰り返す
                var maxColCount = Math.Min(pasteCells.Count(), AssociatedObject.Columns.Count - startColIndex);
                for (int colCount = 0; colCount < maxColCount; colCount++)
                {
                    var column = AssociatedObject.Columns[colCount + startColIndex];

                    // 貼り付け
                    if (!column.IsReadOnly)
                    {
                        var cellContent = column.GetCellContent(targetItem);
                        var customPasted = false;
                        if (cellContent != null)
                        {
                            //インターフェースを実装している
                            var pasteAware = GeneralHelper.FindVisualChild<FrameworkElement>(cellContent)
                                                as ICustomTextPaste;
                            if (pasteAware != null)
                            {
                                pasteAware.Paste(pasteCells[colCount]);
                                customPasted = true;
                            }
                        }
                        if (customPasted == false)
                        {
                            column.OnPastingCellClipboardContent(targetItem, pasteCells[colCount]);
                        }



                    }
                }
            }

            // 選択位置復元
            AssociatedObject.CurrentCell = new DataGridCellInfo(
                AssociatedObject.Items[startRowIndex], AssociatedObject.Columns[startColIndex]);

        }

        private void OnDelete(KeyEventArgs e)
        {
            var gotCell = DataGridHelper.GetCell(AssociatedObject, AssociatedObject.CurrentCell);
            if (gotCell?.IsEditing == true)
            {
                return;
            }

            foreach (var cell in AssociatedObject.SelectedCells)
            {
                if (!cell.Column.IsReadOnly)
                {
                    cell.Column.OnPastingCellClipboardContent(cell.Item, null);
                }
            }
            e.Handled = true;
        }

        private void AssociatedObject_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            if (ClipboardValueProviders == null)
                return;

            var item = e.Item;

            var c = e.ClipboardRowContent.Count;
            for (var i = c - 1; i >= 0; i--)
            {
                var rowContent = e.ClipboardRowContent[i];
                var column = rowContent.Column;
                // Key 付きのカラムか調べる
                var key = DataGridColumnEx.GetKey(column);
                if (key != null && ClipboardValueProviders.TryGetValue(key, out var getValueFunc))
                {
                    string value = getValueFunc.Invoke(item);
                    //string value = getValueFunc(item);

                    var content = e.ClipboardRowContent.FirstOrDefault(x => x.Column == column && x.Item == item);
                    var index = e.ClipboardRowContent.IndexOf(content);
                    if (index >= 0)
                    {
                        // テンプレート列など既定 Content が null の場合でも「置換」する。
                        // 旧実装は null のとき Remove せず Insert していたため空セルが重複していた。
                        e.ClipboardRowContent[index] = new DataGridClipboardCellContent(item, column, value);
                    }
                }
            }
        }

        public static readonly DependencyProperty ClipboardValueProvidersProperty =
        DependencyProperty.Register(
            nameof(ClipboardValueProviders),
            typeof(Dictionary<string, Func<object, string>>),
            typeof(DataGridTablePasteBehavior),
            new PropertyMetadata(null));

        public Dictionary<string, Func<object, string>> ClipboardValueProviders
        {
            get => (Dictionary<string, Func<object, string>>)GetValue(ClipboardValueProvidersProperty);
            set => SetValue(ClipboardValueProvidersProperty, value);
        }




    }
}
