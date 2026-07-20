using System.Collections.Generic;
using FrostNova.Core.TableWork;

namespace FrostNova.Core.TableWorkTest;

/// <summary>
/// Regression tests for ClassTableObject.GetValues, which previously passed the
/// TableColumn itself (instead of the row instance) to GetRawValue.
/// </summary>
public class GetValuesTests
{
    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static ClassTableObject<Person> BuildTable(IList<Person> list)
    {
        var table = new ClassTableObject<Person>(list);
        table.TargetColumns.AddRange(new TableColumn[]
        {
            new TableColumn<Person, int>(x => x.Id),
            new TableColumn<Person, string>(x => x.Name),
        });
        return table;
    }

    [Fact]
    public void GetValues_returns_the_row_property_values_in_column_order()
    {
        var list = new List<Person>
        {
            new() { Id = 7, Name = "Alice" },
            new() { Id = 42, Name = "Bob" },
        };
        var table = BuildTable(list);

        var row0 = table.GetValues(0);
        Assert.Equal(2, row0.Length);
        Assert.Equal(7, row0[0]);
        Assert.Equal("Alice", row0[1]);

        var row1 = table.GetValues(1);
        Assert.Equal(42, row1[0]);
        Assert.Equal("Bob", row1[1]);
    }

    [Fact]
    public void GetValues_does_not_return_the_column_instances()
    {
        var table = BuildTable(new List<Person> { new() { Id = 1, Name = "X" } });

        var values = table.GetValues(0);

        // The bug returned TableColumn objects (or threw); make sure neither happens.
        Assert.All(values, v => Assert.IsNotType<TableColumn<Person, int>>(v));
        Assert.All(values, v => Assert.IsNotType<TableColumn<Person, string>>(v));
    }
}
