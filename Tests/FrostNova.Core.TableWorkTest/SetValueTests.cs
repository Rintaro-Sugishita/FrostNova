using System.Collections.Generic;
using FrostNova.Core.TableWork;

namespace FrostNova.Core.TableWorkTest;

/// <summary>
/// Tests for writing back to row-instance properties via ClassTableObject.SetValue
/// (which routes through TableColumn.SetValueFromExcel -> the compiled setter built by
/// ExpressionHelper.CreateSetter).
/// </summary>
public class SetValueTests
{
    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Inner
    {
        public int Value { get; set; }
    }

    private class Holder
    {
        public Inner Inner { get; set; } = new Inner();
    }

    private class ReadOnlyModel
    {
        public int X { get; } = 5;
    }

    private static ClassTableObject<Person> BuildPersonTable(IList<Person> list)
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
    public void SetValue_writes_to_the_correct_row_property()
    {
        var list = new List<Person>
        {
            new() { Id = 1, Name = "old1" },
            new() { Id = 2, Name = "old2" },
        };
        var table = BuildPersonTable(list);

        table.SetValue(0, 0, 123);
        table.SetValue(0, 1, "new1");
        table.SetValue(1, 1, "new2");

        Assert.Equal(123, list[0].Id);
        Assert.Equal("new1", list[0].Name);
        Assert.Equal(2, list[1].Id);      // untouched
        Assert.Equal("new2", list[1].Name);
    }

    [Fact]
    public void SetValue_converts_incoming_value_to_the_property_type()
    {
        var list = new List<Person> { new() { Id = 0, Name = "" } };
        var table = BuildPersonTable(list);

        table.SetValue(0, 0, "456");   // string -> int
        Assert.Equal(456, list[0].Id);

        table.SetValue(0, 0, 7.0);     // double -> int
        Assert.Equal(7, list[0].Id);
    }

    [Fact]
    public void SetValue_supports_nested_property_paths()
    {
        var list = new List<Holder> { new() };
        var table = new ClassTableObject<Holder>(list);
        table.TargetColumns.Add(new TableColumn<Holder, int>(x => x.Inner.Value));

        table.SetValue(0, 0, 99);

        Assert.Equal(99, list[0].Inner.Value);
    }

    [Fact]
    public void SetValue_on_a_read_only_property_throws()
    {
        var list = new List<ReadOnlyModel> { new() };
        var table = new ClassTableObject<ReadOnlyModel>(list);
        table.TargetColumns.Add(new TableColumn<ReadOnlyModel, int>(x => x.X));

        // No setter -> CreateSetter returns null -> SetValueFromExcel throws.
        Assert.Throws<System.InvalidOperationException>(() => table.SetValue(0, 0, 10));
    }
}
