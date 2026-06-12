using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.Common;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace FrostNova.Core.Wpf.Behaviors
{
    /// <summary>
    /// Provides a behavior for <see cref="TextBox"/> controls that enforces input formatting and value constraints for
    /// numeric and date/time data entry.
    /// </summary>
    /// <remarks><para> <b>TextBoxEditLimitBehavior</b> enables validation and formatting of user input in a
    /// <see cref="TextBox"/> based on the configured input type, such as integer, decimal, or date/time. It restricts
    /// invalid keystrokes, handles paste operations, and automatically formats the text when the control loses focus.
    /// </para> <para> This behavior is typically attached to a <see cref="TextBox"/> using XAML or code, and works in
    /// conjunction with attached properties (such as those provided by <c>TextBoxInput</c>) to determine the expected
    /// input format and constraints (e.g., maximum digits, decimal places, or date/time format). </para> <para> For
    /// date/time input, users can quickly enter the current date or time using keyboard shortcuts: <see
    /// langword="Ctrl"/>+<c>;</c> inserts today's date, and <see langword="Ctrl"/>+<see langword="Shift"/>+<c>;</c>
    /// inserts the current date and time. </para> <para> The behavior automatically reverts invalid input and ensures
    /// that the <see cref="TextBox"/> content remains valid according to the specified format. It also normalizes
    /// full-width alphanumeric characters to half-width. </para> <para> This behavior is not thread-safe and should be
    /// used only on the UI thread. </para></remarks>
    public class TextBoxInputLimitBehavior : Behavior<TextBox>
    {

        #region fields

        private int _previousCaretIndex;
        private bool _isInternalUpdate;
        private string _lastValidText = string.Empty;
        private bool _isImeComposing;
        private bool IsImeComposing(InputFormatType type)
        {
            if ( InputFormatType.None == type)
            {
                return _isImeComposing;

            }
            else
            {
                return false;
            }
        }
            #endregion

        #region initialize and finalize

        protected override void OnAttached()
        {
            base.OnAttached();
            NumpadEnterDetector.EnsureInitialized();

            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.TextChanged += OnTextChanged;
            DataObject.AddPastingHandler(AssociatedObject, OnPaste);
            AssociatedObject.LostFocus += OnLostFocus;
            TextCompositionManager.AddPreviewTextInputStartHandler(
        AssociatedObject, OnTextInputStart);

            TextCompositionManager.AddPreviewTextInputUpdateHandler(
                AssociatedObject, OnTextInputUpdate);

            TextCompositionManager.AddPreviewTextInputHandler(
                AssociatedObject, OnTextInputCompleted);

    //        DependencyPropertyDescriptor
    //.FromProperty(TextBox.TextProperty, typeof(TextBox))
    //.AddValueChanged(AssociatedObject, OnTextPropertyChanged);

            _lastValidText = AssociatedObject.Text;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.TextChanged -= OnTextChanged;
            DataObject.RemovePastingHandler(AssociatedObject, OnPaste);
            AssociatedObject.LostFocus -= OnLostFocus;

            TextCompositionManager.RemovePreviewTextInputStartHandler(
        AssociatedObject, OnTextInputStart);

            TextCompositionManager.RemovePreviewTextInputUpdateHandler(
                AssociatedObject, OnTextInputUpdate);

            TextCompositionManager.RemovePreviewTextInputHandler(
                AssociatedObject, OnTextInputCompleted);
        }

        #endregion

        #region events

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.ImeProcessedKey != Key.None) { return; }
            if (e.Key == Key.ImeProcessed) { return; }
            KeyDownForNow(AssociatedObject, e);
            UpdateSource(AssociatedObject, e);
        }


        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = AssociatedObject;

            var type = TextBoxInput.GetFormatType(tb);
            if (IsImeComposing(type))
                return;
            //if (IsImeComposing(tb)) return;


            var input = NormalizeInput(e.Text);
            var next = GetNextText(tb, input);

            //if (!IsAllowedText(e.Text, tb))
            //    e.Handled = true;

            if (type != InputFormatType.None)
            {
                if (!IsValid(next, tb, type))
                    e.Handled = true;
            }
        }


        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalUpdate)
                return;
            var tb = AssociatedObject;
            var type = TextBoxInput.GetFormatType(tb);
            if (IsImeComposing(type))
                return;

            var normalized = NormalizeInput(tb.Text);

            if (type == InputFormatType.None)
            {
                string original = tb.Text;
                string filtered = FilterAllowedText(original, tb);

                if (original == filtered)
                {
                    _lastValidText = original;
                    return;
                }

                int oldCaret = tb.CaretIndex;
                int removedBeforeCaret = CountRemovedBeforeCaret(original, filtered, oldCaret);

                _isInternalUpdate = true;
                tb.Text = filtered;

                tb.Dispatcher.BeginInvoke(
                    DispatcherPriority.Input,
                    new Action(() =>
                    {
                        tb.CaretIndex = Math.Max(0, oldCaret - removedBeforeCaret);
                    }));

                _isInternalUpdate = false;

                _lastValidText = filtered;

            }
            else
            {
                if (IsValid(normalized, tb, type))
                {
                    _previousCaretIndex = tb.CaretIndex;

                    if (tb.Text != normalized)
                    {
                        _isInternalUpdate = true;
                        tb.Text = normalized;
                        _lastValidText = normalized;
                        //tb.CaretIndex = _previousCaretIndex;
                        tb.CaretIndex = Math.Min(_previousCaretIndex, tb.Text.Length);
                        _isInternalUpdate = false;
                    }
                    _lastValidText = normalized;
                }
                else
                {
                    // NG → 差し戻し
                    _isInternalUpdate = true;

                    //tb.Dispatcher.BeginInvoke(
                    //    DispatcherPriority.Background,
                    //    new Action(() =>
                    //    {
                    //        if (tb.CanUndo)
                    //            tb.Undo();

                    //        _isInternalUpdate = false;
                    //    }));
                    var caret = _previousCaretIndex;

                    tb.Text = _lastValidText;
                    tb.CaretIndex = Math.Min(caret, tb.Text.Length);


                    //_isInternalUpdate = false;
                    return;
                }

            }
        }
        private int CountRemovedBeforeCaret(string original, string filtered, int caret)
        {
            int removed = 0;
            int fi = 0;

            for (int oi = 0; oi < original.Length && oi < caret; oi++)
            {
                if (fi < filtered.Length && original[oi] == filtered[fi])
                {
                    fi++;
                }
                else
                {
                    removed++;
                }
            }

            return removed;
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var tb = AssociatedObject;
            var pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";

            var type = TextBoxInput.GetFormatType(tb);

            if (type == InputFormatType.None)
            {
                string filtered = FilterAllowedText(pasteText, tb);

                if (filtered == pasteText)
                    return;

                // 貼り付け内容を差し替える
                e.DataObject = new DataObject(DataFormats.UnicodeText, filtered);

            }
            else
            {
                pasteText = NormalizeInput(pasteText);

                var next = GetNextText(tb, pasteText);

                if (!IsValid(next, tb, type))
                    e.CancelCommand();
            }


        }


        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            var tb = AssociatedObject;

            if (string.IsNullOrWhiteSpace(tb.Text))
                return;

            ApplyFormat(tb);
        }

        #endregion

        #region format

        private void ApplyFormat(TextBox tb)
        {
            var type = TextBoxInput.GetFormatType(tb);
            switch (type)
            {
                case InputFormatType.Integer:
                case InputFormatType.Decimal:
                    FormatNumber(tb);
                    break;

                case InputFormatType.DateTime:
                    FormatDateTime(tb);
                    break;
            }
            _lastValidText = tb.Text;

        }

        #region format for number

        private void FormatNumber(TextBox tb)
        {
            var intDigits = TextBoxInput.GetIntegerDigits(tb);
            var decDigits = TextBoxInput.GetDecimalDigits(tb);
            var sep = TextBoxInput.GetUseSeparator(tb);

            if (!decimal.TryParse(tb.Text, out var value))
                return;

            string format =
                decDigits > 0
                    ? (sep ? $"N{decDigits}" : $"F{decDigits}")
                    : (sep ? "N0" : "F0");

            tb.Text = value.ToString(format);
        }

        #endregion

        #region format for datetime

        private void FormatDateTime(TextBox tb)
        {
            var format = TextBoxInput.GetDateTimeFormat(tb);
            if (format == DateTimeFormatType.None)
                return;

            var displayFormat = GetDisplayFormat(format);
            var parseFormats = BuildParseFormats(format);
            if (!DateTime.TryParseExact(
                       tb.Text,
                       parseFormats,
                       CultureInfo.CurrentCulture,
                       DateTimeStyles.None,
                       out var dt))
            {
                // ここで戻す or エラー表示
                tb.Text = _lastValidText;
                return;
            }

            tb.Text = dt.ToString(displayFormat);
        }
        public static string GetDisplayFormat(DateTimeFormatType type)
        {
            return type switch
            {
                DateTimeFormatType.yyyyMMddHHmmss => "yyyy/MM/dd HH:mm:ss",
                DateTimeFormatType.yyyyMMddHHmm => "yyyy/MM/dd HH:mm",
                DateTimeFormatType.yyyyMMdd => "yyyy/MM/dd",
                DateTimeFormatType.yyyyMM => "yyyy/MM",
                DateTimeFormatType.MMdd => "MM/dd",
                DateTimeFormatType.d => "d",
                DateTimeFormatType.HHmmss => "HH:mm:ss",
                DateTimeFormatType.HHmm => "HH:mm",
                DateTimeFormatType.mmss => "mm:ss",
                _ => string.Empty
            };
        }

        public static string[] BuildParseFormats(DateTimeFormatType type)
        {
            var list = new List<string>();

            void Add(params string[] formats) => list.AddRange(formats);

            switch (type)
            {
                case DateTimeFormatType.yyyyMMddHHmmss:
                    Add(
                        "yyyy/M/d H:m:s",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d H:m",
                        "yyyy/MM/dd HH:mm",
                        "yyyy/M/d",
                        "yyyy/MM/dd"
                    );
                    break;

                case DateTimeFormatType.yyyyMMddHHmm:
                    Add(
                        "yyyy/M/d H:m",
                        "yyyy/MM/dd HH:mm",
                        "yyyy/M/d",
                        "yyyy/MM/dd"
                    );
                    break;

                case DateTimeFormatType.yyyyMMdd:
                    Add(
                        "yyyy/M/d",
                        "yyyy/MM/dd",
                        "yyyy/M/d H:m:s",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d H:m",
                        "yyyy/MM/dd HH:mm"
                    );
                    break;

                case DateTimeFormatType.yyyyMM:
                    Add(
                        "yyyy/M",
                        "yyyy/MM",
                        "yyyy/M/d H:m:s",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d H:m",
                        "yyyy/MM/dd HH:mm"
                    );
                    break;

                case DateTimeFormatType.MMdd:
                    Add(
                        "M/d",
                        "MM/dd",
                        "yyyy/M/d H:m:s",
                        "yyyy/MM/dd HH:mm:ss",
                        "yyyy/M/d H:m",
                        "yyyy/MM/dd HH:mm"
                    );
                    break;

                case DateTimeFormatType.d:
                    Add(
                        "d"
                    );
                    break;

                case DateTimeFormatType.HHmmss:
                    Add(
                        "H:m:s",
                        "HH:mm:ss",
                        "H:m",
                        "HH:mm"
                    );
                    break;

                case DateTimeFormatType.HHmm:
                    Add(
                        "H:m",
                        "HH:mm"
                    );
                    break;

                case DateTimeFormatType.mmss:
                    Add(
                        "m:s",
                        "mm:ss"
                    );
                    break;
            }

            return list.Distinct().ToArray();
        }

        #endregion

        #endregion

        #region validation

        private bool IsValid(string text, TextBox tb, InputFormatType type)
        {
            return type switch
            {
                //case InputFormatType.Integer:
                //    return Regex.IsMatch(text, @"^-?\d*$");
                //case InputFormatType.Decimal:
                //    return Regex.IsMatch(text, @"^-?\d*(\.\d*)?$");
                InputFormatType.Integer => IsValidInteger(text, tb),
                InputFormatType.Decimal => IsValidDecimal(text, tb),
                InputFormatType.DateTime => IsValidDateTime(text, tb),
                _ => true,
            };
        }
        //private static bool IsImeComposing(TextBox tb)
        //{
        //    var imeState = InputMethod.GetPreferredImeState(tb);
        //    return InputMethod.GetIsInputMethodEnabled(tb)
        //        && imeState == InputMethodState.On;
        //}

        #region validation for number

        private bool IsValidInteger(string text, TextBox tb)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            // 符号のみは途中入力として許可
            if (text == "-")
                return true;

            if (!Regex.IsMatch(text, @"^-?\d+$"))
                return false;

            var digits = text.TrimStart('-').Length;
            var maxDigits = TextBoxInput.GetIntegerDigits(tb);

            return digits <= maxDigits;
        }
        private bool IsValidDecimal(string text, TextBox tb)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            // 途中入力を許可
            if (text == "-" || text == "." || text == "-.")
                return true;

            if (!Regex.IsMatch(text, @"^-?\d*(\.\d*)?$"))
                return false;

            var maxInt = TextBoxInput.GetIntegerDigits(tb);
            var maxDec = TextBoxInput.GetDecimalDigits(tb);

            var unsigned = text.TrimStart('-');
            var parts = unsigned.Split('.');

            var intPart = parts[0];
            var decPart = parts.Length > 1 ? parts[1] : "";

            // 整数部桁数
            if (intPart.Length > maxInt)
                return false;

            // 小数部桁数
            if (decPart.Length > maxDec)
                return false;

            return true;
        }

        #endregion

        #region validation for datetime

        private bool IsValidDateTime(string text, TextBox tb)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var format = TextBoxInput.GetDateTimeFormat(tb);
            var displayFormat = GetDisplayFormat(format);

            // 使用可能文字のみ
            if (!Regex.IsMatch(text, @"^[0-9/\:\s]*$"))
                return false;

            // フォーマットに基づく構造チェック
            if (!IsValidDateTimeStructure(text, displayFormat))
                return false;

            // 値チェック（途中入力許可）
            return IsValidDateTimeValues(text);

        }

        private bool IsValidDateTimeStructure(string text, string format)
        {
            // フォーマットから期待される区切り文字を取得
            var expectedSeparators = format.Where(c => c == '/' || c == ':' || c == ' ')
                                            .GroupBy(c => c)
                                            .ToDictionary(g => g.Key, g => g.Count());

            foreach (var sep in expectedSeparators)
            {
                if (text.Count(c => c == sep.Key) > sep.Value)
                    return false;
            }

            // 区切りが連続するのは禁止（途中入力除く）
            if (Regex.IsMatch(text, @"[\/:\s]{2,}"))
                return false;

            return true;
        }
        private bool IsValidDateTimeValues(string text)
        {
            var parts = Regex.Split(text, @"[\/:\s]");

            // yyyy MM dd HH mm ss の順を仮定
            int? year = null, month = null, day = null;
            int? hour = null;//, minute = null, second = null;

            int index = 0;

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    index++;
                    continue;
                }

                if (!int.TryParse(part, out var value))
                    return false;

                switch (index)
                {
                    case 0: // year
                        if (part.Length > 4) return false;
                        year = value;
                        break;

                    case 1: // month
                        if (value < 1 || value > 12) return false;
                        month = value;
                        break;

                    case 2: // day
                        if (value < 1 || value > 31) return false;

                        // 年月が揃っていたら月日制約をかける
                        if (year.HasValue && month.HasValue &&
                            part.Length == 2 &&
                            IsDayExceedsMonth(year.Value, month.Value, value))
                            return false;

                        day = value;
                        break;

                    case 3: // hour
                        if (value < 0 || value > 23) return false;
                        hour = value;
                        break;

                    case 4: // minute
                    case 5: // second
                        if (value < 0 || value > 59) return false;
                        break;
                }

                index++;
            }

            return true;
        }
        private bool IsDayExceedsMonth(int year, int month, int day)
        {
            try
            {
                DateTime.DaysInMonth(year, month);
                return day > DateTime.DaysInMonth(year, month);
            }
            catch
            {
                return true;
            }
        }

        private bool IsPartialDateTime(string text)
        {
            var parts = Regex.Split(text, @"[\/\:\s]");

            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;

                if (!int.TryParse(part, out var value))
                    return false;

                // 年は桁数だけ見る（値制限なし）
                if (part.Length <= 4)
                    continue;

                // 月・日・時・分・秒の上限
                if (value < 0 || value > 59)
                    return false;
            }

            return true;
        }

        #endregion

        private static string GetNextText(TextBox tb, string input)
        {
            var text = tb.Text ?? "";
            var start = tb.SelectionStart;
            var length = tb.SelectionLength;

            return text.Remove(start, length).Insert(start, input);
        }

        private static string NormalizeInput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 全角英数 → 半角
            return text
                .Normalize(NormalizationForm.FormKC);
        }

        #region validation for Text

        [Obsolete]
        private bool IsAllowedText(string text, TextBox tb)
        {
            foreach (var c in text)
            {
                if (!IsAllowedChar(c, tb))
                    return false;
            }
            return true;
        }
        private string FilterAllowedText(string text, TextBox tb)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sb = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                if (IsAllowedChar(c, tb))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private bool IsAllowedChar(char c, TextBox tb)
        {
            var allowedTypes = TextBoxInput.GetAllowedCharTypes(tb);
            var allowedChars = TextBoxInput.GetAllowedChars(tb);
            var deniedChars = TextBoxInput.GetDeniedChars(tb);

            // ① 明示的禁止
            if (!string.IsNullOrEmpty(deniedChars) && deniedChars.Contains(c))
                return false;

            // ② 明示的許可
            if (!string.IsNullOrEmpty(allowedChars) && allowedChars.Contains(c))
                return true;

            // ③ 種類判定（Unicodeベース）

            // 数字
            if (char.IsDigit(c))
                return allowedTypes.HasFlag(AllowedCharTypes.Digit);

            // 半角英字
            if (c <= 0x007A && char.IsLower(c))
                return allowedTypes.HasFlag(AllowedCharTypes.LowerAlpha);

            if (c <= 0x005A && char.IsUpper(c))
                return allowedTypes.HasFlag(AllowedCharTypes.UpperAlpha);

            // 半角記号（ASCII）
            if (c <= 0x007F && (char.IsPunctuation(c) || char.IsSymbol(c)))
                return allowedTypes.HasFlag(AllowedCharTypes.HalfWidthSymbol);

            // 半角カナ
            if (c >= 0xFF65 && c <= 0xFF9F)
                return allowedTypes.HasFlag(AllowedCharTypes.HalfKana);
           
            if (IsProlongedSoundMark(c))
            {
                return allowedTypes.HasFlag(AllowedCharTypes.Hiragana)
                    || allowedTypes.HasFlag(AllowedCharTypes.Katakana)
                    || allowedTypes.HasFlag(AllowedCharTypes.FullWidth);
            }

            // ひらがな
            if ((c >= 0x3040 && c <= 0x309F) )
                return allowedTypes.HasFlag(AllowedCharTypes.Hiragana);

            // 全角カタカナ
            if ((c >= 0x30A0 && c <= 0x30FF))
                return allowedTypes.HasFlag(AllowedCharTypes.Katakana);



            // その他の全角（漢字など）
            return allowedTypes.HasFlag(AllowedCharTypes.FullWidth);
        }

        private static bool IsProlongedSoundMark(char c)
    => c == 'ー';

        #endregion

        #endregion

        #region Now


        private void KeyDownForNow(TextBox tb, KeyEventArgs e)
        {

            if (TextBoxInput.GetFormatType(tb) != InputFormatType.DateTime)
                return;

            if (TextBoxInput.GetEnableNowDateInput(tb) == false)
                return;

            // Ctrl + ;
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.OemSemicolon)
            {
                SetToday(tb);
                e.Handled = true;
                return;
            }

            // Ctrl + Shift + ;
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)
                && e.Key == Key.OemSemicolon)
            {
                SetNow(tb);
                e.Handled = true;
            }
        }


        private void SetToday(TextBox tb)
        {
            var type = TextBoxInput.GetDateTimeFormat(tb);
            var format = GetDisplayFormat(type);

            var dt = DateTime.Today;

            tb.Text = dt.ToString(format);
            _lastValidText = tb.Text;
        }
        private void SetNow(TextBox tb)
        {
            var type = TextBoxInput.GetDateTimeFormat(tb);
            var format = GetDisplayFormat(type);

            var dt = DateTime.Now;

            tb.Text = dt.ToString(format);
            _lastValidText = tb.Text;
        }
        private DateTime ApplyDateTimeType(DateTime dt, DateTimeFormatType type)
        {
            return type switch
            {
                DateTimeFormatType.HHmm or DateTimeFormatType.HHmmss =>
                    DateTime.Today.Add(dt.TimeOfDay),

                DateTimeFormatType.mmss =>
                    DateTime.Today.AddMinutes(dt.Minute).AddSeconds(dt.Second),

                _ => dt
            };
        }


        #endregion

        #region UpdateSource

        private void UpdateSource(TextBox tb, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            bool isNumpadEnter = NumpadEnterDetector.IsNumpadEnter;

            var type = TextBoxInput.GetFormatType(tb);

            if (IsImeComposing(type)) return;
            // フォーマット & 確定処理
            CommitText(tb);

            e.Handled = true;
        }

        private void CommitText(TextBox tb)
        {
            var type = TextBoxInput.GetFormatType(tb);

            // 空文字はそのまま返す
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                UpdateBindingSource(tb);
                _lastValidText = tb.Text;
                return;
            }


            // 厳密チェック & フォーマット
            ApplyFormat(tb);

            // ★ ここが本題
            UpdateBindingSource(tb);
        }

        private void UpdateBindingSource(TextBox tb)
        {
            BindingExpressionBase be =
                BindingOperations.GetBindingExpressionBase(
                    tb,
                    TextBox.TextProperty);

            be?.UpdateSource();
        }

        #endregion

        #region IME

        private void OnTextInputStart(object sender, TextCompositionEventArgs e)
        {
            _isImeComposing = true;
        }

        private void OnTextInputUpdate(object sender, TextCompositionEventArgs e)
        {
            _isImeComposing = true;
        }

        private void OnTextInputCompleted(object sender, TextCompositionEventArgs e)
        {
            _isImeComposing = false;
        }


        #endregion
    }
}
