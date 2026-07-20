using System;
using System.Linq.Expressions;
using FrostNova.Core.Expressions;

namespace FrostNova.CoreTest
{
    /// <summary>
    /// Regression tests for ExpressionHelper.SetValue(object, LambdaExpression, object?),
    /// which previously computed a converted value but then assigned the original
    /// (unconverted) value, discarding the conversion.
    /// </summary>
    public class ExpressionHelperSetValueTests
    {
        private class Inner
        {
            public int Age { get; set; }
        }

        private class Model
        {
            public int Age { get; set; }
            public string Name { get; set; } = "";
            public Inner Inner { get; set; } = new Inner();
        }

        [Fact]
        public void SetValue_converts_string_to_int_property()
        {
            var model = new Model();
            Expression<Func<Model, int>> selector = x => x.Age;

            // value type (string) is not assignable to int -> must be converted.
            ExpressionHelper.SetValue(model, selector, "42");

            Assert.Equal(42, model.Age);
        }

        [Fact]
        public void SetValue_converts_for_nested_property_path()
        {
            var model = new Model();
            Expression<Func<Model, int>> selector = x => x.Inner.Age;

            ExpressionHelper.SetValue(model, selector, "123");

            Assert.Equal(123, model.Inner.Age);
        }

        [Fact]
        public void SetValue_assigns_directly_when_type_already_matches()
        {
            var model = new Model();
            Expression<Func<Model, string>> selector = x => x.Name;

            ExpressionHelper.SetValue(model, selector, "hello");

            Assert.Equal("hello", model.Name);
        }
    }
}
