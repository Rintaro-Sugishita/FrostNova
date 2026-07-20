using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using FrostNova.Core.ExcelWork;
using FrostNova.Core.TableWork;
using HorizontalAlignment = FrostNova.Core.ExcelWork.HorizontalAlignment;
using VerticalAlignment = FrostNova.Core.ExcelWork.VerticalAlignment;

namespace FrostNova.Core.TableWork.Excel
{
    /// <summary>
    /// Excel (.xlsx) import/export helpers for <see cref="ITableObject"/>, exposed as extension methods.
    /// </summary>
    /// <remarks>
    /// These methods used to live on <c>FrostNova.Core.TableWork.TableDataProcessor</c>. They were moved into
    /// this separate package so that the core <c>FrostNova.Core.TableWork</c> assembly no longer carries a
    /// (transitive) dependency on an Excel engine. This package is built on the Apache-2.0 licensed NPOI backend
    /// (<c>FrostNova.Core.ExcelWork.Npoi</c>), so consumers who only need table-like data handling can reference
    /// <c>FrostNova.Core.TableWork</c> alone without pulling in EPPlus.
    /// </remarks>
    public static class TableExcelExtensions
    {
        /// <summary>
        /// Directly creates and saves an Excel file from the table object with specified styles and formats.
        /// </summary>
        /// <param name="tableObject">The source table object.</param>
        /// <param name="fileName">The destination file path.</param>
        /// <param name="fontFamily">The font family to apply to the worksheet.</param>
        /// <param name="fontSize">The font size to apply to the worksheet.</param>
        /// <param name="withHeader">Whether to create a header row.</param>
        /// <param name="borderColor">The color for cell borders.</param>
        /// <param name="columnBackColor">The background color for header cells.</param>
        /// <param name="columnForeColor">The text color for header cells.</param>
        /// <param name="nullText">The text to display for null/empty values.</param>
        /// <param name="isBoldColumnText">Whether to use bold font for header cells.</param>
        /// <param name="rowHeight">The height of the rows.</param>
        public static void OutputToExcel(
            this ITableObject tableObject, string fileName, string fontFamily, float fontSize,
            bool withHeader,
            Color borderColor, Color columnBackColor, Color columnForeColor,
            string nullText, bool isBoldColumnText, float rowHeight = 25)
        {
            if (tableObject.ColumnCount == 0) return;

            using (ExcelOperator xls = new ExcelOperator())
            {
                xls.Open();
                var sheet = xls.GetSheet(1);
                int columnCount = tableObject.ColumnCount;
                int rowCount = tableObject.RowCount;

                // 1. ヘッダーの生成
                if (withHeader)
                {
                    var headers = tableObject.GetColumnCaptions().ToArray();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        sheet.SetValue(1, j + 1, headers[j]);
                    }

                    var headerCells = sheet.GetRange(1, 1, 1, headers.Length);
                    headerCells.SetHorizontalContentAlignment(HorizontalAlignment.Left);
                    headerCells.SetVerticalContentAlignment(VerticalAlignment.Center);
                    headerCells.SetBackground(columnBackColor);
                    headerCells.SetForeground(columnForeColor);
                    if (isBoldColumnText) headerCells.SetFontStyle(System.Drawing.FontStyle.Bold);
                }

                // 2. 列ごとの書式設定
                int bodyStartRow = withHeader ? 2 : 1;
                for (int j = 0; j < columnCount; j++)
                {
                    var columnData = tableObject.TargetColumns[j];
                    if (!string.IsNullOrEmpty(columnData.Format))
                    {
                        // データが存在する全行に対して書式を一括適用
                        var columnRange = sheet.GetRange(bodyStartRow, j + 1, bodyStartRow + rowCount - 1, j + 1);
                        columnRange.SetFormat(columnData.Format);
                    }
                }

                // 3. データの生成
                for (int i = 0; i < rowCount; i++)
                {
                    int excelRowIndex = bodyStartRow + i;

                    // 行インスタンスは行ごとに1回取得（列ループ内で取り直さない）。
                    // 添字はデータ行の 0 始まり i（ヘッダー有無に依存する excelRowIndex とは別物）。
                    var row = tableObject.GetRowInstance(i);

                    for (int j = 0; j < columnCount; j++)
                    {
                        // ここで TableObject 内の Converter 適用済みの値を取得
                        var column = tableObject.TargetColumns[j];
                        object? oneData = column.GetExcelValue(row);

                        if (oneData == null || oneData == DBNull.Value)
                        {
                            sheet.SetValue(excelRowIndex, j + 1, nullText);
                        }
                        else
                        {
                            // 数値や日付はそのまま渡すことで Excel 側で正しく認識される
                            sheet.SetValue(excelRowIndex, j + 1, oneData);
                        }
                    }
                }

                // 4. 全体仕上げ
                var range = sheet.GetUsedRange();
                range.Height = rowHeight;
                range.SetFontFamily(fontFamily);
                range.SetFontSize(fontSize);
                range.DrawBorder(borderColor);

                sheet.AutoFit();
                xls.SaveAs(fileName);
            }
        }

        /// <summary>
        /// Opens an Excel file and imports data into the table object by mapping columns based on header text.
        /// </summary>
        /// <param name="tableObject">The target table object.</param>
        /// <param name="fileName">The path to the Excel file.</param>
        /// <param name="startRow">The starting row index in the table.</param>
        /// <param name="sheetIndex">The index of the sheet to read from (1-based).</param>
        /// <param name="hasHeader">Whether to use the first row of the Excel sheet to map columns by name.</param>
        public static void ImportFromExcel(
            this ITableObject tableObject,
            string fileName,
            int startRow,
            int sheetIndex = 1,
            bool hasHeader = true)
        {
            using (ExcelOperator xls = new ExcelOperator())
            {
                xls.Open(fileName);
                var sheet = xls.GetSheet(sheetIndex);

                var usedRange = sheet.GetUsedRange();
                int lastRow = usedRange.RowCount;
                int lastCol = usedRange.ColumnCount;

                // --- 1. ヘッダーマッピングの作成 ---
                // key: Excelの列インデックス(1～), value: 対応する TableColumn
                var columnMap = new Dictionary<int, TableColumn>();

                int dataStartRow = 1;

                if (hasHeader)
                {
                    var tableColumns = tableObject.TargetColumns;
                    for (int j = 1; j <= lastCol; j++)
                    {
                        object? headerValue = sheet.GetValue(1, j);
                        string headerText = headerValue?.ToString() ?? "";

                        // TableColumnの中からヘッダーテキストが一致するものを探す
                        var matchedColumn = tableColumns.FirstOrDefault(c => c.HeaderText == headerText);
                        if (matchedColumn != null)
                        {
                            columnMap.Add(j, matchedColumn);
                        }
                    }
                    dataStartRow = 2; // ヘッダーがある場合は2行目からデータ
                }
                else
                {
                    // ヘッダーがない場合は、左から順番にマッピング（従来の挙動）
                    for (int j = 1; j <= Math.Min(lastCol, tableObject.ColumnCount); j++)
                    {
                        columnMap.Add(j, tableObject.TargetColumns[j - 1]);
                    }
                }

                if (columnMap.Count == 0) return; // 一致する列が一つもない場合は終了

                // --- 2. データのインポート ---
                int currentRow = startRow;
                for (int i = dataStartRow; i <= lastRow; i++)
                {
                    // 行の準備
                    object? rowInstance;
                    if (currentRow < tableObject.RowCount)
                    {
                        rowInstance = tableObject.GetRowInstance(currentRow);
                    }
                    else if (tableObject.CanAddRow)
                    {
                        // AddRow は「複数行一括追加」を想定して IEnumerable を返す設計。
                        // ここでは1行分を追加し、その実体（1件目）を書き込み対象にする。
                        rowInstance = tableObject.AddRow(1)?.FirstOrDefault();
                    }
                    else break;

                    if (rowInstance == null) break;

                    // マッピングされた列のみ値をセット
                    foreach (var kvp in columnMap)
                    {
                        int excelColIndex = kvp.Key;
                        var column = kvp.Value;

                        object? excelValue = sheet.GetValue(i, excelColIndex);

                        // Excel専用の逆変換・セット関数を呼び出す
                        column.SetValueFromExcel(rowInstance, excelValue);
                    }

                    currentRow++;
                }
            }
        }
    }
}
