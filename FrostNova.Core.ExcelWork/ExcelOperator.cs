using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using OfficeOpenXml;

namespace FrostNova.Core.ExcelWork
{
    public class ExcelOperator : IDisposable
    {

        static ExcelOperator()
        {
            ExcelPackage.License.SetNonCommercialPersonal("r-sugishita");       
        }



        ExcelPackage? package;
        ExcelWorkbook? book;
        ExcelWorksheets? sheets;

        public const int MAX_COLUMN = ExcelPackage.MaxColumns;
        public const int MAX_ROW_COUNT = ExcelPackage.MaxRows;

        public string? FileName
        {
            get; private set;
        }

        #region excel app

        public static bool IsExcelInstalled()
        {
            return Type.GetTypeFromProgID("Excel.Application") != null;
        }

        public static void OpenExcel(string fileName)
        {
            if (IsExcelInstalled())
            {
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
                processStartInfo.FileName = "EXCEL.exe";
                processStartInfo.UseShellExecute = true;
                processStartInfo.Arguments = fileName;

                System.Diagnostics.Process.Start(processStartInfo);
            }
            else
            {
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo();
                processStartInfo.FileName = "EXPLORER.exe";
                processStartInfo.UseShellExecute = true;
                processStartInfo.Arguments = System.IO.Path.GetDirectoryName(fileName);

                System.Diagnostics.Process.Start(processStartInfo);

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
                package = new ExcelPackage();
            }
            else
            {
                if (!System.IO.Path.IsPathRooted(fileName))
                {
                    fileName = PathHelpers.PathHelper.GetAbsolutePath(fileName);
                }

                if (System.IO.File.Exists(fileName))
                {
                    FileName = fileName;
                    package = new ExcelPackage(fileName);
                    Console.WriteLine($"opened: {fileName}");
                }
                else
                {
                    throw new System.IO.FileNotFoundException($"{fileName} is not found.");
                }
            }

            book = package.Workbook;
            sheets = book.Worksheets;

            if (sheets.Count == 0)
            {
                sheets.Add(GenerateNewSheetName());
            }

        }

        public void Save(string password = "")
        {
            PreSave();
            if (string.IsNullOrEmpty(password))
            {
                package?.Save();
            }
            else
            {
                package?.Save(password);
            }
        }

        public void SaveAs(string fileName, string password = "")
        {
            PreSave();
            if (string.IsNullOrEmpty(password))
            {
                package?.SaveAs(fileName);
            }
            else
            {
                package?.SaveAs(fileName, password);
            }
        }

        public void Dispose()
        {
            sheets?.Dispose();

            book?.Dispose();

            package?.Dispose();
        }

        private void PreSave()
        {
            if (sheets == null) return;
            foreach (var item in sheets)
            {
                item.Calculate();
            }
        }

        #endregion

        #region sheet


        public Sheet AddSheet(string sheetName = "")
        {
            if (sheets == null) throw new Exception("The book is not opened.");
            if (!string.IsNullOrEmpty(sheetName))
            {
                if (IsDuplicatedSheetName(sheetName))
                {
                    throw new Exception($"The sheet name '{sheetName}' is duplicated.");
                }
                return new Sheet(sheets.Add(sheetName));
            }
            else
            {
                return new Sheet(sheets.Add(GenerateNewSheetName()));
            }
        }

        public Sheet AddSheet(Sheet sheet)
        {
            if (sheets == null) throw new Exception("The book is not opened.");
            var sheetObject = sheet.GetSheetObject();
            if (IsDuplicatedSheetName(sheetObject.Name))
            {
                return new Sheet(sheets.Add(GenerateNewSheetName(sheetObject.Name), sheetObject));
            }
            else
            {
                return new Sheet(sheets.Add(sheetObject.Name, sheetObject));
            }
        }


        public Sheet CopySheet(string srcSheet)
        {
            if (sheets == null) throw new Exception("The book is not opened.");
            return new Sheet(sheets.Copy(srcSheet, GenerateNewSheetName(srcSheet)));
        }

        public void RemoveSheet(string sheetName)
        {
            sheets?.Delete(sheetName);
        }

        public string[] GetSheetNames()
        {
            if (sheets == null) throw new Exception("The book is not opened.");

            return sheets.Select(x => x.Name).ToArray();
        }

        public Sheet GetSheet(string sheetName)
        {
            if (sheets == null) throw new Exception("The book is not opened.");
            return new Sheet(sheets[sheetName]);
        }

        /// <summary>
        /// Get sheet.
        /// </summary>
        /// <param name="sheetIndex">1 based sheet index</param>
        /// <returns></returns>
        public Sheet GetSheet(int sheetIndex)
        {
            if (sheets == null) throw new Exception("The book is not opened.");

            return new Sheet(sheets[sheetIndex - 1]);
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
            if (sheets == null) throw new Exception("The book is not opened.");
            return sheets.Any(x => x.Name == sheetName);
        }

        #endregion

        #region book

        public bool IsAutoCalculate
        {
            get { return (book?.CalcMode == ExcelCalcMode.Automatic); }
            set
            {
                if (book == null) return;
                if (value)
                {
                    book.CalcMode = ExcelCalcMode.Automatic;
                }
                else
                {
                    book.CalcMode = ExcelCalcMode.Manual;
                }
            }
        }

        public void Calculate()
        {
            book.Calculate();
        }

        #endregion
    }
}
