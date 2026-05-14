using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// Provides a source generator that creates strongly-typed JSON serialization and settings file I/O methods for
    /// classes annotated with a specific attribute.
    /// </summary>
    /// <remarks><para> <b>JsonSettingsGenerator</b> scans for classes marked with the
    /// <c>GenerateJsonSettingsAttribute</c> and generates partial class code that includes: </para> <list
    /// type="bullet">   <item>JSON serialization and deserialization methods using 
    /// System.Text.Json" </item><item>Convenience methods for reading from and writing to settings files,
    /// both synchronously and asynchronously.</item>   <item>Support for custom default file paths via a static method
    /// annotated with <c>JsonSettingPathAttribute</c> (if present).</item> </list> <para> The generated code is intended to
    /// simplify working with settings or configuration objects in .NET projects, especially when targeting NativeAOT or
    /// requiring source-generated JSON contexts. </para> <para> To use, annotate a class with
    /// <c>GenerateJsonSettingsAttribute</c>. Optionally, provide a static method with <c>JsonSettingPathAttribute</c> to
    /// specify a default file path for settings persistence. </para> <para> This generator is designed for use with C#
    /// source generators and requires the <see cref="Microsoft.CodeAnalysis"/> infrastructure. </para></remarks>
    [Generator(LanguageNames.CSharp)]
    public class JsonSettingsGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GenerateJsonSettingsAttribute";
        private const string PATH_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.JsonSettingPathAttribute";

        private const string DEFAULT_NOTINDENTED_OPTION_NAME = "FrostNova.Core.NativeAOT.DefaultOptions.NotIndentedDefault";
        private const string DEFAULT_INDENTED_OPTION_NAME = "FrostNova.Core.NativeAOT.DefaultOptions.IndentedDefault";

        #region Diagnostic Rules

        private static readonly DiagnosticDescriptor StaticMethodRule = new DiagnosticDescriptor(
            id: "FNJS001",
            title: "GetSavePath method must be static",
            messageFormat: "The method '{0}' marked with [JsonSettingPath] must be static",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor NoParametersRule = new DiagnosticDescriptor(
            id: "FNJS002",
            title: "GetSavePath method must have no parameters",
            messageFormat: "The method '{0}' marked with [JsonSettingPath] must not have any parameters",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ReturnTypeRule = new DiagnosticDescriptor(
            id: "FNJS003",
            title: "GetSavePath method must return string",
            messageFormat: "The method '{0}' marked with [JsonSettingPath] must return 'string'",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        #endregion

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();

            var version = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
            {
                var assembly = typeof(JsonSettingsGenerator).Assembly;
                return assembly.GetName().Version?.ToString() ?? "1.0.0";
            });

            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, _) => GetSemanticTarget(ctx))
                .Where(m => m.Symbol is not null);

            var combined = classDeclarations.Combine(version);

            context.RegisterSourceOutput(combined, (spc, source) => Execute(spc, source.Left, source.Right));
        }

        private static (INamedTypeSymbol? Symbol, bool isNullable, INamedTypeSymbol? ContextType, string[] customConverters) GetSemanticTarget(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (symbol != null)
            {
                var attr = symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == ATTRIBUTE_NAME);
                if (attr != null)
                {
                    var contextTypeSymbol = attr.ConstructorArguments.Length > 0
                        ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
                        : null;

                    var converters = new List<string>();
                    if (attr.ConstructorArguments.Length > 1)
                    {
                        var typedConstants = attr.ConstructorArguments[1].Values;
                        foreach (var constant in typedConstants)
                        {
                            if (constant.Value is INamedTypeSymbol converterSymbol)
                            {
                                converters.Add(converterSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            }
                        }
                    }

                    var compilation = context.SemanticModel.Compilation;
                    var projectNullable = compilation.Options.NullableContextOptions;
                    var nullableContextAtLocation = context.SemanticModel.GetNullableContext(classDeclaration.GetLocation().SourceSpan.Start);

                    bool useNullable = nullableContextAtLocation.HasFlag(NullableContext.AnnotationsEnabled) ||
                                       projectNullable.HasFlag(NullableContextOptions.Annotations);

                    return (symbol, useNullable, contextTypeSymbol, converters.ToArray());
                }
            }
            return (null, false, null, []);
        }

        private static void Execute(SourceProductionContext context, (INamedTypeSymbol? Symbol, bool isNullable, INamedTypeSymbol? ContextType, string[] customConverters) target, string version)
        {
            var classSymbol = target.Symbol!;


            //JsonSerializerContextの型名
            var contextTypeName = target.ContextType?.ToDisplayString();
            var serializableTypes = new List<ITypeSymbol>();

            //JsonSerializableAttributeで指定された型を収集
            //ここでSymbolと同じ型をGeneric引数に持つJsonSerializerContextと、Symbolの配列を対象にする
            if (target.ContextType != null)
            {
                foreach (var attr in target.ContextType.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "JsonSerializableAttribute")
                    {
                        var typeSymbol = attr.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
                        if (typeSymbol == null) continue;


                        // 配列かつ要素型が一致するか (Class1[])
                        bool isArrayOfTarget = typeSymbol is IArrayTypeSymbol arrayType &&
                                               SymbolEqualityComparer.Default.Equals(arrayType.ElementType, classSymbol);

                        // ジェネリックなコレクションかつ、第1引数が一致するか (List<Class1>, IEnumerable<Class1> 等)
                        bool isGenericCollectionOfTarget = typeSymbol is INamedTypeSymbol namedType &&
                                                           namedType.IsGenericType &&
                                                           namedType.TypeArguments.Length == 1 &&
                                                           namedType.TypeArguments.Any(t => SymbolEqualityComparer.Default.Equals(t, classSymbol));

                        if (isArrayOfTarget || isGenericCollectionOfTarget)
                            serializableTypes.Add(typeSymbol);

                    }
                }
            }
            var allPathMethods = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == PATH_ATTRIBUTE_NAME))
                .ToList();

            IMethodSymbol? validMethod = null;
            foreach (var method in allPathMethods)
            {
                bool hasError = false;
                if (!method.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(StaticMethodRule, method.Locations[0], method.Name));
                    hasError = true;
                }
                if (method.Parameters.Length > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NoParametersRule, method.Locations[0], method.Name));
                    hasError = true;
                }
                if (method.ReturnType.SpecialType != SpecialType.System_String)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ReturnTypeRule, method.Locations[0], method.Name));
                    hasError = true;
                }
                if (!hasError)
                {
                    validMethod = method;
                    break;
                }
            }

            var source = ProcessClass(classSymbol, target.isNullable, target.ContextType, serializableTypes, validMethod?.Name, target.customConverters, version);
            var hintName = classSymbol.ToDisplayString().Replace(".", "_") + ".JsonSettings.g.cs";
            context.AddSource(hintName, source);
        }

        private static string ProcessClass(INamedTypeSymbol classSymbol, bool isNullable, INamedTypeSymbol? contextType, List<ITypeSymbol> contextTargetTypes, string? getPathMethodName, string[] customConverters, string version)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var accessibility = GetAccessibilityText(classSymbol.DeclaredAccessibility);
            var className = classSymbol.Name;
            var contextTypeName = contextType?.ToDisplayString();
            var q = isNullable ? "?" : string.Empty;
            var genAttr = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"FrostNova.Core.NativeAOT.JsonSettingsGenerator\", \"{version}\")]";


            var interfaceDeclaration = $" : FrostNova.Core.NativeAOT.IJsonSerializable<{className}>";
            var interfaceDeclaration2 = $" , FrostNova.Core.NativeAOT.IJsonStore<{className}>";


            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            if (isNullable) sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Diagnostics;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");


            if (getPathMethodName != null)
            {
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    partial class {className}{interfaceDeclaration}{interfaceDeclaration2}");
                sb.AppendLine("#else");
                sb.AppendLine($"    partial class {className}");
                sb.AppendLine("#endif");

            }
            else
            {
                sb.AppendLine("#if NET8_0_OR_GREATER");
                sb.AppendLine($"    partial class {className}{interfaceDeclaration}");
                sb.AppendLine("#else");
                sb.AppendLine($"    partial class {className}");
                sb.AppendLine("#endif");

            }


            sb.AppendLine("    {");

            string converterInstances = string.Join(", ", customConverters.Select(c => $"new {c}()"));
            if (!string.IsNullOrEmpty(converterInstances)) converterInstances = ", " + converterInstances;


            // 1. JsonTypeInfo を解決するためのプロパティを生成
            // 属性で指定されたコンテキスト（例: MyJsonContext）から対象クラスの情報を取得します
            //if (contextTypeName != null)
            //{
            //    sb.AppendLine($@"        /// <summary>Gets the JSON type metadata required for NativeAOT.</summary>");
            //    sb.AppendLine($@"        private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<{className}> TypeInfo => {contextTypeName}.Default.{className};");
            //}
            //else
            //{
            //    // コンテキストが指定されていない場合のフォールバック（限定的ですが、リフレクションベースのOptionsを使用）
            //    sb.AppendLine($@"        private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<{className}> TypeInfo => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<{className}>){option2}.GetTypeInfo(typeof({className}));");
            //}

            // --- Fields (Internal for Extensions) ---
            if (contextType != null)
            {
                sb.AppendLine($@"        {genAttr}
        internal static readonly JsonSerializerOptions _indentOptions = new JsonSerializerOptions {{ 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = {{ new JsonStringEnumConverter(){converterInstances} }}
        }};
        {genAttr}
        internal static readonly {contextTypeName} _indentedContext = new {contextTypeName}(_indentOptions);
        {genAttr}
        internal static readonly {contextTypeName} _defaultContext = {contextTypeName}.Default;");
            }
            else
            {

                if (customConverters.Length > 0)
                {
                    sb.AppendLine($"        {genAttr}");
                    sb.AppendLine("        internal static readonly JsonSerializerOptions _specificIndentedOptions = new JsonSerializerOptions");
                    sb.AppendLine("        {");
                    sb.AppendLine("            WriteIndented = true,");
                    sb.AppendLine("            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,");
                    sb.AppendLine($"            Converters = {{ new global::System.Text.Json.Serialization.JsonStringEnumConverter(){converterInstances} }} // 必要に応じて動的に追加");
                    sb.AppendLine("        };");
                    sb.AppendLine("");

                    sb.AppendLine($"        {genAttr}");
                    sb.AppendLine("        internal static readonly JsonSerializerOptions _specificOptions = new JsonSerializerOptions");
                    sb.AppendLine("        {");
                    sb.AppendLine("            WriteIndented = false,");
                    sb.AppendLine("            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,");
                    sb.AppendLine($"            Converters = {{ new global::System.Text.Json.Serialization.JsonStringEnumConverter(){converterInstances} }} // 必要に応じて動的に追加");
                    sb.AppendLine("        };");

                }



            }

            // --- Serialization Methods ---

            // 共通して利用する TypeInfo の参照先を決定
            string typeInfoSource;
            string genericText;
            if (contextTypeName != null)
            {
                // Contextがある場合は、既に生成済みの _indentedContext から取得
                typeInfoSource = $"_indentedContext.{className}";
                genericText = "";
            }
            else
            {
                // Contextがない場合は、Optionsから動的に取得（リフレクションベース）
                var currentOption = customConverters.Any() ? $"{className}._specificIndentedOptions" : DEFAULT_INDENTED_OPTION_NAME;
                typeInfoSource = $"{currentOption}";
                genericText = $"<{className}>";
            }

            // 共通の FromJson (ReadOnlySpan版)
            sb.AppendLine($@"
        {genAttr}
        public static {className}{q} FromJson(ReadOnlySpan<char> json)
        {{
            if (json.IsWhiteSpace()) return null;
            try {{
                var a = JsonSerializer.Deserialize{genericText}(json, {typeInfoSource});
                a?.AfterDeserialize();
                return a;
            }} catch {{ return null; }}
        }}

        {genAttr}
        public static {className}{q} FromJson(ReadOnlySpan<byte> utf8Json)
        {{
            if (utf8Json.Length == 0) return null;
            try {{
                var a = JsonSerializer.Deserialize{genericText}(utf8Json, {typeInfoSource});
                a?.AfterDeserialize();
                return a;
            }} catch {{ return null; }}
        }}");

            // string版の FromJson は共通して Span版を呼ぶ
            sb.AppendLine($@"
        {genAttr}
        public static {className}{q} FromJson(string json) => FromJson(json.AsSpan());");


            // --- Serialization Methods (ToJson部分) ---

            if (contextTypeName != null)
            {
                // Case 1: Context (NativeAOT) が指定されている場合
                // 引数の isIndented に応じて、インデントあり用とデフォルト用の Context から TypeInfo を選択
                sb.AppendLine($@"
        /// <summary>Serializes the current instance to a JSON string.</summary>
        {genAttr}
        public string ToJson(bool isIndented = true)
        {{BeforeSerialize();
            // インデントの有無に応じて TypeInfo を切り替えて最速で実行
            var typeInfo = isIndented ? _indentedContext.{className} : _defaultContext.{className};
            return JsonSerializer.Serialize(this, typeInfo);
        }}");
            }
            else
            {
                // Case 2: Context がない場合 (リフレクションベース)
                // 既存の動的な option 選択ロジックを利用
                var optionSelection = customConverters.Any()
                    ? $"isIndented ? {className}._specificIndentedOptions : {className}._specificOptions"
                    : $"isIndented ? {DEFAULT_INDENTED_OPTION_NAME} : {DEFAULT_NOTINDENTED_OPTION_NAME}";

                sb.AppendLine($@"
        /// <summary>Serializes the current instance to a JSON string.</summary>
        {genAttr}
        public string ToJson(bool isIndented = true)
        {{BeforeSerialize();
            // 既存の JsonSerializerOptions を選択して実行
            var options = {optionSelection};
            return JsonSerializer.Serialize(this, options);
        }}");
            }






            //            if (contextTypeName != null)
            //            {
            //                sb.AppendLine($@"
            //        /// <summary>
            //        /// Deserializes the specified JSON string into a <see cref=""{{className}}""/> object.
            //        /// </summary>
            //        /// <param name=""json"">The JSON string to deserialize.</param>
            //        /// <returns>A <see cref=""{{className}}""/> object, or <see langword=""null""/> if the string is null, empty, or invalid.</returns>        {genAttr}
            //        public static {className}{q} FromJson(string json) 
            //        {{
            //            if (string.IsNullOrWhiteSpace(json)) return null;
            //            try
            //            {{
            //                var a = JsonSerializer.Deserialize(json, _indentedContext.{className});
            //                a?.AfterDeserialize();
            //                return a;
            //            }}
            //            catch {{ return null; }}
            //        }}

            //        /// <summary>
            //        /// Serializes the current instance to a JSON string.
            //        /// </summary>
            //        /// <param name=""isIndented""><see langword=""true""/> to format the JSON with indentation; otherwise, <see langword=""false""/>. Default is <see langword=""true""/>.</param>
            //        /// <returns>A JSON string representation of the current instance.</returns>
            //        {genAttr}
            //        public string ToJson(bool isIndented = true)
            //        {{
            //            BeforeSerialize();
            //            return JsonSerializer.Serialize(this, isIndented ? _indentedContext.{className} : _defaultContext.{className});
            //        }}");


            //            }
            //            else
            //            {

            //                var option = customConverters.Any() ? "isIndented ? " + className + "._specificIndentedOptions : " + className + "._specificOptions" :
            //                  "isIndented ? " + DEFAULT_INDENTED_OPTION_NAME + " : " + DEFAULT_NOTINDENTED_OPTION_NAME
            //                  ;
            //                var option2 = customConverters.Any() ? className + "._specificIndentedOptions" :
            //                    DEFAULT_INDENTED_OPTION_NAME
            //                    ;

            //                sb.AppendLine($@"
            //        /// <summary>
            //        /// Deserializes the specified JSON string into a <see cref=""{{className}}""/> object.
            //        /// </summary>
            //        /// <param name=""json"">The JSON string to deserialize.</param>
            //        /// <returns>A <see cref=""{{className}}""/> object, or <see langword=""null""/> if the string is null, empty, or invalid.</returns>
            //        {genAttr}
            //        public static {className}{q} FromJson(string json) 
            //        {{
            //            if (string.IsNullOrWhiteSpace(json)) return null;
            //            try
            //            {{
            //                var a = JsonSerializer.Deserialize<{className}>(json, {option2});
            //                a?.AfterDeserialize();
            //                return a;
            //            }}
            //            catch {{ return null; }}
            //        }}

            //        /// <summary>
            //        /// Serializes the current instance to a JSON string.
            //        /// </summary>
            //        /// <param name=""isIndented""><see langword=""true""/> to format the JSON with indentation; otherwise, <see langword=""false""/>. Default is <see langword=""true""/>.</param>
            //        /// <returns>A JSON string representation of the current instance.</returns>
            //        {genAttr}
            //        public string ToJson(bool isIndented = true)
            //        {{
            //            BeforeSerialize(); 
            //            return JsonSerializer.Serialize(this, {option});
            //        }}

            //");
            //            }

            sb.AppendLine($@"
        /// <summary>
        /// Extensibility method called before the serialization process starts.
        /// </summary>
        {genAttr}
        partial void BeforeSerialize();

        /// <summary>
        /// Extensibility method called after the deserialization process is completed.
        /// </summary>        {genAttr}
        partial void AfterDeserialize();
        ");

            GenerateFileIOMethods(sb, q, className, contextTypeName, customConverters, genAttr);
            if (getPathMethodName != null) GenerateSettingsMethods(sb, className, getPathMethodName, genAttr);

            sb.AppendLine("    }");

            // --- Collection Extensions ---
            GenerateCollectionExtensions(sb, className, accessibility, contextTypeName, contextTargetTypes, customConverters, genAttr, q);

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void GenerateFileIOMethods(StringBuilder sb, string q, string className, string? contextTypeName, string[] customConverters, string genAttr)
        {
            sb.AppendLine($@"
        {genAttr}
        public static {className}{q} LoadFromFile(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{ return FromJson(File.ReadAllText(path)); }}
            catch (Exception ex) {{ Debug.WriteLine(ex.ToString()); return null; }}
        }}
        {genAttr}
        public void SaveToFile(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, ToJson());
            }} catch (Exception ex) {{ Debug.WriteLine(ex.ToString()); }}
        }}");




            var option2 = customConverters.Any() ? className + "._specificIndentedOptions" :
                DEFAULT_INDENTED_OPTION_NAME;


            string loadAsync = contextTypeName != null
                ? $"await JsonSerializer.DeserializeAsync(stream, _indentedContext.{className})"
                : $"await JsonSerializer.DeserializeAsync<{className}>(stream, {option2})";

            string saveAsync = contextTypeName != null
                ? $"await JsonSerializer.SerializeAsync(stream, this, _indentedContext.{className})"
                : $"await JsonSerializer.SerializeAsync(stream, this, {option2})";

            sb.AppendLine($@"
        {genAttr}
        public static async Task<{className}{q}> LoadFromFileAsync(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{
                using var stream = File.OpenRead(path);
                return {loadAsync};
            }} catch (Exception ex) {{ Debug.WriteLine(ex.ToString()); return null; }}
        }}
        {genAttr}
        public async Task SaveToFileAsync(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using var stream = File.Create(path);
                {saveAsync};
            }} catch (Exception ex) {{ Debug.WriteLine(ex.ToString()); }}
        }}");
        }

        private static void GenerateSettingsMethods(StringBuilder sb, string className, string methodName, string genAttr)
        {
            sb.AppendLine($@"
        {genAttr}
        public static {className} LoadSettings() => LoadFromFile({methodName}()) ?? new {className}();
        {genAttr}
        public void SaveSettings() => SaveToFile({methodName}());
        {genAttr}
        public static async Task<{className}> LoadSettingsAsync() => await LoadFromFileAsync({methodName}()) ?? new {className}();
        {genAttr}
        public Task SaveSettingsAsync() => SaveToFileAsync({methodName}());");


            if (methodName != "GetSavePath")
            {
                sb.AppendLine($@"
        {genAttr}
        static string FrostNova.Core.NativeAOT.IJsonStore<{className}>.GetSavePath() => {methodName}();");
            }




        }

        private static void GenerateCollectionExtensions(StringBuilder sb, string className, string accessibility, string? contextTypeName, List<ITypeSymbol> contextTargetTypes, string[] customConverters, string genAttr, string q)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"    ///<summary>{className} collection extension</summary>");
            sb.AppendLine($"    {genAttr}");
            sb.AppendLine($"    {accessibility} static class {className}CollectionJsonExtensions");
            sb.AppendLine("    {");

            
            if (contextTypeName != null)
            {
                // NativeAOT Overloads for specific types
                //        sb.AppendLine($@"        public static string ToJson(this List<{className}> list) => JsonSerializer.Serialize(list, {className}._indentedContext.List{className});
                //public static string ToJson(this {className}[] array) => JsonSerializer.Serialize(array, {className}._indentedContext.{className}Array);
                //public static string ToJson(this IEnumerable<{className}> items) => JsonSerializer.Serialize(items, {className}._indentedContext.IEnumerable{className});

                //public static List<{className}>{q} To{className}List(this string json) => string.IsNullOrWhiteSpace(json) ? new List<{className}>() : JsonSerializer.Deserialize(json, {className}._indentedContext.List{className});
                //public static {className}[]{q} To{className}Array(this string json) => string.IsNullOrWhiteSpace(json) ? Array.Empty<{className}>() : JsonSerializer.Deserialize(json, {className}._indentedContext.{className}Array);");

                foreach (var type in contextTargetTypes)
                {
                    // Property名を推測 (System.Text.Jsonの命名規則に基づく)
                    // 実際にはContext内のIPropertySymbolを走査してJsonTypeInfo<T>を探すのがより確実
                    var typeFull = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var propertyName = typeFull.Split('.').Last().Replace("[]", "Array");

                    var propertyName2 = type.Name.Replace("[]", "Array");


                    var name1 = (propertyName2 == "" ? propertyName : propertyName2+className);
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine($"        public static {typeFull}{q} To{name1}(this string json, {contextTypeName} context)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            return JsonSerializer.Deserialize(json, {className}._indentedContext.{name1});");
                    sb.AppendLine("        }");

                    var a = "isIndented ? " + className + "._indentedContext." + name1 + " : " + className + "._defaultContext." + name1;

                    sb.AppendLine();
                    sb.AppendLine($"        public static string ToJson(this {typeFull} value, bool isIndented = true)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            return JsonSerializer.Serialize(value,{a});");
                    sb.AppendLine("        }");
                }

            }
            else
            {
                string converterInstances = string.Join(", ", customConverters.Select(c => $"new {c}()"));
                if (!string.IsNullOrEmpty(converterInstances)) converterInstances = ", " + converterInstances;



                // Reflection - Single IEnumerable is enough for ToJson

                //        sb.AppendLine($@"        public static string ToJson(this IEnumerable<{className}> items, bool isIndented = true) => JsonSerializer.Serialize(items, {className}._indentedOptions);
                //public static List<{className}>{q} To{className}List(this string json) => string.IsNullOrWhiteSpace(json) ? new List<{className}>() : JsonSerializer.Deserialize<List<{className}>(json, {className}._defaultOptions);
                //public static {className}[]{q} To{className}Array(this string json) => string.IsNullOrWhiteSpace(json) ? Array.Empty<{className}>() : JsonSerializer.Deserialize<{className}[]>(json, {className}._defaultOptions);");




                //defaultを使うかclassのoptionsを使うか
                var option = customConverters.Any() ? "isIndented ? " + className + "._specificIndentedOptions : " + className + "._specificOptions" :
                    "isIndented ? " + DEFAULT_INDENTED_OPTION_NAME + " : " + DEFAULT_NOTINDENTED_OPTION_NAME
                    ;
                var option2 = customConverters.Any() ? className + "._specificIndentedOptions" :
                    DEFAULT_INDENTED_OPTION_NAME
                    ;

                sb.AppendLine($@"        public static string ToJson(this IEnumerable<{className}> items, bool isIndented = true)");
                sb.AppendLine("         {");
                sb.AppendLine($"            return JsonSerializer.Serialize(items, {option});");
                sb.AppendLine("         }");

                sb.AppendLine($@"        public static List<{className}>{q} To{className}List(this string json)");
                sb.AppendLine("         {");
                sb.AppendLine($"            return string.IsNullOrWhiteSpace(json) ? new List<{className}>() : JsonSerializer.Deserialize<List<{className}>>(json, {option2});");
                sb.AppendLine("         }");
                sb.AppendLine($@"        public static {className}[]{q} To{className}Array(this string json)");
                sb.AppendLine("         {");
                sb.AppendLine($"            return string.IsNullOrWhiteSpace(json) ? Array.Empty<{className}>() : JsonSerializer.Deserialize<{className}[]>(json, {option2});");
                sb.AppendLine("         }");

            }

            sb.AppendLine("    }");
        }

       static string GetAccessibilityText(Accessibility accessibility)
        {

            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Protected => "protected",
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "protected internal",
                Accessibility.ProtectedOrInternal => "private protected",
                _ => "private",
            };
        }


    }
}
