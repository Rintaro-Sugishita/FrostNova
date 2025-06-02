using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using FrostNova.Core.TypeToolkit;

namespace FrostNova.Core.JsonData.JsonConverters
{
    public class GuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeText = reader.GetString();
            return ConvertHelper.StrToAny<Guid>(typeText, Guid.Empty);
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {

            string guidText =value.ToString();

            writer.WriteStringValue(guidText);
        }
    }
}
