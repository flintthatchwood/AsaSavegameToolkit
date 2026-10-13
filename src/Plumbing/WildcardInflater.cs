namespace AsaSavegameToolkit.Plumbing;

/// <summary>
/// Utility methods for decompressing ARK cryopod data encoded with a proprietary compression format.
/// This format uses control bytes (0xF0-0xFF) to encode run-length sequences and special patterns.
/// </summary>
public static class WildcardInflater
{
    private enum ReadState
    {
        None,
        Escape,
        Switch
    }

    /// <summary>
    /// Decompresses a byte array that was compressed using ARK's cryopod compression format.
    /// </summary>
    /// <param name="compressedData">The compressed byte array</param>
    /// <returns>The decompressed byte array</returns>
    public static byte[] Inflate(byte[] compressedData)
    {
        if (compressedData == null)
            throw new ArgumentNullException(nameof(compressedData));

        return Decompress(compressedData.AsSpan());
    }

    /// <summary>
    /// Decompresses a byte array segment that was compressed using ARK's cryopod compression format.
    /// </summary>
    /// <param name="compressedData">The array containing compressed data</param>
    /// <param name="offset">The offset in the array where compressed data starts</param>
    /// <param name="count">The number of bytes to decompress</param>
    /// <returns>The decompressed byte array</returns>
    public static byte[] Decompress(byte[] compressedData, int offset, int count)
    {
        if (compressedData == null)
            throw new ArgumentNullException(nameof(compressedData));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (offset + count > compressedData.Length)
            throw new ArgumentException("Offset and count exceed array bounds");

        var segment = new byte[count];
        Array.Copy(compressedData, offset, segment, 0, count);
        return Inflate(segment);
    }

    /// <summary>
    /// Decompresses a ReadOnlySpan of bytes that was compressed using ARK's cryopod compression format.
    /// </summary>
    /// <param name="compressedData">The compressed data span</param>
    /// <returns>The decompressed byte array</returns>
    public static byte[] Decompress(ReadOnlySpan<byte> compressedData)
    {
        // Pre-size at ~4× the compressed length to avoid most reallocations.
        // MemoryStream is far cheaper than List<byte>: a single backing array vs
        // per-element boxing overhead, and Write(Span<T>) avoids per-byte dispatch.
        using var result = new MemoryStream(Math.Max(256, compressedData.Length * 4));
        var inputIndex = 0;
        var readState = ReadState.None;
        // Pre-allocate scratch buffers outside the loop to satisfy CA2014.
        Span<byte> zeros = stackalloc byte[14];   // max RLE run (0xF2-0xFE, value 2-14)
        Span<byte> pattern = stackalloc byte[11]; // 0xFF pattern: 0,0,0,b1,0,0,0,b2,0,0,0

        while (inputIndex < compressedData.Length)
        {
            byte next = compressedData[inputIndex++];

            // Handle Switch state: split byte into two nibbles
            if (readState == ReadState.Switch)
            {
                result.WriteByte((byte)(0xF0 | ((next & 0xF0) >> 4)));
                result.WriteByte((byte)(0xF0 | (next & 0x0F)));
                readState = ReadState.None;
                continue;
            }

            if (readState == ReadState.None)
            {
                // 0xF0: Escape byte - read next byte literally
                if (next == 0xF0)
                {
                    readState = ReadState.Escape;
                    continue;
                }

                // 0xF1: Switch to nibble splitting mode
                if (next == 0xF1)
                {
                    readState = ReadState.Switch;
                    continue;
                }

                // 0xF2-0xFE: Run-length encoding for zeros (2-14 zeros)
                if (next >= 0xF2 && next < 0xFF)
                {
                    int byteCount = next & 0x0F;
                    result.Write(zeros[..byteCount]);
                    continue;
                }

                // 0xFF: Special pattern - reads 2 bytes and outputs: 0,0,0,b1,0,0,0,b2,0,0,0
                if (next == 0xFF)
                {
                    if (inputIndex + 1 >= compressedData.Length)
                        throw new InvalidDataException("Unexpected end of data while reading 0xFF pattern");

                    byte b1 = compressedData[inputIndex++];
                    byte b2 = compressedData[inputIndex++];

                    // reuse pre-allocated pattern span (zeros already cleared from stackalloc)
                    pattern[3] = b1;
                    pattern[7] = b2;
                    result.Write(pattern);
                    continue;
                }
            }

            // Return the byte as-is
            readState = ReadState.None;
            result.WriteByte(next);
        }

        return result.ToArray();
    }
}
