using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using FrostNova.Core.TypeToolkit;
using Microsoft.VisualBasic.FileIO;
using System.Windows;

namespace FrostNova.Core.TableWork;


/// <summary>
/// Provides methods for importing, exporting, and processing tabular data in various formats, including clipboard
/// text, CSV, JSON, and Excel. Enables conversion between table objects and external representations, facilitating
/// data exchange and manipulation.
/// </summary>
/// <remarks>TableDataProcessor offers a unified interface for handling table data across multiple
/// formats. It supports reading and writing operations to and from clipboard, files, and Excel sheets, as well as
/// conversion to JSON and CSV. The class is designed to work with implementations of ITableObject, allowing
/// flexible integration with different table structures. Many methods accept parameters for customizing
/// import/export behavior, such as delimiter selection, header inclusion, encoding, and formatting options. Thread
/// safety is not guaranteed; instances should be used accordingly in multithreaded scenarios.</remarks>
public class TableDataProcessor
{

    #region Clipboard

    /// <summary>
    /// Pastes delimited text (typically TSV from Excel) from a string into the table object.
    /// </summary>
    /// <param name="tableObject">The target table object to receive data.</param>
    /// <param name="tableText">The source text string.</param>
    /// <param name="startRowIndex">The starting row index in the table.</param>
    /// <param name="startColumnIndex">The starting column index in the table.</param>
    /// <returns>True if the operation was successful.</returns>
    public static bool PasteFromClipboard(ITableObject tableObject, string tableText, int startRowIndex, int startColumnIndex)
    {
        PasteFromText(tableObject, tableText, startRowIndex, startColumnIndex, delimiter: "\t", hasHeader: false);
        return true;
    }

    /// <summary>
    /// Copies the specified table object to the clipboard in HTML format, applying the given formatting options for
    /// columns and null values.
    /// </summary>
    /// <remarks>The copied HTML can be pasted into applications that support rich text or HTML-formatted
    /// clipboard content, such as Microsoft Word or Excel.</remarks>
    /// <param name="tableObject">The table object to be copied to the clipboard. Cannot be null.</param>
    /// <param name="columnBackColor">The background color to apply to table columns in the HTML output.</param>
    /// <param name="columnForeColor">The foreground (text) color to apply to table columns in the HTML output.</param>
    /// <param name="nullForeColor">The foreground (text) color to use for cells containing null values.</param>
    /// <param name="nullText">The text to display in place of null values within the table.</param>
    /// <param name="isWrapColumnText">true to wrap column text in the HTML output; otherwise, false.</param>
    /// <param name="isBoldColumnText">true to display column text in bold in the HTML output; otherwise, false.</param>
    public static void CopyToClipboard(ITableObject tableObject,
        Color columnBackColor,
        Color columnForeColor,
        Color nullForeColor,
        string nullText,
        bool isWrapColumnText,
        bool isBoldColumnText)
    {
        var html = ToClipboardTextAsHtml(tableObject, columnBackColor, columnForeColor, nullForeColor, nullText, isWrapColumnText, isBoldColumnText);

        Clipboard.SetText(html, TextDataFormat.Html);
    }

    /// <summary>
    /// Generates an HTML-formatted string of the table data, optimized for pasting into Microsoft Excel with styles.
    /// </summary>
    /// <param name="tableObject">The source table object.</param>
    /// <param name="columnBackColor">The background color for header cells.</param>
    /// <param name="columnForeColor">The text color for header cells.</param>
    /// <param name="nullForeColor">The text color for null/empty values.</param>
    /// <param name="nullText">The display text for null/empty values.</param>
    /// <param name="isWrapColumnText">Whether to allow text wrapping in cells.</param>
    /// <param name="isBoldColumnText">Whether to use bold font for header cells.</param>
    /// <returns>A formatted HTML string including the CF_HTML clipboard header.</returns>
    public static string ToClipboardTextAsHtml(
        ITableObject tableObject,
        Color columnBackColor,
        Color columnForeColor,
        Color nullForeColor,
        string nullText,
        bool isWrapColumnText,
        bool isBoldColumnText)
    {
        // スタイル準備
        string ToHex(Color c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";
        string foreColorHex = ToHex(columnForeColor);
        string backColorHex = ToHex(columnBackColor);
        string nullColorHex = ToHex(nullForeColor);

        string wrapStyleText = isWrapColumnText ? "" : " nowrap";
        string columnTag = isBoldColumnText ? "th" : "td";

        string nullOutText = $"\t\t<td style=\"text-align: left; mso-number-format:'\\@'; color: #{nullColorHex}\" nowrap>{WebUtility.HtmlEncode(nullText)}</td>";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<table border=\"1\">");

        // ヘッダー生成
        sb.AppendLine("\t<tr>");
        foreach (var item in tableObject.GetColumnCaptions())
        {
            sb.AppendLine($"\t\t<{columnTag} style=\"mso-number-format: '\\@'; text-align: left; color: #{foreColorHex}; background-color: #{backColorHex};\" {wrapStyleText}>{WebUtility.HtmlEncode(item)}</{columnTag}>");
        }
        sb.AppendLine("\t</tr>");

        // データ生成
        for (int i = 0; i < tableObject.RowCount; i++)
        {
            sb.AppendLine("\t<tr>");
            object? rowData = tableObject.GetRowInstance(i);

            for (int j = 0; j < tableObject.ColumnCount; j++)
            {
                var column = tableObject.TargetColumns[j];
                object? oneData = tableObject.GetValue(i, j);
                string? text = column.GetTextValue(rowData);

                if (text == null)
                {
                    sb.AppendLine(nullOutText);
                    continue;
                }

                string textAlign = "left";
                string msoFormat = "'\\@'";

                if (oneData != null)
                {
                    var t = Nullable.GetUnderlyingType(oneData.GetType()) ?? oneData.GetType();
                    if (t == typeof(int) || t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(long))
                    {
                        textAlign = "right";
                        msoFormat = "0";
                    }
                    else if (t == typeof(bool))
                    {
                        textAlign = "center";
                        msoFormat = "0";
                    }
                    else if (t == typeof(DateTime) || t == typeof(TimeSpan))
                    {
                        textAlign = "right";
                        msoFormat = "'\\@'"; // 数値化を防ぐため '\@' を指定
                    }
                }

                sb.AppendLine($"\t\t<td style=\"text-align: {textAlign}; mso-number-format: {msoFormat};\" {wrapStyleText}>{WebUtility.HtmlEncode(text)}</td>");
            }
            sb.AppendLine("\t</tr>");
        }
        sb.AppendLine("</table>");

        // --- オリジナルの計算ロジックを復元しつつ、矛盾を解消 ---
        string body = sb.ToString();
        byte[] bytes = Encoding.UTF8.GetBytes(body);

        // マジックナンバーを調整（StartFragment 120 までのオフセットを考慮）
        int fragmentLength = bytes.Length + 120;
        int endHtml = fragmentLength + 20;

        StringBuilder headerBuilder = new StringBuilder();
        headerBuilder.AppendLine("Version:0.9");
        headerBuilder.AppendLine("StartHTML:00000097");
        headerBuilder.AppendLine($"EndHTML:{endHtml:D8}");
        headerBuilder.AppendLine("StartFragment:00000120");
        headerBuilder.AppendLine($"EndFragment:{fragmentLength:D8}");
        headerBuilder.AppendLine("");

        return headerBuilder.ToString() + body;
    }

    #endregion

    #region CSV

    /// <summary>
    /// Generates a CSV/delimited string from the table object.
    /// </summary>
    /// <param name="tableObject">The source table object.</param>
    /// <param name="delimiter">The field separator string.</param>
    /// <param name="withHeader">Whether to include column captions.</param>
    /// <returns>A delimited text string.</returns>
    public static string ToCsvText(ITableObject tableObject, string delimiter = ", ", bool withHeader = true)
    {
        var sb = new StringBuilder();

        if (withHeader)
        {
            var captions = tableObject.GetColumnCaptions();
            sb.AppendLine(string.Join(delimiter, captions.Select(c => EscapeField(c, delimiter, true))));
        }

        for (int i = 0; i < tableObject.RowCount; i++)
        {
            sb.AppendLine(string.Join(delimiter, GetEscapedRowValues(tableObject, i, delimiter)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports the table object data to a CSV/delimited file.
    /// </summary>
    /// <param name="tableObject">The source table object.</param>
    /// <param name="fileName">The destination file path.</param>
    /// <param name="delimiter">The field separator string.</param>
    /// <param name="withHeader">Whether to include column captions.</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8 with BOM).</param>
    public static void OutputToCsv(ITableObject tableObject, string fileName, string delimiter = ", ", bool withHeader = true, Encoding? encoding = null)
    {
        if (tableObject.ColumnCount == 0) return;

        var actualEncoding = encoding ?? new System.Text.UTF8Encoding(true);

        using (var sw = new StreamWriter(path: fileName, append: false, encoding: actualEncoding))
        {
            //create row
            int rowCount = tableObject.RowCount;
            int columnCount = tableObject.ColumnCount;
            // ヘッダー出力
            if (withHeader)
            {
                var captions = tableObject.GetColumnCaptions();
                sw.WriteLine(string.Join(delimiter, captions.Select(c => EscapeField(c, delimiter, true))));
            }

            // データ行出力（1行ずつ書き込むのでメモリを食わない）
            for (int i = 0; i < tableObject.RowCount; i++)
            {
                sw.WriteLine(string.Join(delimiter, GetEscapedRowValues(tableObject, i, delimiter)));
            }
        }
    }

    // 共通：1行分のエスケープ済み文字列リストを作成する
    private static IEnumerable<string> GetEscapedRowValues(ITableObject tableObject, int rowIndex, string delimiter)
    {
        var rowInstance = tableObject.GetRowInstance(rowIndex);
        for (int j = 0; j < tableObject.ColumnCount; j++)
        {
            var column = tableObject.TargetColumns[j];
            object? oneData = tableObject.GetValue(rowIndex, j);
            string? text = column.GetTextValue(rowInstance) ?? "";

            bool isNumericOrBool = false;
            if (oneData != null)
            {
                var t = Nullable.GetUnderlyingType(oneData.GetType()) ?? oneData.GetType();
                isNumericOrBool = t == typeof(int) || t == typeof(decimal) || t == typeof(double) ||
                                  t == typeof(float) || t == typeof(long) || t == typeof(bool);
            }

            bool forceQuote = delimiter.Contains(",") && !isNumericOrBool;
            yield return EscapeField(text, delimiter, forceQuote);
        }
    }

    private static string EscapeField(string field, string delimiter, bool forceQuote)
    {
        if (string.IsNullOrEmpty(field)) return forceQuote ? "\"\"" : "";

        // タブ区切りのエスケープ（列ズレ防止）
        if (delimiter.Contains("\t"))
        {
            return field.Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
        }

        // デリミタがカンマ系の場合
        string escaped = field.Replace("\"", "\"\"");

        // 1. 強制クォート指定がある
        // 2. データの中にデリミタそのものが含まれている
        // 3. 改行が含まれている
        if (forceQuote || field.Contains(delimiter.Trim()) || field.Contains("\r") || field.Contains("\n"))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }

    /// <summary>
    /// Parses a delimited text string and populates the table object.
    /// </summary>
    /// <param name="tableObject">The target table object.</param>
    /// <param name="text">The delimited source text.</param>
    /// <param name="startRow">The starting row index in the table.</param>
    /// <param name="startCol">The starting column index in the table.</param>
    /// <param name="delimiter">The field separator string.</param>
    /// <param name="hasHeader">Whether to skip the first row as a header.</param>
    public static void PasteFromText(
        ITableObject tableObject,
        string text,
        int startRow,
        int startCol,
        string delimiter = ", ",
        bool hasHeader = true)
    {
        using (var reader = new StringReader(text))
        using (var parser = new TextFieldParser(reader))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(delimiter);
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = true;

            int currentRow = startRow;
            bool isFirstLine = true;

            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields() ?? Array.Empty<string>();

                // ヘッダー行のスキップ判定
                if (isFirstLine && hasHeader)
                {
                    isFirstLine = false;
                    continue;
                }

                // --- 行の準備 ---
                object? rowInstance;
                if (currentRow < tableObject.RowCount)
                {
                    // 既存の行を使う
                    rowInstance = tableObject.GetRowInstance(currentRow);
                }
                else if (tableObject.CanAddRow)
                {
                    // 新しい行を追加する
                    rowInstance = tableObject.AddRow();
                    //tableObject.AddRow(rowInstance); //もう追加されてるので改めて追加しない
                }
                else
                {
                    // これ以上行を増やせない場合は終了（切り捨て）
                    break;
                }

                if (rowInstance == null)
                {
                    break;
                }

                // --- 列への流し込み ---
                for (int f = 0; f < fields.Length; f++)
                {
                    int targetCol = startCol + f;

                    // テーブルの列範囲を超えたらその行の以降のフィールドは無視
                    if (targetCol >= tableObject.ColumnCount) break;

                    var column = tableObject.TargetColumns[targetCol];
                    string val = fields[f];

                    // TableColumn 内部で ConvertBack (文字列 -> 型変換 -> 反映) を完結させる
                    // TableColumn 側で rowInstance のプロパティに値をセットする想定
                    column.SetValueFromText(rowInstance, val);
                }

                currentRow++;
            }
        }
    }

    /// <summary>
    /// Reads a delimited file (CSV/TSV) and populates the table object using a stream.
    /// </summary>
    /// <param name="tableObject">The target table object.</param>
    /// <param name="filePath">The path to the source file.</param>
    /// <param name="startRow">The starting row index in the table.</param>
    /// <param name="startCol">The starting column index in the table.</param>
    /// <param name="delimiter">The field separator string.</param>
    /// <param name="hasHeader">Whether to skip the first row as a header.</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8).</param>
    public static void ImportFromFile(
        ITableObject tableObject,
        string filePath,
        int startRow,
        int startCol,
        string delimiter = ", ",
        bool hasHeader = true,
        Encoding? encoding = null)
    {
        // エンコーディング指定がない場合は、一旦システムのデフォルトまたはUTF-8(BOM)を想定
        var actualEncoding = encoding ?? Encoding.UTF8;

        // ストリームを開いて TextFieldParser に渡す
        // これにより、ファイル全体をメモリに載せずに処理可能
        using (var parser = new TextFieldParser(filePath, actualEncoding))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(delimiter);
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = true;

            int currentRow = startRow;
            bool isFirstLine = true;

            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields() ?? Array.Empty<string>();

                // ヘッダー行のスキップ判定
                if (isFirstLine && hasHeader)
                {
                    isFirstLine = false;
                    continue;
                }

                // --- 行の準備 ---
                object? rowInstance;
                if (currentRow < tableObject.RowCount)
                {
                    rowInstance = tableObject.GetRowInstance(currentRow);
                }
                else if (tableObject.CanAddRow)
                {
                    rowInstance = tableObject.AddRow();
                }
                else
                {
                    // これ以上行を増やせない場合は終了（切り捨て）
                    break;
                }

                if (rowInstance == null) break;

                // --- 列への流し込み ---
                for (int f = 0; f < fields.Length; f++)
                {
                    int targetCol = startCol + f;
                    if (targetCol >= tableObject.ColumnCount) break;

                    var column = tableObject.TargetColumns[targetCol];
                    // ここでも SetValueFromText を共有！
                    column.SetValueFromText(rowInstance, fields[f]);
                }

                currentRow++;
            }
        }
    }
    #endregion

    #region Json

    /// <summary>
    /// Serializes the table object data into a JSON array string.
    /// </summary>
    /// <param name="tableObject">The source table object.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON formatted string.</returns>
    public static string ToJson(ITableObject tableObject, bool indented = true)
    {
        var captions = tableObject.GetColumnCaptions().ToList();
        var list = new List<Dictionary<string, object?>>();

        for (int i = 0; i < tableObject.RowCount; i++)
        {
            var rowData = new Dictionary<string, object?>();
            for (int j = 0; j < tableObject.ColumnCount; j++)
            {
                // 生のデータ（object?）を取得してDictionaryへ
                rowData[captions[j]] = tableObject.GetValue(i, j);
            }
            list.Add(rowData);
        }

        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = indented };
        return System.Text.Json.JsonSerializer.Serialize(list, options);
    }

    /// <summary>
    /// Deserializes a JSON string and populates the table object.
    /// </summary>
    /// <param name="tableObject">The target table object.</param>
    /// <param name="jsonText">The JSON source string.</param>
    /// <param name="startRow">The starting row index in the table.</param>
    public static void PasteFromJson(ITableObject tableObject, string jsonText, int startRow)
    {
        // Dictionaryのリストとしてデシリアライズ
        var list = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, System.Text.Json.JsonElement>>>(jsonText);
        if (list == null) return;

        var captions = tableObject.GetColumnCaptions().ToList();
        int currentRow = startRow;

        foreach (var rowDict in list)
        {
            // 行の準備（既存 or 新規）
            object? rowInstance;
            if (currentRow < tableObject.RowCount)
            {
                rowInstance = tableObject.GetRowInstance(currentRow);
            }
            else if (tableObject.CanAddRow)
            {
                rowInstance = tableObject.AddRow();
            }
            else break;

            if (rowInstance == null) break;

            // 各列のデータを流し込む
            for (int j = 0; j < tableObject.ColumnCount; j++)
            {
                string colName = captions[j];
                if (rowDict.TryGetValue(colName, out var jsonElem))
                {
                    // JsonElement の RawText または ToString を取得
                    // SetValueFromText を経由することで、適切な型へ再変換される
                    string valStr = jsonElem.ValueKind == System.Text.Json.JsonValueKind.String
                        ? jsonElem.GetString() ?? ""
                        : jsonElem.GetRawText();

                    tableObject.TargetColumns[j].SetValueFromText(rowInstance, valStr);
                }
            }
            currentRow++;
        }
    }

    #endregion

    #region Excel

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
    [Obsolete("Moved to the FrostNova.Core.TableWork.Excel package as an ITableObject extension method. " +
        "Add that package and call tableObject.OutputToExcel(...). This keeps FrostNova.Core.TableWork free of any Excel-engine (EPPlus) dependency.", error: false)]
    public static void OutputToExcel(
        ITableObject tableObject, string fileName, string fontFamily, float fontSize,
        bool withHeader,
        Color borderColor, Color columnBackColor, Color columnForeColor,
        string nullText, bool isBoldColumnText, float rowHeight = 25)
        => throw new NotSupportedException(
            "TableDataProcessor.OutputToExcel has moved to the FrostNova.Core.TableWork.Excel package. " +
            "Add that package and call the ITableObject.OutputToExcel(...) extension method instead.");

    /// <summary>
    /// Opens an Excel file and imports data into the table object by mapping columns based on header text.
    /// </summary>
    /// <param name="tableObject">The target table object.</param>
    /// <param name="fileName">The path to the Excel file.</param>
    /// <param name="startRow">The starting row index in the table.</param>
    /// <param name="sheetIndex">The index of the sheet to read from (1-based).</param>
    /// <param name="hasHeader">Whether to use the first row of the Excel sheet to map columns by name.</param>
    [Obsolete("Moved to the FrostNova.Core.TableWork.Excel package as an ITableObject extension method. " +
        "Add that package and call tableObject.ImportFromExcel(...). This keeps FrostNova.Core.TableWork free of any Excel-engine (EPPlus) dependency.", error: false)]
    public static void ImportFromExcel(
        ITableObject tableObject,
        string fileName,
        int startRow,
        int sheetIndex = 1,
        bool hasHeader = true)
        => throw new NotSupportedException(
            "TableDataProcessor.ImportFromExcel has moved to the FrostNova.Core.TableWork.Excel package. " +
            "Add that package and call the ITableObject.ImportFromExcel(...) extension method instead.");

    #endregion

}
