using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace FrostNova.Core.JsonData
{
    public static class JsonDataHelper
    {
        public static T? LoadFromJsonText<T>(string? text) where T : class, new() 
        {
            if (text == null) { return null; }
            return JsonSerializer.Deserialize<T>(text?? "");
        }

        public static object? LoadFromJsonText(string? text, Type type)
        {
            if (text == null) { return null; }
            return JsonSerializer.Deserialize(text, type);
        }

        public static T? LoadFromFile<T>(string path) where T: class, new() 
        {
            if (!File.Exists(path)) { return null; }

            var jsonText = File.ReadAllText(path);

            return LoadFromJsonText<T>(jsonText);
        }

        public static object LoadFromFile(string path, Type type)
        {

            if (type == null) throw new ArgumentException();

            if (type.FullName == null) throw new ArgumentException();

            if (!File.Exists(path))
            {

                return type.Assembly.CreateInstance(type.FullName)!;
            }

            var jsonText = File.ReadAllText(path);

            return LoadFromJsonText(jsonText, type) ?? type.Assembly.CreateInstance(type.FullName)!;

        }

        public static string ToJsonText<T>(T data, bool isFormat = false)
        {
            string jsonText = JsonSerializer.Serialize(data, typeof(T));
            jsonText = FormatJson(jsonText, isFormat);
            return jsonText;

        }
        public static string ToJsonText(object data, Type type, bool isFormat = false)
        {
            string jsonText = JsonSerializer.Serialize(data, type);
            jsonText = FormatJson(jsonText, isFormat);
            return jsonText;
        }

        public static void SaveToFile<T>(string path, T data, bool isFormat = false)
        {
            string? folder =  Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder)) return;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string jsonText = ToJsonText(data, isFormat);
            File.WriteAllText(path, jsonText);

        }

        public static void SaveToFile(string path, object data, Type type, bool isFormat = false)
        {
            string? folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder)) return;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string jsonText = ToJsonText(data, isFormat);
            File.WriteAllText(path, jsonText);

        }




        public static string FormatJson(string jsonText, bool doesIndent = true)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(jsonText);
            using (MemoryStream stream = new MemoryStream())
            using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, indent: doesIndent))
            using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(buffer, XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteNode(reader, true);
                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }

        }



    }
}
