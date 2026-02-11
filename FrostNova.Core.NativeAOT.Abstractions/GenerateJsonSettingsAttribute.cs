using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// Specifies that methods for reading and writing settings in JSON format should be automatically generated.
    /// </summary>
    /// <remarks>
    /// When applied, a NativeAOT-compatible JsonSerializerContext is generated internally, and methods such as LoadFromFile and SaveToFile are added.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateJsonSettingsAttribute : Attribute
    {
        /// <summary>
        /// 用する JsonSerializerContext の型を指定します。
        /// Specifies the type of JsonSerializerContext to use.
        /// </summary>
        public Type? ContextType { get; }

        public Type[] Converters { get; }

        /// <summary>
        /// Specifies that methods for reading and writing settings in JSON format should be automatically generated.
        /// <param name="contextType">JsonSerializerContext types when NativeAOT</param>
        /// <param name="converters">json converter types when Reflection</param>
        /// </summary>
        public GenerateJsonSettingsAttribute(Type? contextType = null, params Type[] converters)
        {
            ContextType = contextType;
            Converters = converters;
        }
    }
}
