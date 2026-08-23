using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Entry point for reading and writing Excel (.xlsx) files via ClosedXML (MIT).
    /// A cross-platform, drop-in-style alternative to the NPOI- and EPPlus-based ExcelOperator
    /// with an identical public surface and no non-commercial licensing restriction.
    /// Open a workbook with a constructor or <see cref="Open"/>, access sheets through the returned
    /// <see cref="Sheet"/> objects, then call <see cref="Save"/> or <see cref="SaveAs"/>.
    /// Implements <see cref="IDisposable"/>; use in a <c>using</c> statement.
    /// </summary>
    public class ExcelOperator : IDisposable
    {
        // Excel 2007+ limits. Kept as literals so they can be used as default parameter values.
        public const int MAX_COLUMN = 16384;
        public const int MAX_ROW_COUNT = 1048576;

        private XLWorkbook? _workbook;
        private StyleCache? _styles;
        private bool _autoCalculate = true;

        public string? FileName { get; private set; }

        internal XLWorkbook Workbook => _workbook ?? throw new InvalidOperationException("The book is not opened.");
        internal StyleCache Styles => _styles ?? throw new InvalidOperationException("The book is not opened.");

        #region excel app

        public static bool IsExcelInstalled()
        {
            // COM/ProgID probing is only meaningful on Windows; elsewhere assume not installed.
            if (!OperatingSystem.IsWindows()) return false;
            return Type.GetTypeFromProgID("Excel.Application") != null;
        }

        public static void OpenExcel(string fileName)
        {
            // Delegate to the OS. On Windows this opens Excel (or Explorer); on other platforms
            // it hands the file to the default handler without throwing.
            try
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                };

                if (OperatingSystem.IsWindows() && IsExcelInstalled())
                {
                    processStartInfo.FileName = "EXCEL.exe";
                    processStartInfo.Arguments = fileName;
                }
                else
                {
                    processStartInfo.FileName = fileName;
                }
                System.Diagnostics.Process.Start(processStartInfo);
            }
            catch
            {
                // Opening in an external viewer is best-effort; never fault the caller.
            }
        }

        #endregion

        #region open and save

        public ExcelOperator()
        {
        }

        public ExcelOperator(string fileName)
        {
            Open(fileName);
        }

        public void Open(string fileName = "")
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _workbook = new XLWorkbook();
            }
            else
            {
                if (!Path.IsPathRooted(fileName))
                {
                    fileName = Path.GetFullPath(fileName);
                }

                if (File.Exists(fileName))
                {
                    FileName = fileName;
                    // ClosedXML keeps its own handle to the source (needed to copy parts it does not
                    // model when saving), and releases it on Dispose. The path constructor also opens
                    // .xltx templates. Saving to a different path is unaffected.
                    _workbook = new XLWorkbook(fileName);
                }
                else
                {
                    throw new FileNotFoundException($"{fileName} is not found.");
                }
            }

            _styles = new StyleCache(_workbook);

            if (!_workbook.Worksheets.Any())
            {
                _workbook.Worksheets.Add(GenerateNewSheetName());
            }
        }

        public void Save(string password = "")
        {
            if (FileName == null)
            {
                throw new InvalidOperationException("This workbook was not opened from a file. Use SaveAs(fileName).");
            }
            SaveAs(FileName, password);
        }

        public void SaveAs(string fileName, string password = "")
        {
            if (!string.IsNullOrEmpty(password))
            {
                throw new NotSupportedException("Password-protected save is not supported by the ClosedXML backend.");
            }

            PreSave();
            // Do not evaluate formulas at save time. ClosedXML's evaluator is incomplete (e.g. VLOOKUP
            // over structured table references), and baking an error value such as #N/A into the file is
            // worse than leaving the formula uncached for the host to recalculate. Cells written as
            // formulas therefore carry no cached value and Excel/LibreOffice recompute them on open.
            Workbook.SaveAs(fileName, new SaveOptions { EvaluateFormulasBeforeSaving = false });
        }

        public void Dispose()
        {
            _workbook?.Dispose();
            _workbook = null;
        }

        private void PreSave()
        {
            if (_autoCalculate)
            {
                Calculate();
            }
        }

        #endregion

        #region sheet

        public Sheet AddSheet(string sheetName = "")
        {
            var name = string.IsNullOrEmpty(sheetName) ? GenerateNewSheetName() : sheetName;
            if (IsDuplicatedSheetName(name))
            {
                throw new Exception($"The sheet name '{name}' is duplicated.");
            }
            return new Sheet(Workbook.Worksheets.Add(name), Styles);
        }

        public Sheet AddSheet(Sheet sheet)
        {
            var name = GenerateNewSheetName(sheet.Name);
            var cloned = sheet.GetSheetObject().CopyTo(name);
            return new Sheet(cloned, Styles);
        }

        public Sheet CopySheet(string srcSheet)
        {
            if (!Workbook.Worksheets.TryGetWorksheet(srcSheet, out var ws))
            {
                throw new ArgumentException($"Sheet '{srcSheet}' was not found.", nameof(srcSheet));
            }
            var cloned = ws.CopyTo(GenerateNewSheetName(srcSheet));
            return new Sheet(cloned, Styles);
        }

        public void RemoveSheet(string sheetName)
        {
            if (Workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
            {
                ws.Delete();
            }
        }

        public string[] GetSheetNames()
        {
            return Workbook.Worksheets.Select(ws => ws.Name).ToArray();
        }

        public Sheet GetSheet(string sheetName)
        {
            if (!Workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
            {
                throw new ArgumentException($"Sheet '{sheetName}' was not found.", nameof(sheetName));
            }
            return new Sheet(ws, Styles);
        }

        /// <summary>Get sheet by 1-based index.</summary>
        public Sheet GetSheet(int sheetIndex)
        {
            return new Sheet(Workbook.Worksheet(sheetIndex), Styles);
        }

        #endregion

        #region naming

        private int sheetNameIndex = 1;

        private string GenerateNewSheetName(string baseSheetName = "")
        {
            while (true)
            {
                string sheetName = string.IsNullOrEmpty(baseSheetName) ? $"sheet{sheetNameIndex}" : baseSheetName + sheetNameIndex;
                if (!IsDuplicatedSheetName(sheetName))
                {
                    return sheetName;
                }
                sheetNameIndex++;
            }
        }

        private bool IsDuplicatedSheetName(string sheetName)
        {
            return Workbook.Worksheets.TryGetWorksheet(sheetName, out _);
        }

        #endregion

        #region book

        public bool IsAutoCalculate
        {
            get => _autoCalculate;
            set => _autoCalculate = value;
        }

        public void Calculate()
        {
            // Defer calculation to the host application instead of baking values here.
            // ClosedXML's formula engine is incomplete (e.g. VLOOKUP over structured table references
            // resolves to #N/A), so eagerly recalculating would cache wrong values. Formulas are read
            // lazily when needed and saved uncached (see SaveAs), and Excel recalculates them on open.
            Workbook.CalculateMode = XLCalculateMode.Auto;
        }

        #endregion
    }
}
