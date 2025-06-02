using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace FrostNova.Core.JsonData.JsonConverters
{
    public class TypeConverter : JsonConverter<Type>
    {
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeText = reader.GetString();
            return Type.GetType(typeText);

        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {

            string text = value.FullName;

            writer.WriteStringValue(text);
        }
    }
}
