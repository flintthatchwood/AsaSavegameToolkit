
using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using AsaSavegameToolkit.Plumbing.Primitives;
using CommunityToolkit.HighPerformance.Buffers;

namespace AsaSavegameToolkit.Plumbing.Readers;

/// <summary>
/// Low-level binary reader for ARK save data implemented over a memory buffer.
/// Preserves public API from the original `AsaArchive` but reads from a
/// `ReadOnlyMemory<byte>` and uses spans/stackalloc/ArrayPool where appropriate
/// to reduce temporary allocations.
/// </summary>
public class AsaArchive : IDisposable
{
    private readonly ILogger _logger;
    private readonly ReadOnlyMemory<byte> _data;
    private bool _disposed;

    public AsaArchive(ILogger logger, ReadOnlyMemory<byte> data, string fileName)
    {
        _logger = logger;
        _data = data;
        FileName = fileName;
        Position = 0;
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
    public Dictionary<int, string> NameTable { get; set; } = new();


    /// <summary>
    /// Current position in the buffer.
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Total length of the buffer in bytes.
    /// </summary>
    public long Length => _data.Length;

    /// <summary>
    /// Remaining bytes available to read.
    /// </summary>
    public long RemainingLength => Length - Position;

    /// <summary>
    /// If true, missing FName indices will be assigned a placeholder and added to the name table instead of throwing.
    /// Useful for parsing cryopod payloads which can reference constants not present in the embedded name table.
    /// </summary>
    public bool AllowDynamicNameTable { get; set; } = false;

    public bool IsCryopod { get; internal set; }

    public bool IsArkFile { get; internal set; }

    // --- Primitive reads implemented over spans / BinaryPrimitives ---



    public void WriteToFile(string fileName)
    {
        File.WriteAllBytes(fileName, _data.ToArray());
    }

    private ReadOnlySpan<byte> SpanAt(long offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > _data.Length)
            throw new EndOfStreamException($"Attempt to read {count} bytes at offset {offset} in {FileName} (length {_data.Length}).");
        return _data.Span.Slice((int)offset, count);
    }

    public byte[] ReadBytes(int count)
    {
        var span = SpanAt(Position, count);
        Position += count;
        return span.ToArray();
    }

    public byte ReadByte()
    {
        var span = SpanAt(Position, 1);
        Position += 1;
        return span[0];
    }

    public short ReadInt16()
    {
        var span = SpanAt(Position, 2);
        Position += 2;
        return BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public int ReadInt32()
    {
        var span = SpanAt(Position, 4);
        Position += 4;
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    public long ReadInt64()
    {
        var span = SpanAt(Position, 8);
        Position += 8;
        return BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public ushort ReadUInt16()
    {
        var span = SpanAt(Position, 2);
        Position += 2;
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public uint ReadUInt32()
    {
        var span = SpanAt(Position, 4);
        Position += 4;
        return BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public ulong ReadUInt64()
    {
        var span = SpanAt(Position, 8);
        Position += 8;
        return BinaryPrimitives.ReadUInt64LittleEndian(span);
    }

    public float ReadFloat()
    {
        var span = SpanAt(Position, 4);
        Position += 4;
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(span));
    }

    public double ReadDouble()
    {
        var span = SpanAt(Position, 8);
        Position += 8;
        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(span));
    }

    /// <summary>
    /// Reads a GUID (16 bytes) with ARK/Unreal byte reordering.
    /// </summary>
    public Guid ReadGuid()
    {
        const int size = 16;
        var bytes = SpanAt(Position, size);
        Position += size;
        return ConvertToGuid(bytes);
    }

    /// <summary>
    /// Convert a Guid to ARK byte ordering into a fresh byte[].
    /// (keeps signature from original but uses spans internally)
    /// </summary>
    public static byte[] ConvertToBytes(Guid guid)
    {
        Span<byte> tmp = stackalloc byte[16];
        guid.TryWriteBytes(tmp);

        // Reorder according to mapping: [0,1,2,3,6,7,4,5,11,10,9,8,15,14,13,12]
        Span<byte> outBuf = stackalloc byte[16];
        outBuf[0] = tmp[0];
        outBuf[1] = tmp[1];
        outBuf[2] = tmp[2];
        outBuf[3] = tmp[3];
        outBuf[4] = tmp[6];
        outBuf[5] = tmp[7];
        outBuf[6] = tmp[4];
        outBuf[7] = tmp[5];
        outBuf[8] = tmp[11];
        outBuf[9] = tmp[10];
        outBuf[10] = tmp[9];
        outBuf[11] = tmp[8];
        outBuf[12] = tmp[15];
        outBuf[13] = tmp[14];
        outBuf[14] = tmp[13];
        outBuf[15] = tmp[12];

        return outBuf.ToArray();
    }

    /// <summary>
    /// Convert ARK/Unreal ordered bytes to a Guid.
    /// </summary>
    public static Guid ConvertToGuid(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16) throw new ArgumentException("Need 16 bytes to convert to Guid", nameof(bytes));

        Span<byte> reordered = stackalloc byte[16];
        reordered[0] = bytes[0];
        reordered[1] = bytes[1];
        reordered[2] = bytes[2];
        reordered[3] = bytes[3];
        reordered[4] = bytes[6];
        reordered[5] = bytes[7];
        reordered[6] = bytes[4];
        reordered[7] = bytes[5];
        reordered[8] = bytes[11];
        reordered[9] = bytes[10];
        reordered[10] = bytes[9];
        reordered[11] = bytes[8];
        reordered[12] = bytes[15];
        reordered[13] = bytes[14];
        reordered[14] = bytes[13];
        reordered[15] = bytes[12];

        return new Guid(reordered);
    }

    /// <summary>
    /// Reads a length-prefixed string.
    /// Format: Int32 (length including null terminator) + UTF8 bytes + null terminator
    /// Uses span-based decoding and ArrayPool for larger strings.
    /// </summary>
    public string ReadString()
    {
        var startPosition = Position;
        var length = ReadInt32();
        if (length == 0) return string.Empty;

        int byteCount;
        Encoding encoding;

        if (length < 0)
        {
            // Unicode (UCS-2)
            int charCount = checked(-length);
            byteCount = charCount * 2;
            encoding = Encoding.Unicode;
        }
        else
        {
            // UTF-8
            byteCount = length;
            encoding = Encoding.UTF8;
        }

        if (byteCount > RemainingLength)
        {
            throw new Exception($"Corrupted file: {FileName}. Position {startPosition} requires {byteCount} bytes, but only {RemainingLength} remain.");
        }

        // Slice the span, excluding the null terminator (assumed last 1 or 2 bytes)
        int terminatorSize = (length < 0) ? 2 : 1;
        int usefulByteCount = Math.Max(0, byteCount - terminatorSize);
        var byteSpan = SpanAt(Position, usefulByteCount);

        // This is the magic part: it decodes and pools in one step
        string value = StringPool.Shared.GetOrAdd(byteSpan, encoding);

        Position += byteCount;
        return value;
    }

    /// <summary>
    /// Reads an FName (Unreal Engine name).
    /// Format: Int32 (name index) + Int32 (instance number)
    /// Immediately resolves the string value from the name table.
    /// </summary>
    public FName ReadFName()
    {
        if ((IsCryopod || IsArkFile) && NameTable.Count == 0)
        {
            var name = ReadString();
            return new FName(int.MinValue, 0, name);
        }

        var nameIndex = ReadInt32();
        int instanceNumber = ReadInt32();

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
    /// Reads an FPropertyTypeName recursively (Unreal Engine 5.5+).
    /// </summary>
    public FPropertyTypeName ReadPropertyTypeName(int depth = 0)
    {
        if (depth > 10)
        {
            throw new InvalidDataException($"Exceeded maximum recursion depth while reading FPropertyTypeName at position {Position}. This may indicate a corrupted save file.");
        }

        var typeName = ReadFName();
        var parameterCount = ReadInt32();

        if (parameterCount * 12 > RemainingLength)
        {
            throw new AsaDataException($"Invalid type parameter count read at offset {Position - 4} of {FileName}");
        }

        var parameters = parameterCount == 0
            ? Array.Empty<FPropertyTypeName>()
            : new FPropertyTypeName[parameterCount];

        for (int i = 0; i < parameterCount; i++)
        {
            parameters[i] = ReadPropertyTypeName(depth + 1);
        }

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
            // No unmanaged resources to free for memory-backed reader.
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}