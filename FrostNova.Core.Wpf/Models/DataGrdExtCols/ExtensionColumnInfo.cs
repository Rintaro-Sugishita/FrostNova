using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Models.DataGrdExtCols;

public class ExtensionColumnInfo
{
    public string UniqueKey { get; set; } = string.Empty; // "ExtensionName.ItemKey"
    public string HeaderText { get; set; } = string.Empty;
    public ColumnType Type { get; set; }
    public bool IsReadOnly { get; set; } = false;
    public Visibility Visibility { get; set; } = Visibility.Visible;

    public string Width { get; set; } = "Auto";

    // ボタン列用のコマンド
    public ICommand? Command { get; set; }

    // コマンドに渡す固定パラメーター（必要であれば）
    public object CommandParameter { get; set; }
    public List<ComboBoxOption> Options { get; set; } = new();
}
