using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using DemoFile.Sdk;
using QuickGraph;
using QuickGraph.Algorithms.Search;

namespace DemoFile.SdkGen;

internal static class Program
{
    private static readonly IReadOnlySet<string> IgnoreClasses = new HashSet<string>
    {
        "GameTime_t",
        "GameTick_t",
        "CGameSceneNodeHandle",
        "HSequence"
    };

    private static readonly IReadOnlySet<string> EngineSchemaClasses = new HashSet<string>
    {
        "CEntityInstance",
        "CEntityComponent",
        "CEntityIdentity",
        "CScriptComponent"
    };

    public static async Task Main(string[] args)
    {
        var (demoPath, outputPath) = args switch
        {
            [var fst, var snd] => (Path.GetFullPath(fst), Path.GetFullPath(snd)),
            _ => throw new Exception("Expected arguments: <path to .dem> <output dir for .cs files>")
        };

        Console.WriteLine($"Writing output to: {outputPath}");
        var gameName = Path.GetExtension(outputPath)[1..];
        Console.WriteLine($"Using game name: {gameName}");
        var gameSdkInfo = new GameSdkInfo(gameName);

        var (protocolVersion, networkClasses) = await ReadNetworkClasses(demoPath);

        // Concat together all enums and classes
        var allEnums = new SortedDictionary<string, SchemaEnum>();
        var allClasses = new SortedDictionary<string, SchemaClass>();

        var schemaFiles = new[] {"server.json", "!GlobalTypes.json"};

        foreach (var schemaFile in schemaFiles)
        {
            var schemaPath = Path.Combine(outputPath, "Schema", schemaFile);

            var schema = JsonSerializer.Deserialize<SchemaModule>(
                File.ReadAllText(schemaPath),
                SchemaJson.SerializerOptions)!;

            foreach (var (enumName, schemaEnum) in schema.Enums)
            {
                allEnums[enumName] = schemaEnum;
            }

            foreach (var (className, schemaClass) in schema.Classes)
            {
                if (IgnoreClasses.Contains(className))
                    continue;

                allClasses[className] = schemaClass;
            }
        }

        var parentToChildMap = allClasses.Where(kvp => kvp.Value.Parent != null)
            .GroupBy(kvp => kvp.Value.Parent!)
            .ToDictionary(g => g.Key, g => g.ToImmutableList());

        // Generate graph of classes -> fields
        var graph = new AdjacencyGraph<string, Edge<string>>();
        var parentToChildGraph = new AdjacencyGraph<string, Edge<string>>();

        // Types used as pointers
        var pointeeTypes = new HashSet<string>();

        foreach (var (className, schemaClass) in allClasses)
        {
            if (schemaClass.Parent != null)
            {
                graph.AddVerticesAndEdge(new Edge<string>(className, schemaClass.Parent));
                parentToChildGraph.AddVerticesAndEdge(new Edge<string>(schemaClass.Parent, className));
            }

            foreach (var field in schemaClass.Fields)
            {
                var currentType = field.Type;
                while (currentType != null)
                {
                    if (currentType.IsDeclared)
                    {
                        graph.AddVerticesAndEdge(new Edge<string>(className, currentType.Name));
                    }

                    currentType = currentType.Inner;
                }

                // Pointers mean we need to add references to the child classes of referenced type
                if (field.Type.Category == SchemaTypeCategory.Ptr)
                {
                    var childClasses = parentToChildMap.GetValueOrDefault(
                        field.Type.Inner!.Name,
                        ImmutableList<KeyValuePair<string, SchemaClass>>.Empty);

                    var queue = new Queue<(string, string)>(childClasses.Select(x => (className, x.Key)));

                    while (queue.Count > 0)
                    {
                        var (parent, childClass) = queue.Dequeue();

                        graph.AddVerticesAndEdge(new Edge<string>(parent, childClass));

                        var myChildren = parentToChildMap.GetValueOrDefault(
                            childClass,
                            ImmutableList<KeyValuePair<string, SchemaClass>>.Empty);
                        foreach (var (toAdd, _) in myChildren)
                        {
                            queue.Enqueue((childClass, toAdd));
                        }
                    }

                    pointeeTypes.Add(field.Type.Inner!.Name);
                }
            }
        }

        // Build a list of all classes that implement CEntityInstance
        var entityClasses = new HashSet<string>();
        var networkClassSearch = new BreadthFirstSearchAlgorithm<string, Edge<string>>(parentToChildGraph);
        networkClassSearch.FinishVertex += node => { entityClasses.Add(node); };
        networkClassSearch.Compute("CEntityInstance");

        // Do a search from all entity classes
        var visited = new HashSet<string>();
        var search = new BreadthFirstSearchAlgorithm<string, Edge<string>>(graph);
        search.FinishVertex += node => { visited.Add(node); };

        Console.Write($"Building network class reachability graph");
        foreach (var networkClassName in networkClasses)
        {
            Console.Write('.');
            search.Compute(networkClassName);
        }
        Console.WriteLine();

        // Only emit visited vertices from the search
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine($"// Generated from protocol v{protocolVersion}");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("#pragma warning disable CS1591");
        builder.AppendLine();
        builder.AppendLine("using System.ComponentModel;");
        builder.AppendLine("using System.Diagnostics;");
        builder.AppendLine("using System.Diagnostics.CodeAnalysis;");
        builder.AppendLine("using System.Drawing;");
        builder.AppendLine("using System.Runtime.CompilerServices;");
        builder.AppendLine("using DemoFile;");
        builder.AppendLine();
        builder.AppendLine("namespace DemoFile.Sdk;");

        foreach (var (enumName, schemaEnum) in allEnums)
        {
            if (visited.Contains(enumName))
            {
                WriteEnum(gameSdkInfo, builder, enumName, schemaEnum);
            }
        }

        var visitedClassNames = new HashSet<string>();
        var visitedEntityClasses = new HashSet<string>();
        foreach (var (className, schemaClass) in allClasses)
        {
            if (!visited.Contains(className))
                continue;

            var isPointeeType = pointeeTypes.Contains(className);
            var isEntityClass = entityClasses.Contains(className);

            if (!EngineSchemaClasses.Contains(className))
            {
                WriteClass(gameSdkInfo, builder, className, schemaClass, parentToChildMap, isPointeeType, isEntityClass, allClasses);
            }

            visitedClassNames.Add(className);
            if (isEntityClass)
                visitedEntityClasses.Add(className);
        }

        WriteDecoderSet(gameSdkInfo, builder, visitedClassNames, allClasses);

        WriteEntityFactoriesLookup(gameSdkInfo, visitedEntityClasses, builder);

        Console.WriteLine("Saving Schema.cs...");
        var schemaPathCs = Path.Combine(outputPath, "Sdk", "Schema.cs");
        File.WriteAllText(schemaPathCs, builder.ToString());

        Console.WriteLine("Saving EntityEvents.AutoGen.cs...");
        var entityEventsCs = Path.Combine(outputPath, "EntityEvents.AutoGen.cs");
        File.WriteAllText(entityEventsCs, WriteEntityEvents(gameSdkInfo, visitedEntityClasses));

        Console.WriteLine("Done!");
    }

    private static async Task<(int ProtocolVersion, IReadOnlyList<string> NetworkClasses)> ReadNetworkClasses(string demoPath)
    {
        var cts = new CancellationTokenSource();
        var demo = new DummyDemoParser();

        var protocolVersion = 0;
        var networkClasses = new List<string>();

        demo.DemoEvents.DemoFileHeader += msg =>
        {
            protocolVersion = msg.NetworkProtocol;
        };

        demo.DemoEvents.DemoClassInfo += msg =>
        {
            networkClasses.AddRange(msg.Classes.Select(classInfo => classInfo.NetworkName));
            cts.Cancel();
        };

        Console.WriteLine($"Reading class information from: {demoPath}");

        try
        {
            await demo.ReadAllAsync(File.OpenRead(demoPath), cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }

        Console.WriteLine($"Protocol version: {protocolVersion}");
        Console.WriteLine($"Network classes: {networkClasses.Count} classes");
        return (protocolVersion, networkClasses);
    }

    private static string WriteEntityEvents(GameSdkInfo gameSdkInfo, IEnumerable<string> entityClassNames)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"using DemoFile.Sdk;");
        builder.AppendLine($"");
        builder.AppendLine($"namespace DemoFile;");
        builder.AppendLine($"");
        builder.AppendLine($"public struct EntityEvents");
        builder.AppendLine($"{{");

        foreach (var entityClass in entityClassNames.Order())
        {
            var genericArgs = entityClass == "CEntityInstance" ? $"<{gameSdkInfo.DemoParserClass}>" : "";
            builder.AppendLine($"    public EntityEvents<{entityClass}{genericArgs}, {gameSdkInfo.DemoParserClass}> {entityClass};");
        }

        builder.AppendLine($"}}");

        return builder.ToString();
    }

    private static void WriteEntityFactoriesLookup(
        GameSdkInfo gameSdkInfo,
        IEnumerable<string> networkClasses,
        StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine($"internal static class {gameSdkInfo.GameName}EntityFactories");
        builder.AppendLine("{");
        builder.AppendLine($"    public static readonly IReadOnlyDictionary<string, EntityFactory<{gameSdkInfo.DemoParserClass}>> All = new Dictionary<string, EntityFactory<{gameSdkInfo.DemoParserClass}>>");
        builder.AppendLine("    {");

        foreach (var className in networkClasses.Order())
        {
            if (className == "CEntityInstance")
                continue;

            builder.AppendLine($"        {{\"{className}\", (context, decoder) => new {className}(context, decoder)}},");
        }

        builder.AppendLine("    };");
        builder.AppendLine("}");
    }

    private static void WriteDecoderSet(
        GameSdkInfo gameSdkInfo,
        StringBuilder builder,
        IEnumerable<string> classNames,
        SortedDictionary<string, SchemaClass> classMap)
    {
        builder.AppendLine();
        builder.AppendLine($"internal sealed class {gameSdkInfo.GameName}DecoderSet : DecoderSet");
        builder.AppendLine("{");
        builder.AppendLine($"    internal {gameSdkInfo.GameName}DecoderSet(IReadOnlyDictionary<SerializerKey, Serializer> serializers) : base(serializers)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override bool TryGetDecoderByName(string className, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor), NotNullWhen(true)] out Type? classType, [NotNullWhen(true)] out SendNodeDecoder<object>? decoder)");
        builder.AppendLine("    {");
        builder.AppendLine("        switch (className)");
        builder.AppendLine("        {");

        foreach (var className in classNames)
        {
            if (className == "CEntityInstance")
                continue;

            if (classMap.TryGetValue(className, out var schemaClass) && schemaClass.IsBoxedIntegerType)
                continue;

            var schemaType = SchemaFieldType.FromDeclaredClass(className);

            builder.AppendLine($"        case \"{className}\":");
            builder.AppendLine($"        {{");
            builder.AppendLine($"            var innerDecoder = GetDecoder<{schemaType.GetCsTypeName(gameSdkInfo)}>(new SerializerKey(className, 0));");
            builder.AppendLine($"            classType = typeof({schemaType.GetCsTypeName(gameSdkInfo)});");
            builder.AppendLine($"            decoder = (object instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
            builder.AppendLine($"            {{");
            builder.AppendLine($"                Debug.Assert(instance is {schemaType.GetCsTypeName(gameSdkInfo)});");
            builder.AppendLine($"                var @this = Unsafe.As<{schemaType.GetCsTypeName(gameSdkInfo)}>(instance);");
            builder.AppendLine($"                innerDecoder(@this, path, ref buffer);");
            builder.AppendLine($"            }};");
            builder.AppendLine($"            return true;");
            builder.AppendLine($"        }}");
        }

        builder.AppendLine("        default:");
        builder.AppendLine("            classType = null;");
        builder.AppendLine("            decoder = null;");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override SendNodeDecoderFactory<T> GetFactory<T>()");
        builder.AppendLine("    {");

        var hardcodedClasses = gameSdkInfo.HardcodedChildClasses.Values.SelectMany(x => x);

        foreach (var className in classNames.Concat(hardcodedClasses).Order())
        {
            if (className == "CEntityInstance")
                continue;

            if (classMap.TryGetValue(className, out var schemaClass) && schemaClass.IsBoxedIntegerType)
                continue;

            var schemaType = SchemaFieldType.FromDeclaredClass(className);

            builder.AppendLine($"        if (typeof(T) == typeof({schemaType.GetCsTypeName(gameSdkInfo)}))");
            builder.AppendLine($"        {{");
            builder.AppendLine($"            return (SendNodeDecoderFactory<T>)(object)new SendNodeDecoderFactory<{schemaType.GetCsTypeName(gameSdkInfo)}>({schemaType.GetCsTypeName(gameSdkInfo)}.CreateFieldDecoder);");
            builder.AppendLine($"        }}");
        }

        builder.AppendLine();
        builder.AppendLine("        throw new NotImplementedException($\"Unknown send node class: {typeof(T)}\");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void WriteClass(
        GameSdkInfo gameSdkInfo,
        StringBuilder builder,
        string schemaClassName,
        SchemaClass schemaClass,
        IReadOnlyDictionary<string, ImmutableList<KeyValuePair<string, SchemaClass>>> parentToChildMap,
        bool isPointeeType,
        bool isEntityClass,
        IReadOnlyDictionary<string, SchemaClass> classMap)
    {
        var isCEntityInstance = schemaClassName == "CEntityInstance";
        var classType = SchemaFieldType.FromDeclaredClass(schemaClassName);
        var classNameCs = classType.GetCsTypeName(gameSdkInfo);

        builder.AppendLine();
        foreach (var metadata in schemaClass.Metadata)
        {
            builder.AppendLine($"// {metadata.Name}{(metadata.HasValue ? $" {metadata}" : "")}");
        }

        if (schemaClass.IsBoxedIntegerType)
        {
            builder.AppendLine($"public readonly record struct {classNameCs}(int Value);");
            return;
        }

        builder.Append($"public partial class {classType.GetCsTypeName(gameSdkInfo)}");

        var parentType = schemaClass.Parent == null
            ? null
            : SchemaFieldType.FromDeclaredClass(schemaClass.Parent);
        if (parentType != null)
            builder.Append($" : {parentType.GetCsTypeName(gameSdkInfo)}");

        builder.AppendLine();
        builder.AppendLine("{");

        // All entity classes eventually derive from CEntityInstance,
        // which is the root networkable class.
        if (isEntityClass && !isCEntityInstance)
        {
            builder.AppendLine($"    internal {classNameCs}({gameSdkInfo.DemoParserClass}.EntityContext context, SendNodeDecoder<object> decoder) : base(context, decoder) {{}}");
            builder.AppendLine();
        }

        // Types that are used as pointers in other classes will need
        // a CreateDowncastDecoder method, to allow for polymorphic pointers.
        if (isPointeeType)
        {
            builder.AppendLine($"    internal static SendNodeDecoder<{classNameCs}> CreateDowncastDecoder(SerializerKey serializerKey, DecoderSet decoderSet, out Func<{classNameCs}> factory)");
            builder.AppendLine($"    {{");

            builder.AppendLine($"        if (serializerKey.Name == \"{schemaClassName}\")");
            builder.AppendLine($"        {{");
            builder.AppendLine($"            factory = () => new {classNameCs}();");
            builder.AppendLine($"            return decoderSet.GetDecoder<{classNameCs}>(serializerKey);");
            builder.AppendLine($"        }}");

            if (parentToChildMap.TryGetValue(schemaClassName, out var directChildren))
            {
                var hardcodedChildren = gameSdkInfo.HardcodedChildClasses.GetValueOrDefault(
                    schemaClassName,
                    ArraySegment<string>.Empty);

                var childClasses = new Queue<string>(directChildren.Select(x => x.Key).Concat(hardcodedChildren));

                while (childClasses.Count > 0)
                {
                    var childClass = childClasses.Dequeue();

                    builder.AppendLine($"        else if (serializerKey.Name == \"{childClass}\")");
                    builder.AppendLine($"        {{");
                    builder.AppendLine($"            factory = () => new {childClass}();");
                    builder.AppendLine($"            var childClassDecoder = decoderSet.GetDecoder<{childClass}>(serializerKey);");
                    builder.AppendLine($"            return ({classNameCs} instance, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                Debug.Assert(instance is {childClass});");
                    builder.AppendLine($"                var downcastInstance = Unsafe.As<{childClass}>(instance);");
                    builder.AppendLine($"                childClassDecoder(downcastInstance, path, ref buffer);");
                    builder.AppendLine($"            }};");
                    builder.AppendLine($"        }}");

                    var myChildren = parentToChildMap.GetValueOrDefault(
                        childClass,
                        ImmutableList<KeyValuePair<string, SchemaClass>>.Empty);
                    foreach (var (toAdd, _) in myChildren)
                    {
                        childClasses.Enqueue(toAdd);
                    }
                }
            }

            builder.AppendLine($"        throw new NotImplementedException($\"Unknown derived class of {classNameCs}: {{serializerKey}}\");");
            builder.AppendLine($"    }}");
            builder.AppendLine();
        }

        foreach (var metadata in schemaClass.Metadata)
        {
            if (metadata.Name != "MNetworkVarTypeOverride")
                continue;

            var typeOverride = metadata.ClassValue;

            SchemaField? ancestorField = null;
            var searchType = classMap[schemaClass.Parent!];
            while (ancestorField == null)
            {
                ancestorField = searchType.Fields.FirstOrDefault(x => x.Name == typeOverride.Name);
                if (ancestorField == null)
                    searchType = classMap[searchType.Parent!];
            }

            Debug.Assert(searchType != null && ancestorField != null);

            var csPropertyName = searchType.CsPropertyNameForField(gameSdkInfo, schemaClassName, ancestorField);

            var overrideType = ancestorField.Type.Inner != null
                ? ancestorField.Type with { Inner = ancestorField.Type.Inner with { Name = typeOverride.Type } }
                : ancestorField.Type with { Name = typeOverride.Type };

            builder.AppendLine($"    public new {overrideType.GetCsTypeName(gameSdkInfo)} {csPropertyName}");
            builder.AppendLine($"    {{");
            builder.AppendLine($"        get => ({overrideType.GetCsTypeName(gameSdkInfo)}) base.{csPropertyName};");
            builder.AppendLine($"    }}");
            builder.AppendLine();
        }

        foreach (var field in schemaClass.Fields)
        {
            var defaultValue = field.Type.Category switch
            {
                SchemaTypeCategory.DeclaredClass =>
                    " = new();",
                SchemaTypeCategory.FixedArray =>
                    field.Type.IsString
                        ? $" = \"\";"
                        : $" = Array.Empty<{field.Type.Inner!.GetCsTypeName(gameSdkInfo)}>();",
                SchemaTypeCategory.Atomic when field.Type.Atomic == SchemaAtomicCategory.Collection =>
                    $" = new NetworkedVector<{field.Type.Inner!.GetCsTypeName(gameSdkInfo)}>();",
                _ => null
            };

            foreach (var metadata in field.Metadata)
            {
                builder.AppendLine($"    // {metadata.Name}{(metadata.HasValue ? $" {metadata}" : "")}");
            }

            var csPropertyName = schemaClass.CsPropertyNameForField(gameSdkInfo, schemaClassName, field);
            builder.AppendLine($"    public {field.Type.GetCsTypeName(gameSdkInfo)} {csPropertyName} {{ get; private set; }}{defaultValue}");

            if (isEntityClass && field.Type.TryGetEntityHandleType(gameSdkInfo, out var entityType))
            {
                builder.AppendLine($"    public {entityType}? {csPropertyName[..^6]} => {csPropertyName}.Get(Demo);");
            }

            builder.AppendLine();
        }

        // Write decoder method
        builder.AppendLine($"    internal {(schemaClass.Parent == null ? "" : "new ")}static SendNodeDecoder<{classNameCs}> CreateFieldDecoder(SerializableField field, DecoderSet decoderSet)");
        builder.AppendLine("    {");

        foreach (var field in schemaClass.Fields)
        {
            var fieldCsTypeName = field.Type.GetCsTypeName(gameSdkInfo);
            var fieldCsPropertyName = schemaClass.CsPropertyNameForField(gameSdkInfo, schemaClassName, field);
            var fieldClass = classMap.GetValueOrDefault(field.Type.Name);
            
            field.TryGetMetadata("MNetworkSerializer", out var serializer);
            field.TryGetMetadata("MNetworkAlias", out var alias);

            if (field.Type.Category == SchemaTypeCategory.DeclaredClass && fieldClass?.IsBoxedIntegerType != true)
            {
                if (!IgnoreClasses.Contains(field.Type.Name))
                {
                    builder.AppendLine($"        if (field.SendNode.Length >= 1 && field.SendNode.Span[0] == \"{field.Name}\")");
                    builder.AppendLine($"        {{");
                    builder.AppendLine($"            var innerDecoder = {fieldCsTypeName}.CreateFieldDecoder(field with {{SendNode = field.SendNode[1..]}}, decoderSet);");
                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                innerDecoder(@this.{fieldCsPropertyName}, path, ref buffer);");
                    builder.AppendLine($"            }};");
                    builder.AppendLine($"        }}");
                }
            }
            else
            {
                builder.AppendLine($"        if (field.VarName == \"{alias?.StringValue ?? field.Name}\")");
                builder.AppendLine($"        {{");

                if (field.Type.Atomic == SchemaAtomicCategory.Collection && field.Type.Inner!.Category == SchemaTypeCategory.DeclaredClass)
                {
                    var inner = field.Type.Inner!;

                    // This field is a variable array for a declared class
                    // (i.e. we'll need to delegate deserialisation of the child elements)

                    builder.AppendLine($"            var innerDecoder = decoderSet.GetDecoder<{inner.GetCsTypeName(gameSdkInfo)}>(field.FieldSerializerKey!.Value);");
                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                if (path.Length == 1)");
                    builder.AppendLine($"                {{");
                    builder.AppendLine($"                    var newSize = (int)buffer.ReadUVarInt32();");
                    builder.AppendLine($"                    @this.{fieldCsPropertyName}.Resize(newSize);");
                    builder.AppendLine($"                }}");
                    builder.AppendLine($"                else");
                    builder.AppendLine($"                {{");
                    builder.AppendLine($"                    Debug.Assert(path.Length > 2);");
                    builder.AppendLine($"                    var index = path[1];");
                    builder.AppendLine($"                    @this.{fieldCsPropertyName}.EnsureSize(index + 1);");
                    builder.AppendLine($"                    var element = @this.{fieldCsPropertyName}[index] ??= new {inner.GetCsTypeName(gameSdkInfo)}();");
                    builder.AppendLine($"                    innerDecoder(element, path[2..], ref buffer);");
                    builder.AppendLine($"                }}");
                    builder.AppendLine($"            }};");
                }
                else if (field.Type.Atomic == SchemaAtomicCategory.Collection)
                {
                    Debug.Assert(field.Type.Inner!.Category is SchemaTypeCategory.Atomic or SchemaTypeCategory.Builtin or SchemaTypeCategory.DeclaredEnum);

                    // This field is a variable array for an atomic value

                    var elementCsTypeName = field.Type.Inner!.GetCsTypeName(gameSdkInfo);

                    var decoderMethod = field.Type.Inner.Category == SchemaTypeCategory.DeclaredEnum
                        ? $"CreateDecoder_enum<{elementCsTypeName}>"
                        : $"CreateDecoder_{elementCsTypeName}";

                    builder.AppendLine($"            var decoder = FieldDecode.{decoderMethod}(field.FieldEncodingInfo);");
                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                if (path.Length == 1)");
                    builder.AppendLine($"                {{");
                    builder.AppendLine($"                    var newSize = (int)buffer.ReadUVarInt32();");
                    builder.AppendLine($"                    @this.{fieldCsPropertyName}.Resize(newSize);");
                    builder.AppendLine($"                }}");
                    builder.AppendLine($"                else");
                    builder.AppendLine($"                {{");
                    builder.AppendLine($"                    Debug.Assert(path.Length == 2);");
                    builder.AppendLine($"                    var index = path[1];");
                    builder.AppendLine($"                    @this.{fieldCsPropertyName}.EnsureSize(index + 1);");
                    builder.AppendLine($"                    var element = decoder(ref buffer);");
                    builder.AppendLine($"                    @this.{fieldCsPropertyName}[index] = element;");
                    builder.AppendLine($"                }}");
                    builder.AppendLine($"            }};");
                }
                else if (field.Type.TryGetArrayElementType(out var elementType))
                {
                    Debug.Assert(serializer == null, "Unexpected serializer on fixed array type");

                    // This field is a fixed array

                    var elementCsTypeName = elementType.GetCsTypeName(gameSdkInfo);
                    var elementClass = classMap.GetValueOrDefault(elementType.Name);

                    if (elementClass?.IsBoxedIntegerType == true)
                    {
                        builder.AppendLine($"            var fixedArraySize = field.VarType.ArrayLength;");
                        builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                        builder.AppendLine($"            {{");
                        builder.AppendLine($"                if (@this.{fieldCsPropertyName}.Length == 0) @this.{fieldCsPropertyName} = new {elementCsTypeName}[fixedArraySize];");
                        builder.AppendLine($"                @this.{fieldCsPropertyName}[path[1]] = new {elementCsTypeName}(buffer.ReadVarInt32());");
                        builder.AppendLine($"            }};");
                    }
                    else
                    {
                        var decoderMethod = elementType.Category == SchemaTypeCategory.DeclaredEnum
                            ? $"CreateDecoder_enum<{elementCsTypeName}>"
                            : $"CreateDecoder_{elementCsTypeName}";

                        builder.AppendLine($"            var fixedArraySize = field.VarType.ArrayLength;");
                        builder.AppendLine($"            var decoder = FieldDecode.{decoderMethod}(field.FieldEncodingInfo);");
                        builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                        builder.AppendLine($"            {{");
                        builder.AppendLine($"                if (@this.{fieldCsPropertyName}.Length == 0) @this.{fieldCsPropertyName} = new {elementCsTypeName}[fixedArraySize];");
                        builder.AppendLine($"                @this.{fieldCsPropertyName}[path[1]] = decoder(ref buffer);");
                        builder.AppendLine($"            }};");
                    }
                }
                else if (field.Type.Category == SchemaTypeCategory.Ptr)
                {
                    var inner = field.Type.Inner!;
                    Debug.Assert(inner.Category == SchemaTypeCategory.DeclaredClass);

                    // This field represents a nested serializer (SendNode)

                    // Fields marked as polymorphic are encoded differently.
                    // Rather than a boolean indicating whether the pointer is null,
                    // the field describes which child class the pointer is an instance of.
                    var isPolymorphic = field.TryGetMetadata("MNetworkPolymorphic", out _);

                    if (isPolymorphic)
                    {
                        builder.AppendLine($"            SendNodeDecoder<{inner.GetCsTypeName(gameSdkInfo)}>? innerDecoder = null;");
                    }
                    else
                    {
                        builder.AppendLine($"            Debug.Assert(field.FieldSerializerKey.HasValue);");
                        builder.AppendLine($"            var serializerKey = field.FieldSerializerKey.Value;");
                        builder.AppendLine($"            var innerDecoder = {inner.GetCsTypeName(gameSdkInfo)}.CreateDowncastDecoder(serializerKey, decoderSet, out var factory);");
                    }

                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                if (path.Length == 1)");
                    builder.AppendLine($"                {{");
                    builder.AppendLine($"                    var isSet = buffer.ReadOneBit();");

                    if (isPolymorphic)
                    {
                        builder.AppendLine($"                    var childClassId = ((int) buffer.ReadUBitVar()) - 1;");
                        builder.AppendLine($"                    innerDecoder = {inner.Name}.CreateDowncastDecoder(field.PolymorphicTypes[childClassId], decoderSet, out var factory);");
                        builder.AppendLine($"                    if (!isSet)");
                        builder.AppendLine($"                    {{");
                        builder.AppendLine($"                        innerDecoder = null;");
                        builder.AppendLine($"                        @this.{fieldCsPropertyName} = null;");
                        builder.AppendLine($"                    }}");
                        builder.AppendLine($"                    else");
                        builder.AppendLine($"                    {{");
                        builder.AppendLine($"                        @this.{fieldCsPropertyName} = factory();");
                        builder.AppendLine($"                        return;");
                        builder.AppendLine($"                    }}");
                    }
                    else
                    {
                        builder.AppendLine($"                    @this.{fieldCsPropertyName} = isSet ? factory() : null;");
                    }

                    builder.AppendLine($"                }}");
                    builder.AppendLine($"                else");
                    builder.AppendLine($"                {{");

                    if (isPolymorphic)
                    {
                        builder.AppendLine($"                    Debug.Assert(innerDecoder != null);");
                        builder.AppendLine($"                    Debug.Assert(@this.{fieldCsPropertyName} != null);");
                        builder.AppendLine($"                    var inner = @this.{fieldCsPropertyName}!;");
                    }
                    else
                    {
                        builder.AppendLine($"                    var inner = @this.{fieldCsPropertyName} ??= factory();");
                    }

                    builder.AppendLine($"                    innerDecoder(inner, path[1..], ref buffer);");
                    builder.AppendLine($"                }}");
                    builder.AppendLine($"            }};");
                }
                else if (field.Type.Category == SchemaTypeCategory.DeclaredClass && fieldClass?.IsBoxedIntegerType == true)
                {
                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");
                    builder.AppendLine($"                @this.{fieldCsPropertyName} = new {fieldCsTypeName}(buffer.ReadVarInt32());");
                    builder.AppendLine($"            }};");
                }
                else
                {
                    // This field is a primitive - decode and assign the value directly

                    var decoderMethod =
                        field.Type.Category == SchemaTypeCategory.DeclaredEnum
                            ? $"FieldDecode.CreateDecoder_enum<{fieldCsTypeName}>"
                            : serializer == null
                                ? $"FieldDecode.CreateDecoder_{fieldCsTypeName}"
                                : $"CreateDecoder_{serializer.StringValue}";

                    builder.AppendLine($"            var decoder = {decoderMethod}(field.FieldEncodingInfo);");
                    builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
                    builder.AppendLine($"            {{");

                    if (serializer == null)
                    {
                        builder.AppendLine($"                @this.{fieldCsPropertyName} = decoder(ref buffer);");
                    }
                    else
                    {
                        builder.AppendLine($"                @this.{fieldCsPropertyName} = decoder(@this, ref buffer);");
                    }

                    builder.AppendLine($"            }};");
                }

                builder.AppendLine($"        }}");
            }
        }

        if (parentType == null)
        {
            builder.AppendLine($"        if (FallbackDecoder.TryCreate(field.VarName, field.VarType, field.FieldEncodingInfo, decoderSet, out var fallback))");
            builder.AppendLine($"        {{");
            builder.AppendLine($"            return ({classNameCs} @this, ReadOnlySpan<int> path, ref BitBuffer buffer) =>");
            builder.AppendLine($"            {{");
            builder.AppendLine($"#if DEBUG");
            builder.AppendLine($"                var _field = field;");
            builder.AppendLine($"#endif");
            builder.AppendLine($"                fallback(default, path, ref buffer);");
            builder.AppendLine($"            }};");
            builder.AppendLine($"        }}");
            builder.AppendLine($"        throw new NotSupportedException($\"Unrecognised serializer field: {{field.VarName}}\");");
        }
        else
        {
            builder.AppendLine($"        return {parentType.GetCsTypeName(gameSdkInfo)}.CreateFieldDecoder(field, decoderSet);");
        }

        builder.AppendLine("    }");

        if (isEntityClass)
        {
            foreach (var eventName in new[] { "Create", "Delete", "PreUpdate", "PostUpdate" })
            {
                var accessor = isCEntityInstance ? "virtual" : "override";
                var eventField = isCEntityInstance ? "EntityInstanceEvents" : $"EntityEvents.{classNameCs}";

                builder.AppendLine($"");
                builder.AppendLine("    [EditorBrowsable(EditorBrowsableState.Advanced)]");
                builder.AppendLine($"    public {accessor} void Fire{eventName}Event()");
                builder.AppendLine($"    {{");
                builder.AppendLine($"        Demo.{eventField}.{eventName}?.Invoke(this);");
                if (parentType != null)
                {
                    builder.AppendLine($"        base.Fire{eventName}Event();");
                }

                builder.AppendLine($"    }}");
            }
        }

        builder.AppendLine("}");
    }

    private static string EnumType(int alignment) =>
        alignment switch
        {
            1 => "byte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
        };

    private static void WriteEnum(GameSdkInfo gameSdkInfo, StringBuilder builder, string enumName, SchemaEnum schemaEnum)
    {
        var enumType = SchemaFieldType.FromDeclaredClass(enumName);

        builder.AppendLine();
        builder.AppendLine($"public enum {enumType.GetCsTypeName(gameSdkInfo)} : {EnumType(schemaEnum.Align)}");
        builder.AppendLine("{");

        var maxValue = schemaEnum.Align switch
        {
            1 => byte.MaxValue,
            2 => short.MaxValue,
            4 => int.MaxValue,
            8 => long.MaxValue,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Write enum items
        foreach (var enumItem in schemaEnum.Items)
        {
            if (enumItem.Value < 0)
            {
                builder.AppendLine($"    {enumItem.Name} = {enumItem.Value},");
            }
            else
            {
                builder.AppendLine($"    {enumItem.Name} = 0x{enumItem.Value:X},");
            }
        }

        builder.AppendLine("}");
    }
}
