using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using FrostNova.Core.TableWork;
using FrostNova.Core.TableWork.Excel;

namespace FrostNova.Core.TableWork.Excel.Tests;

/// <summary>
/// Round-trip tests for the ITableObject Excel extension methods, focused on the
/// row-append path of ImportFromExcel. Previously the import assigned the result of
/// AddRow() (an IEnumerable of the newly added rows) directly as the row instance,
/// which threw InvalidCastException inside SetValueFromExcel's (TModel)instance cast
/// whenever the sheet had more rows than the target table.
/// </summary>
public class ImportFromExcelTests
{
    private class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static ClassTableObject<Row> BuildTable(IList<Row> list)
    {
        var table = new ClassTableObject<Row>(list);
        table.TargetColumns.AddRange(new TableColumn[]
        {
            new TableColumn<Row, int>(x => x.Id) { HeaderText = "Id" },
            new TableColumn<Row, string>(x => x.Name) { HeaderText = "Name" },
        });
        return table;
    }

    [Fact]
    public void ImportFromExcel_appends_rows_when_target_is_empty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fn_import_{Guid.NewGuid():N}.xlsx");
        try
        {
            // --- write a 3-row sheet (with header) ---
            var source = BuildTable(new List<Row>
            {
                new() { Id = 1, Name = "Alice" },
                new() { Id = 2, Name = "Bob" },
                new() { Id = 3, Name = "Carol" },
            });
            source.OutputToExcel(path, "Meiryo UI", 11f, withHeader: true,
                Color.Black, Color.White, Color.Black, nullText: "", isBoldColumnText: false);

            Assert.True(File.Exists(path));

            // --- import into an empty, growable table ---
            var targetList = new List<Row>();
            var target = BuildTable(targetList);
            target.CanAddRow = true;

            target.ImportFromExcel(path, startRow: 0, sheetIndex: 1, hasHeader: true);

            Assert.Equal(3, targetList.Count);
            Assert.Equal(1, targetList[0].Id);
            Assert.Equal("Alice", targetList[0].Name);
            Assert.Equal(2, targetList[1].Id);
            Assert.Equal("Bob", targetList[1].Name);
            Assert.Equal(3, targetList[2].Id);
            Assert.Equal("Carol", targetList[2].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromExcel_fills_existing_rows_and_appends_the_rest()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fn_import_{Guid.NewGuid():N}.xlsx");
        try
        {
            var source = BuildTable(new List<Row>
            {
                new() { Id = 10, Name = "Ten" },
                new() { Id = 20, Name = "Twenty" },
                new() { Id = 30, Name = "Thirty" },
            });
            source.OutputToExcel(path, "Meiryo UI", 11f, withHeader: true,
                Color.Black, Color.White, Color.Black, nullText: "", isBoldColumnText: false);

            // one pre-existing row; the remaining two must be appended.
            var targetList = new List<Row> { new() { Id = -1, Name = "placeholder" } };
            var target = BuildTable(targetList);
            target.CanAddRow = true;

            target.ImportFromExcel(path, startRow: 0, sheetIndex: 1, hasHeader: true);

            Assert.Equal(3, targetList.Count);
            Assert.Equal(10, targetList[0].Id);
            Assert.Equal("Ten", targetList[0].Name);
            Assert.Equal(30, targetList[2].Id);
            Assert.Equal("Thirty", targetList[2].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
