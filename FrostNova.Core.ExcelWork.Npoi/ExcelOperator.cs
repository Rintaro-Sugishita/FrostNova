using System;
using System.IO;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Entry point for reading and writing Excel (.xlsx) files via NPOI (Apache-2.0).
    /// A drop-in-style alternative to the EPPlus-based <c>FrostNova.Core.ExcelWork.ExcelOperator</c>
    /// that carries no non-commercial licensing restriction.
    /// Open a workbook with a constructor or <see cref="Open"/>, access sheets through the returned
    /// <see cref="Sheet"/> objects, then call <see cref="Save"/> or <see cref="SaveAs"/>.
    /// Implements <see cref="IDisposable"/>; use in a <c>using</c> statement.
    /// </summary>
    public class ExcelOperator : IDisposable
    {
        // Excel 2007+ limits. Kept as literals so they can be used as default parameter values.
        public const int MAX_COLUMN = 16384;
        public const int MAX_ROW_COUNT = 1048576;

        private IWorkbook? _workbook;
        private StyleCache? _styles;
        private bool _autoCalculate = true;

        public string? FileName { get; private set; }

        internal IWorkbook Workbook => _workbook ?? throw new InvalidOperationException("The book is not opened.");
        internal StyleCache Styles => _styles ?? throw new InvalidOperationException("The book is not opened.");

        #region excel app

        public static bool IsExcelInstalled()
        {
            return Type.GetTypeFromProgID("Excel.Application") != null;
        }

        public static void OpenExcel(string fileName)
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
            };
            if (IsExcelInstalled())
            {
                processStartInfo.FileName = "EXCEL.exe";
                processStartInfo.Arguments = fileName;
            }
            else
            {
                processStartInfo.FileName = "EXPLORER.exe";
                processStartInfo.Arguments = Path.GetDirectoryName(fileName);
            }
            System.Diagnostics.Process.Start(processStartInfo);
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
                _workbook = new XSSFWorkbook();
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
                    using var fs = File.OpenRead(fileName);
                    _workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    throw new FileNotFoundException($"{fileName} is not found.");
                }
            }

            _styles = new StyleCache(_workbook);

            if (_workbook.NumberOfSheets == 0)
            {
                _workbook.CreateSheet(GenerateNewSheetName());
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
                throw new NotSupportedException("Password-protected save is not supported by the NPOI backend.");
            }

            PreSave();
            using var fs = File.Create(fileName);
            Workbook.Write(fs, false);
        }

        public void Dispose()
        {
            _workbook?.Close();
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
            return new Sheet(Workbook.CreateSheet(name), Styles);
        }

        public Sheet AddSheet(Sheet sheet)
        {
            var name = GenerateNewSheetName(sheet.Name);
            var index = Workbook.GetSheetIndex(sheet.GetSheetObject());
            var cloned = Workbook.CloneSheet(index);
            Workbook.SetSheetName(Workbook.GetSheetIndex(cloned), name);
            return new Sheet(cloned, Styles);
        }

        public Sheet CopySheet(string srcSheet)
        {
            var index = Workbook.GetSheetIndex(srcSheet);
            if (index < 0) throw new ArgumentException($"Sheet '{srcSheet}' was not found.", nameof(srcSheet));
            var cloned = Workbook.CloneSheet(index);
            Workbook.SetSheetName(Workbook.GetSheetIndex(cloned), GenerateNewSheetName(srcSheet));
            return new Sheet(cloned, Styles);
        }

        public void RemoveSheet(string sheetName)
        {
            var index = Workbook.GetSheetIndex(sheetName);
            if (index >= 0) Workbook.RemoveSheetAt(index);
        }

        public string[] GetSheetNames()
        {
            return Enumerable.Range(0, Workbook.NumberOfSheets).Select(Workbook.GetSheetName).ToArray();
        }

        public Sheet GetSheet(string sheetName)
        {
            var sheet = Workbook.GetSheet(sheetName) ?? throw new ArgumentException($"Sheet '{sheetName}' was not found.", nameof(sheetName));
            return new Sheet(sheet, Styles);
        }

        /// <summary>Get sheet by 1-based index.</summary>
        public Sheet GetSheet(int sheetIndex)
        {
            return new Sheet(Workbook.GetSheetAt(sheetIndex - 1), Styles);
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
            return Workbook.GetSheetIndex(sheetName) >= 0;
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
            try
            {
                XSSFFormulaEvaluator.EvaluateAllFormulaCells(Workbook);
            }
            catch
            {
                // Some functions may be unsupported by NPOI's evaluator; defer to Excel on open.
                for (int i = 0; i < Workbook.NumberOfSheets; i++)
                {
                    Workbook.GetSheetAt(i).ForceFormulaRecalculation = true;
                }
            }
        }

        #endregion
    }
}
