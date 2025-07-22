using Microsoft.Extensions.FileSystemGlobbing;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static OfficeOpenXml.ExcelErrorValue;

namespace FrostNova.Core.ExcelWork.TemplateFramework
{
    public class RangeTemplate : IDisposable
    {
        const string pattern = @"\{\{(.*?)\}\}";
        const string formulaPattern = @"\{(.*?)\}";

        public int FromRow { get; private set; } = -1;

        public int ToRow { get; private set; } = -1;

        public int FromColumn { get; private set; } = -1;

        public int ToColumn { get; private set; } = -1;

        private Sheet _sheet;

        private Dictionary<string, List<KeyCell>> KeyCollection = new Dictionary<string, List<KeyCell>>();

        private Dictionary<string, SettingValue> keyValueList = new Dictionary<string, SettingValue>();

        private Dictionary<(int relativeRow, int relativeColumn), SettingValue> addressValueList = new Dictionary<(int relativeRow, int relativeColumn), SettingValue>();

        private Dictionary<string, SettingValue> keyFormulaList = new Dictionary<string, SettingValue>();

        private Dictionary<(int relativeRow, int relativeColumn), SettingValue> addressFormulaList = new Dictionary<(int relativeRow, int relativeColumn), SettingValue>();

        private Range _templateRange;

        private InsertShiftType? ShiftType { get; set; } = null;
        private int flushedCount = 0;
        private bool isTemplateDelete = true;

        public string SheetName
        {
            get => _sheet.Name;
        }

        public string TemplateName
        {
            get; set;
        } = "";

        internal RangeTemplate(Sheet sheet, int fromRow, int fromColumn, int toRow, int toColumn, InsertShiftType? shiftType = null)
        {
            _sheet = sheet;
            FromRow = fromRow;
            ToRow = toRow;
            FromColumn = fromColumn;
            ToColumn = toColumn;

            ShiftType = shiftType;

            _templateRange = GetTemplateRange();
            Analyze();
        }

        public void SetValue(int relativeRow, int relativeColumn, object value, bool canOverwrite = true, string format = "", HorizontalAlignment horizontalAlignment = HorizontalAlignment.General, VerticalAlignment verticalAlignment = VerticalAlignment.Top)
        {
            if (!canOverwrite && addressValueList.ContainsKey((relativeRow, relativeColumn))) { return; }
            addressValueList[(relativeRow, relativeColumn)] = CreateValue(value, format, horizontalAlignment, verticalAlignment);
        }

        public void SetValue(string key, object? value, bool canOverwrite = true, string format = "", HorizontalAlignment horizontalAlignment = HorizontalAlignment.General, VerticalAlignment verticalAlignment = VerticalAlignment.Top)
        {
            if (!canOverwrite && keyValueList.ContainsKey(key)) { return; }
            keyValueList[key] = CreateValue(value, format, horizontalAlignment, verticalAlignment);
        }

        public void SetA1Formula(string key, string formula, bool canOverwrite = true, string format = "", HorizontalAlignment horizontalAlignment = HorizontalAlignment.General, VerticalAlignment verticalAlignment = VerticalAlignment.Top)
        {
            if (!canOverwrite && keyValueList.ContainsKey(key)) { return; }
            keyFormulaList[key] = CreateValue(formula, format, horizontalAlignment, verticalAlignment, true, false);
        }


        private SettingValue CreateValue(object? value, string format = "", HorizontalAlignment horizontalAlignment = HorizontalAlignment.General, VerticalAlignment verticalAlignment = VerticalAlignment.Top, bool isFormula = false, bool isR1C1 = false)
        {
            SettingValue settingValue = new SettingValue();
            settingValue.Value = value;
            settingValue.Format = format;
            settingValue.IsFormula = isFormula;
            settingValue.IsR1C1 = isR1C1;
            //settingValue.HorizontalAlignment = horizontalAlignment;
            //settingValue.VerticalAlignment = verticalAlignment;

            return settingValue;
        }



        public RangeTemplate Flush(DataRow valueClass, int pasteRow = -1, int pasteColumn = -1)
        {
            foreach (DataColumn item in valueClass.Table.Columns)
            {
                SetValue(item.ColumnName, valueClass[item.ColumnName], false);
            }

            return Flush(pasteRow, pasteColumn);
        }

        public RangeTemplate Flush<T>(Dictionary<string, T> valueDictionary, int pasteRow = -1, int pasteColumn = -1) where T : class
        {
            foreach (var item in valueDictionary)
            {
                SetValue(item.Key, item.Value, false);
            }

            return Flush(pasteRow, pasteColumn);
        }

        public RangeTemplate Flush<T>(T valueClass, int pasteRow = -1, int pasteColumn = -1) where T : class
        {
            var t = typeof(T);
            var properties = t.GetProperties();
            foreach (var item in properties)
            {
                SetValue(item.Name, item.GetValue(valueClass), false);
            }


            return Flush(pasteRow, pasteColumn);

        }

        public RangeTemplate Flush(int pasteRow = -1, int pasteColumn = -1)
        {

            Range pasteRange;
            if (pasteRow == -1 && pasteColumn == -1)
            {
                pasteRange = _templateRange;
                isTemplateDelete = false;
            }
            else
            {
                if (pasteRow != -1 && pasteColumn == -1)
                {
                    //insert row
                    _sheet.InsertRow(pasteRow, _templateRange.RowCount);

                }
                else if (pasteRow == -1 && pasteColumn != -1)
                {
                    //insert column
                    _sheet.InsertColumn(pasteColumn, _templateRange.ColumnCount);
                }
                else if (pasteRow != -1 && pasteColumn != -1)
                {
                    //insert range
                    _sheet.InsertRange(pasteRow, pasteColumn, pasteRow + _templateRange.RowCount - 1, pasteColumn + _templateRange.ColumnCount - 1, ShiftType ?? InsertShiftType.EntireRow);

                }
                pasteRange = _sheet.GetRangeFromCount(
                    pasteRow == -1 ? _templateRange.Row : pasteRow,
                    pasteColumn == -1 ? _templateRange.Column : pasteColumn,
                    _templateRange.RowCount, _templateRange.ColumnCount);

                _templateRange.CopyToRange(pasteRange);
            }

            
            if (!keyValueList.ContainsKey("no")) keyValueList.Add("no", CreateValue(flushedCount + 1));

            //set value
            if (KeyCollection.Count > 0)
            {
                SetKeyValue(pasteRange.Row, pasteRange.Column, keyValueList);
                SetKeyValue(pasteRange.Row, pasteRange.Column, keyFormulaList);
            }

            SetAddressValue(pasteRange.Row, pasteRange.Column, addressValueList);
            SetAddressValue(pasteRange.Row, pasteRange.Column, addressFormulaList);



            keyValueList.Clear();
            keyFormulaList.Clear();
            addressValueList.Clear();
            addressFormulaList.Clear();
            flushedCount++;

            return this;
        }



        public Range GetTemplateRange()
        {
            Range templateRange;

            if (FromRow == -1 || FromColumn == -1 || ToRow == -1 || ToColumn == -1)
            {
                Range usedRange = _sheet.GetUsedRange();
                templateRange = _sheet.GetRange(
                    FromRow == -1 ? usedRange.Row : FromRow,
                    FromColumn == -1 ? usedRange.Column : FromColumn,
                    ToRow == -1 ? usedRange.ToRow : ToRow,
                    ToColumn == -1 ? usedRange.ToColumn : ToColumn
                    );
            }
            else
            {
                templateRange = _sheet.GetRange(FromRow, FromColumn, ToRow, ToColumn);
            }

            return templateRange;
        }

        public void Analyze()
        {
            KeyCollection.Clear();
            for (int i = 0; i < _templateRange.RowCount; i++)

                for (int j = 0; j < _templateRange.ColumnCount; j++)
                {
                    var row = _templateRange.Row + i;
                    var column = _templateRange.Column + j;

                    var value = _sheet.GetValue(row, column);
                    if (value == null) { continue; }
                    var valueText = value.ToString();
                    if (string.IsNullOrEmpty(valueText)) { continue; }
                    string[] matches;

                    var a = Regex.Match(valueText, pattern);
                    if (a.Success && a.Groups.Count > 1 && a.Groups[1].Length > 1)
                    {
                        var matchedValue = a.Groups[1].Value;
                        matches = matchedValue.Split(';');
                        Console.Write(matchedValue);


                    }
                    else { continue; }

                    if (matches.Length > 0)
                    {
                        KeyCell key = new KeyCell();
                        foreach (string match in matches)
                        {

                            if (match.StartsWith("format="))
                            {
                                key.Format = match.Replace("format=", "");
                            }
                            else if (match.StartsWith("formula="))
                            {
                                key.Formula = match.Replace("formula=", "");
                            }
                            //else if (match.Value.StartsWith("h_align="))
                            //{

                            //}
                            //else if (match.Value.StartsWith("v_align="))
                            //{

                            //}
                            else
                            {
                                key.Key = match;
                            }


                        }
                        key.RelativeRow = i + 1;
                        key.RelativeColumn = j + 1;


                        if (KeyCollection.ContainsKey(key.Key))
                        {
                            KeyCollection[key.Key].Add(key);
                        }
                        else
                        {
                            KeyCollection.Add(key.Key, new List<KeyCell>() { key });
                        }
                    }

                }

        }

        private void SetAddressValue(int pasteRow, int pasteColumn, Dictionary<(int relativeRow, int relativeColumn), SettingValue> list)
        {
            foreach (var item in list)
            {
                int absoluteRow = item.Key.relativeRow + pasteRow - 1;
                int absoluteColumn = item.Key.relativeColumn + pasteColumn - 1;

                SetValueToCell(pasteRow, pasteColumn, item.Value, item.Key.relativeRow, item.Key.relativeColumn, null);
            }
        }

        private void SetKeyValue(int pasteRow, int pasteColumn, Dictionary<string, SettingValue> list)
        {
            foreach (var item in list)
            {
                if (KeyCollection.ContainsKey(item.Key))
                {
                    var targetCells = KeyCollection[item.Key];
                    foreach (var targetCell in targetCells)
                    {
                        int absoluteRow = targetCell.RelativeRow + pasteRow - 1;
                        int absoluteColumn = targetCell.RelativeColumn + pasteColumn - 1;

                        SetValueToCell(pasteRow, pasteColumn, item.Value, targetCell.RelativeRow, targetCell.RelativeColumn, targetCell);
                    }
                }
            }
        }

        private void SetValueToCell(int pasteRow, int pasteColumn, SettingValue value, int relativeRow, int relativeColumn, KeyCell? cell)
        {
            int absoluteRow = relativeRow + pasteRow - 1;
            int absoluteColumn = relativeColumn + pasteColumn - 1;

            if (!string.IsNullOrEmpty(value.Format) || value.IsFormula || !string.IsNullOrEmpty(cell?.Format) || !string.IsNullOrEmpty(cell?.Formula))
            {

                string formula = "";
                if (!string.IsNullOrEmpty(cell?.Formula) && value.Value == TemplateFormulaValue.Value)
                {
                    formula = ResolveFormula(cell.Formula, pasteRow, pasteColumn, false);
                }
                else if (!string.IsNullOrEmpty(value.Formula))
                {
                    formula = ResolveFormula(value.Formula, pasteRow, pasteColumn, false);
                }

                using (var range = _sheet.GetRange(absoluteRow, absoluteColumn))
                {
                    if (!string.IsNullOrEmpty(formula))
                    {
                        if (!formula.StartsWith("="))
                        {
                            formula = formula.Insert(0, "=");
                        }

                        if (value.IsR1C1)
                        {
                            range.SetFormulaR1C1(formula);
                        }
                        else
                        {
                            range.SetFormula(formula);
                        }
                    }
                    else
                    {
                        range.Value = value.Value;
                        range.Value = null;
                    }


                    if (!string.IsNullOrEmpty(value.Format))
                    {
                        range.SetFormat(value.Format);
                    }
                    else if (!string.IsNullOrEmpty(cell?.Format))
                    {
                        range.SetFormat(cell.Format);
                    }

                }
            }
            else
            {
                _sheet.SetValue(absoluteRow, absoluteColumn, value.Value);
            }

        }

        public string ResolveFormula(string srcFormula, int pasteRow, int pasteColumn, bool r1c1)
        {
            var metches = Regex.Matches(srcFormula, formulaPattern);

            if (metches.Count > 0)
            {
                foreach (Match a in metches)
                {
                    if (a.Success && a.Groups.Count > 1 && a.Groups[1].Length > 1)
                    {
                        var matchedValue = a.Groups[1].Value;
                        if (!KeyCollection.ContainsKey(matchedValue)) { continue; }

                        var cell = KeyCollection[matchedValue].First();
                        var absoluteRowIndex = pasteRow + cell.RelativeRow - 1;
                        var absoluteColumnIndex = pasteColumn + cell.RelativeColumn - 1;

                        ExcelAddress add = new ExcelAddress(absoluteRowIndex, absoluteColumnIndex, absoluteRowIndex, absoluteColumnIndex);
                        srcFormula = Regex.Replace(srcFormula, a.Value, add.Address);

                    }
                }
            }
            return srcFormula;
        }

        public void Dispose()
        {
            _templateRange?.Dispose();
            if (isTemplateDelete)
            {
                _templateRange?.Delete((ShiftType ?? InsertShiftType.EntireRow).ToDeleteShiftType());
            }
        }
    }
}
