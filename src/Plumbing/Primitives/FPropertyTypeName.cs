using System.Collections.Concurrent;

namespace AsaSavegameToolkit.Plumbing.Primitives;

/// <summary>
/// Represents the complete type information for a property, including generic type parameters.
/// Corresponds to Unreal Engine's FPropertyTypeName (UE 5.5+).
/// 
/// Examples:
/// - IntProperty
/// - ByteProperty(EPrimalEquipmentType)
/// - ArrayProperty(IntProperty)
/// - MapProperty(NameProperty, FloatProperty)
/// - StructProperty(Vector, 12345678-1234-1234-1234-123456789abc)
/// - MapProperty(StructProperty(KeyStruct), EnumProperty(ByteEnum, ByteProperty))
/// 
/// Instances are interned globally: identical type signatures always resolve to the same object,
/// so the thousands of copies of (e.g.) "IntProperty" across all game objects share one instance.
/// </summary>
public class FPropertyTypeName
{
    // Global intern pool. A typical save file has fewer than 200 distinct type signatures
    // but millions of property reads, so sharing instances cuts heap pressure dramatically.
    private static readonly ConcurrentDictionary<string, FPropertyTypeName> InternPool = new();

    private FPropertyTypeName(FName typeName, FPropertyTypeName[] parameters)
    {
        TypeName = typeName;
        Parameters = parameters.Length == 0
            ? Array.Empty<FPropertyTypeName>()
            : Array.AsReadOnly(parameters);
    }

    /// <summary>
    /// Returns an interned FPropertyTypeName for the given type name and parameters.
    /// All callers that describe the same type receive the exact same object reference.
    /// </summary>
    public static FPropertyTypeName Create(FName typeName, FPropertyTypeName[] parameters)
    {
        // Build the canonical key used for interning. ToString() is only called once per
        // unique type signature; after that the pool returns the cached instance directly.
        var key = BuildKey(typeName, parameters);
        return InternPool.GetOrAdd(key, static (k, args) => new FPropertyTypeName(args.typeName, args.parameters), (typeName, parameters));
    }

    private static string BuildKey(FName typeName, FPropertyTypeName[] parameters)
    {
        if (parameters.Length == 0)
            return typeName.ToString();
        var paramStrings = string.Join(", ", parameters.Select(static p => p.ToString()));
        return $"{typeName}<{paramStrings}>";
    }

    /// <summary>
    /// Convenience overload for creating an interned type with FName parameters.
    /// </summary>
    public static FPropertyTypeName Create(FName typeName, params FName[] paramNames)
    {
        var parameters = paramNames.Length == 0
            ? Array.Empty<FPropertyTypeName>()
            : paramNames.Select(n => Create(n, Array.Empty<FPropertyTypeName>())).ToArray();
        return Create(typeName, parameters);
    }

    /// <summary>
    /// The root type name (e.g., "IntProperty", "ArrayProperty", "MapProperty").
    /// </summary>
    public FName TypeName { get; }
    
    /// <summary>
    /// Generic type parameters.
    /// - Empty for simple types like IntProperty
    /// - 1 element for ByteProperty(EnumName), ArrayProperty(InnerType), SetProperty(InnerType)
    /// - 2 elements for MapProperty(KeyType, ValueType), StructProperty(StructName, StructGuid)
    /// - Nested for complex types like MapProperty(StructProperty(Key), EnumProperty(Enum, ByteProperty))
    /// </summary>
    /// <remarks>
    /// Immutable after construction. FPropertyTypeName instances are interned, so
    /// mutation would corrupt all sharers of that instance.
    /// </remarks>
    public IReadOnlyList<FPropertyTypeName> Parameters { get; }
    
    /// <summary>
    /// Returns true if this is a simple type with no parameters.
    /// </summary>
    public bool IsSimple => Parameters.Count == 0;
    
    /// <summary>
    /// Gets the number of type parameters.
    /// </summary>
    public int ParameterCount => Parameters.Count;
    
    /// <summary>
    /// Gets a parameter by index, or null if out of bounds.
    /// </summary>
    public FPropertyTypeName? GetParameter(int index)
    {
        return index >= 0 && index < Parameters.Count ? Parameters[index] : null;
    }
    
    /// <summary>
    /// Gets the type name of a parameter by index, or null if out of bounds.
    /// </summary>
    public FName? GetParameterName(int index)
    {
        return GetParameter(index)?.TypeName;
    }
    
    /// <summary>
    /// Returns true if this is StructProperty with the specified struct name as first parameter.
    /// </summary>
    public bool IsStruct(string structName)
    {
        return TypeName.FullName == "StructProperty" && 
               Parameters.Count > 0 && 
               Parameters[0].TypeName.FullName == structName;
    }
    
    /// <summary>
    /// Returns true if this is EnumProperty or ByteProperty with the specified enum name as first parameter.
    /// </summary>
    public bool IsEnum(string enumName)
    {
        return (TypeName == "EnumProperty" || TypeName == "ByteProperty") &&
               Parameters.Count > 0 &&
               Parameters[0].TypeName == enumName;
    }
    
    public override string ToString()
    {
        if (Parameters.Count == 0)
            return TypeName.ToString();
        
        var paramStrings = string.Join(", ", Parameters.Select(static p => p.ToString()));
        return $"{TypeName}<{paramStrings}>";
    }
}
