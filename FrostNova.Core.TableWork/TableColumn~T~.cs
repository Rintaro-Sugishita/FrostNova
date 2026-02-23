using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FrostNova.Core.Expressions;
using FrostNova.Core.TypeToolkit;

namespace FrostNova.Core.TableWork
{

    public class TableColumn<TModel, TProperty> : TableColumn
    {
        public override LambdaExpression PropertySelector => TypedSelector;
        public Expression<Func<TModel, TProperty?>> TypedSelector { get; }
        private readonly Func<TModel, TProperty?> _compiledGetter;
        private readonly Action<TModel, TProperty?>? _compiledSetter;
        public override Type PropertyType => typeof(TProperty);
        public TableColumn(Expression<Func<TModel, TProperty?>> selector)
        {
            TypedSelector = selector;
            HeaderText = string.Empty;
            _compiledGetter = selector.Compile();
            _compiledSetter = ExpressionHelper.CreateSetter(selector);
        }


        public TableColumn(Expression<Func<TModel, TProperty?>> selector, string headerText = "", string? format = null
            , Func<TProperty?, string?>? converter = null, Func<string?, TProperty?>? backConverter = null
            , Func<TProperty?, object?>? excelConverter = null, Func<object?, TProperty?>? excelBackConverter = null
            )
        {
            TypedSelector = selector;
            HeaderText = headerText;
            Format = format;
            Converter = converter;
            BackConverter = backConverter;
            ExcelConverter = excelConverter;
            ExcelBackConverter = excelBackConverter;
            _compiledGetter = selector.Compile();
            _compiledSetter = ExpressionHelper.CreateSetter(selector);
        }


        public Func<TProperty?, string?>? Converter { get; set; } = null;
        public Func<string?, TProperty?>? BackConverter { get; set; } = null;

        public Func<TProperty?, object?>? ExcelConverter { get; set; } = null;
        public Func<object?, TProperty?>? ExcelBackConverter { get; set; } = null;

        public override object? GetRawValue(object instance)
                => _compiledGetter((TModel)instance);

        public override string? GetTextValue(object instance)
        {
            var raw = (TProperty?)GetRawValue(instance);
            if (Converter != null) return Converter(raw);

            // Converterがない場合は Format を適用して文字列化
            if (raw is IFormattable f && !string.IsNullOrEmpty(Format))
                return f.ToString(Format, null);

            return raw?.ToString();
        }

        public override object? GetExcelValue(object instance)
        {
            var raw = (TProperty?)GetRawValue(instance);
            return ExcelConverter != null ? ExcelConverter(raw) : raw;
        }

        // --- 設定系 ---
        private Exception CreateReadOnlyException() =>
    new InvalidOperationException($"The column '{HeaderText}' is read-only or has no setter.");
        public override void SetValueFromText(object instance, string? value)
        {

            if (_compiledSetter == null) throw CreateReadOnlyException();

            // Text専用のBackConverterがあれば適用、なければ共通パース
            TProperty? valToSet = BackConverter != null
                ? BackConverter(value)
                : ParseValue(value);

            _compiledSetter((TModel)instance, valToSet);
        }

        public override void SetValueFromExcel(object instance, object? value)
        {
            if (_compiledSetter == null) throw CreateReadOnlyException();

            // Excel専用のExcelBackConverterがあれば適用、なければ共通パース
            TProperty? valToSet = ExcelBackConverter != null
                ? ExcelBackConverter(value)
                : ParseValue(value);

            _compiledSetter((TModel)instance, valToSet);
        }


        private TProperty? ParseValue(object? value)
        {
            // 1. 基本的な null チェック
            if (value == null || value == DBNull.Value) return default;

            // 2. すでに目的の型である場合は、変換不要（Excelからの読み込みなどで多い）
            if (value is TProperty typedValue) return typedValue;

            // 3. 文字列の場合、Format を考慮してパースを試みる
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                var targetType = typeof(TProperty);
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // DateTime の特別処理
                if (underlyingType == typeof(DateTime) && !string.IsNullOrEmpty(Format))
                {
                    if (DateTime.TryParseExact(stringValue, Format, null, System.Globalization.DateTimeStyles.None, out var dt))
                        return (TProperty?)(object)dt;
                }
                // TimeSpan の特別処理
                else if (underlyingType == typeof(TimeSpan) && !string.IsNullOrEmpty(Format))
                {
                    if (TimeSpan.TryParseExact(stringValue, Format, null, out var ts))
                        return (TProperty?)(object)ts;
                }
                // 数値型の特別処理（カンマや通貨記号を許容）
                else if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(int))
                {
                    if (decimal.TryParse(stringValue, System.Globalization.NumberStyles.Any, null, out var num))
                        return ConvertHelper.ConvToAnyNull<TProperty>(num);
                }
            }

            // 4. 最終手段：標準のコンバーターに任せる
            return ConvertHelper.ConvToAnyNull<TProperty>(value);
        }



    }
}
