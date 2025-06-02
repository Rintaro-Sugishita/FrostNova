using FrostNova.Core.Common;
using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.JsonData.JsonConverters
{
    public class BindableStructConverter<T> : JsonConverter<BindableStruct<T>> where T : struct
    {
        public override BindableStruct<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeText = reader.GetString();
            return new BindableStruct<T>(FrostNova.Core.TypeToolkit.ConvertHelper.ConvToAny<T>(typeText));

        }

        public override void Write(Utf8JsonWriter writer, BindableStruct<T> value, JsonSerializerOptions options)
        {

            string ColorText = value?.Value.ToString() ?? "";

            writer.WriteStringValue(ColorText);
        }
    }
}
