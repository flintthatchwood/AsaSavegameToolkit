using System.Reflection.PortableExecutable;

using AsaSavegameToolkit.Plumbing.Primitives;

using Microsoft.Extensions.Logging;

namespace AsaSavegameToolkit.Plumbing.Readers;

/// <summary>
/// Low-level binary reader for ARK save data.
/// Wraps a BinaryReader and provides Unreal Engine-specific read operations.
/// </summary>
public class AsaArchive : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly ILogger _logger;
    private bool _disposed;

    public AsaArchive(ILogger logger, Stream stream, string fileName)
    {
        _logger = logger;
        _reader = new BinaryReader(stream);
        FileName = fileName;
    }
    
    public string FileName { get; }
    
    /// <summary>
    /// Save file format version (from SaveHeader).
    /// Determines which parsing logic to use.
    /// </summary>
    public short SaveVersion { get; set; }
    
    /// <summary>
    /// Name table from SaveHeader (maps int index → string name).
    /// Required to resolve FName instances.
    /// </summary>
    public Dictionary<int, string> NameTable { get; set; } = [];

    /// <summary>
    /// Optional shared string pool for deduplicating FString reads (object instance names,
    /// StrProperty values, etc.) across archives that parse the same save file in parallel.
    /// Strings that share the same content will resolve to the same object reference,
    /// reducing heap pressure for repeated values like tribe names and server names.
    /// Set by <see cref="AsaSaveReader"/> before parsing begins.
    /// </summary>
    public System.Collections.Concurrent.ConcurrentDictionary<string, string>? StringPool { get; set; }
    
    /// <summary>
    /// Current position in the stream.
    /// </summary>
    public long Position
    {
        get => _reader.BaseStream.Position;
        set => _reader.BaseStream.Position = value;
    }

    /// <summary>
    /// Total length of the stream in bytes.
    /// </summary>
    public long Length => _reader.BaseStream.Length;

    /// <summary>
    /// Total length of the stream in bytes.
    /// </summary>
    public long RemainingLength => Length - Position;

    /// <summary>
    /// If true, missing FName indices will be assigned a placeholder and added to the name table instead of throwing.
    /// Useful for parsing cryopod payloads which can reference constants not present in the embedded name table.
    /// </summary>
    public bool AllowDynamicNameTable { get; set; } = false;

    public bool IsCryopod { get; internal set; }

    /// <summary>
    /// Reads a fixed number of bytes.
    /// </summary>
    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);
    
    /// <summary>
    /// Reads a byte.
    /// </summary>
    public byte ReadByte() => _reader.ReadByte();
    
    /// <summary>
    /// Reads a 16-bit signed integer.
    /// </summary>
    public short ReadInt16() => _reader.ReadInt16();
    
    /// <summary>
    /// Reads a 32-bit signed integer.
    /// </summary>
    public int ReadInt32() => _reader.ReadInt32();
    
    /// <summary>
    /// Reads a 64-bit signed integer.
    /// </summary>
    public long ReadInt64() => _reader.ReadInt64();
    
    /// <summary>
    /// Reads a 16-bit unsigned integer.
    /// </summary>
    public ushort ReadUInt16() => _reader.ReadUInt16();
    
    /// <summary>
    /// Reads a 32-bit unsigned integer.
    /// </summary>
    public uint ReadUInt32() => _reader.ReadUInt32();
    
    /// <summary>
    /// Reads a 64-bit unsigned integer.
    /// </summary>
    public ulong ReadUInt64() => _reader.ReadUInt64();
    
    /// <summary>
    /// Reads a 32-bit floating point number.
    /// </summary>
    public float ReadFloat() => _reader.ReadSingle();
    
    /// <summary>
    /// Reads a 64-bit floating point number.
    /// </summary>
    public double ReadDouble() => _reader.ReadDouble();
   
    /// <summary>
    /// Reads a GUID (16 bytes).
    /// ARK/Unreal stores GUIDs with specific byte reordering.
    /// </summary>
    public Guid ReadGuid()
    {
        var bytes = _reader.ReadBytes(16);
        
        return ConvertToGuid(bytes);
    }

    public static byte[] ConvertToBytes(Guid guid)
    {
        // Byte order mapping: [0,1,2,3,6,7,4,5,11,10,9,8,15,14,13,12]

        var bytes = guid.ToByteArray();
        return [
            bytes[0], bytes[1], bytes[2], bytes[3],    // First 4: as-is
            bytes[6], bytes[7], bytes[4], bytes[5],    // Next 4: swap pairs
            bytes[11], bytes[10], bytes[9], bytes[8],  // Next 4: reverse
            bytes[15], bytes[14], bytes[13], bytes[12] // Last 4: reverse
        ];
    }

    public static Guid ConvertToGuid(byte[] bytes)
    {
        // Byte order mapping: [0,1,2,3,6,7,4,5,11,10,9,8,15,14,13,12]

        return new Guid([
            bytes[0], bytes[1], bytes[2], bytes[3],    // First 4: as-is
            bytes[6], bytes[7], bytes[4], bytes[5],    // Next 4: swap pairs
            bytes[11], bytes[10], bytes[9], bytes[8],  // Next 4: reverse
            bytes[15], bytes[14], bytes[13], bytes[12] // Last 4: reverse
        ]);
    }

    /// <summary>
    /// Reads a length-prefixed string.
    /// Format: Int32 (length including null terminator) + UTF8 bytes + null terminator
    /// </summary>
    public string ReadString()
    {
        var startPosition = Position;
        var length = _reader.ReadInt32();
        if (length == 0)
            return string.Empty;

        string value;
        if (length < 0)
        {
            // Unicode string (UCS-2)
            length = -length;
            if (length > RemainingLength)
            {
                throw new Exception($"Attempting to read a Unicode string of length {length} (including null terminator) at position {startPosition} in {FileName}, but only {RemainingLength} bytes remain in the stream. This may indicate a corrupted save file.");
            }

            var bytes = _reader.ReadBytes(length * 2);
            value = System.Text.Encoding.Unicode.GetString(bytes, 0, (length - 1) * 2);
        }
        else
        {
            // UTF-8 string
            if (length > RemainingLength)
            {
                throw new Exception($"Attempting to read a UTF-8 string of length {length} at position {startPosition} in {FileName}, but only {RemainingLength} bytes remain. This may indicate a corrupted save file.");
            }
            var utf8Bytes = _reader.ReadBytes(length);
            value = System.Text.Encoding.UTF8.GetString(utf8Bytes, 0, length - 1); // Exclude null terminator
        }

        // Deduplicate against the shared pool when one is available.
        // This collapses repeated FStrings (parent instance names, tribe names, server names)
        // to a single string instance for the lifetime of the save read, without permanently
        // interning them as string.Intern() would.
        return StringPool != null ? StringPool.GetOrAdd(value, value) : value;
    }

    /// <summary>
    /// Reads an FName (Unreal Engine name).
    /// Format: Int32 (name index) + Int32 (instance number)
    /// Immediately resolves the string value from the name table.
    /// </summary>
    public FName ReadFName()
    {
        if (IsCryopod && NameTable.Count == 0)
        {
            // Property deserialization requires reading FNames, but cryopods write some of their objects with inline
            // strings. The branch logic is simpler if we just allow for inline FNames
            var name = ReadString();
            return new FName(int.MinValue, 0, name);
        }

        var nameIndex = _reader.ReadInt32();
        int instanceNumber = _reader.ReadInt32();

        // Resolve the name string immediately
        if (!NameTable.TryGetValue(nameIndex, out var nameString))
        {
            if (AllowDynamicNameTable)
            {
                nameString = $"Name_{nameIndex:X8}";
                NameTable[nameIndex] = nameString;
            }
            else
            {
                throw new KeyNotFoundException($"Name index {nameIndex} not found in name table at position {Position - 8} of {FileName}");
            }
        }

        return new FName(nameIndex, instanceNumber, nameString);
    }

    /// <summary>
    /// Reads an FPropertyTypeName (Unreal Engine 5.5+ type system).
    /// Recursively reads the type tree: FName (type) + Int32 (parameter count) + recursive parameters.
    /// Example: MapProperty(NameProperty, FloatProperty) reads:
    ///   1. FName=MapProperty, InnerCount=2
    ///   2. FName=NameProperty, InnerCount=0
    ///   3. FName=FloatProperty, InnerCount=0
    /// </summary>
    public FPropertyTypeName ReadPropertyTypeName(int depth = 0)
    {
        // Guard here against a stack overflow caused by a misread (e.g. due to a corrupted save file)
        if(depth > 10)
        {
            throw new InvalidDataException($"Exceeded maximum recursion depth while reading FPropertyTypeName at position {Position}. This may indicate a corrupted save file.");
        }

        var typeName = ReadFName();
        var parameterCount = _reader.ReadInt32();

        // The minimum size for a FPropertyTypeName is 12 bytes (fname + int32)
        if (parameterCount * 12 > RemainingLength)
        {
            throw new AsaDataException($"Invalid type parameter count read at offset {Position - 4} of {FileName}");
        }

        var parameters = parameterCount == 0
            ? Array.Empty<FPropertyTypeName>()
            : new FPropertyTypeName[parameterCount];

        for (int i = 0; i < parameterCount; i++)
        {
            parameters[i] = ReadPropertyTypeName(depth + 1); // Recursive call
        }

        // Intern the instance: identical type signatures share a single object across
        // all game objects in the save, keeping the FPropertyTypeName tree very small.
        return FPropertyTypeName.Create(typeName, parameters);
    }
    
    public string[] ReadStringArray()
    {
        var stringCount = ReadInt32();
        if (stringCount * 4 > RemainingLength) // sanity check for string count vs remaining bytes (minimum 4 bytes per string)
        {
            throw new AsaDataException($"Invalid string count {stringCount} read at offset {Position - 4} of {FileName}");
        }

        var strings = new string[stringCount];
        for (int i = 0; i < stringCount; i++)
        {
            strings[i] = ReadString();
        }

        return strings;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
