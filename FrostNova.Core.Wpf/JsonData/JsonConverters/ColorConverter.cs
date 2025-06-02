using FrostNova.Core.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.JsonData.JsonConverters
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeText = reader.GetString();
            return ColorTextConverter.ConvertBack(typeText ?? "");

        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {

            string ColorText = ColorTextConverter.Convert(value);

            writer.WriteStringValue(ColorText);
        }
    }
}
