using AsaSavegameToolkit.Plumbing;

namespace AsaSavegameToolkit.Tests.Utilities;

[TestClass]
public class WildcardInflaterTests
{
    [TestMethod]
    public void Decompress_WithEscapeByte_ReturnsNextByteLiterally()
    {
        // 0xF0 is the escape byte, followed by 0xF0 should return 0xF0
        byte[] input = [0xF0, 0xF0];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.HasCount(1, result);
        Assert.AreEqual(0xF0, result[0]);
    }

    [TestMethod]
    public void Decompress_WithSwitchByte_SplitsNextByteIntoNibbles()
    {
        // 0xF1 is the switch byte, 0xAB should become 0xFA and 0xFB
        byte[] input = [0xF1, 0xAB];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.HasCount(2, result);
        Assert.AreEqual(0xFA, result[0]); // 0xF0 | (0xAB >> 4) = 0xF0 | 0x0A = 0xFA
        Assert.AreEqual(0xFB, result[1]); // 0xF0 | (0xAB & 0x0F) = 0xF0 | 0x0B = 0xFB
    }

    [TestMethod]
    public void Decompress_WithRunLengthByte_InsertsZeros()
    {
        // 0xF3 should insert 3 zeros, followed by 0x42
        byte[] input = [0xF3, 0x42];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.HasCount(4, result);
        Assert.AreEqual(0x00, result[0]);
        Assert.AreEqual(0x00, result[1]);
        Assert.AreEqual(0x00, result[2]);
        Assert.AreEqual(0x42, result[3]);
    }

    [TestMethod]
    public void Decompress_WithSpecialPatternByte_InsertsCorrectPattern()
    {
        // 0xFF followed by 0x12 and 0x34 should produce: 0,0,0,0x12,0,0,0,0x34,0,0,0
        byte[] input = [0xFF, 0x12, 0x34];
        
        byte[] result = WildcardInflater.Inflate(input);

        byte[] expected = [0, 0, 0, 0x12, 0, 0, 0, 0x34, 0, 0, 0];
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Decompress_WithNormalBytes_ReturnsAsIs()
    {
        byte[] input = [0x12, 0x34, 0x56];
        
        byte[] result = WildcardInflater.Inflate(input);

        CollectionAssert.AreEqual(input, result);
    }

    [TestMethod]
    public void Decompress_WithMixedData_DecompressesCorrectly()
    {
        // Mix of normal bytes and run-length encoding
        byte[] input = [0x12, 0xF3, 0x34]; // 0x12, three zeros, 0x34
        
        byte[] result = WildcardInflater.Inflate(input);

        byte[] expected = [0x12, 0x00, 0x00, 0x00, 0x34];
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Decompress_WithMultipleRunLengthBytes_WorksCorrectly()
    {
        // 0xF2 = 2 zeros, 0xF5 = 5 zeros
        byte[] input = [0xF2, 0xF5];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.HasCount(7, result); // 2 + 5
        foreach (var b in result)
        {
            Assert.AreEqual(0, b);
        }
    }

    [TestMethod]
    public void Decompress_WithEscapedControlBytes_ReturnsLiterally()
    {
        // Escape various control bytes
        byte[] input = [0xF0, 0xF1, 0xF0, 0xF2, 0xF0, 0xFF];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.HasCount(3, result);
        Assert.AreEqual(0xF1, result[0]);
        Assert.AreEqual(0xF2, result[1]);
        Assert.AreEqual(0xFF, result[2]);
    }

    [TestMethod]
    public void Decompress_WithComplexPattern_DecompressesCorrectly()
    {
        // Complex test: normal + escape + switch + run-length
        byte[] input = [0x42, 0xF0, 0xFF, 0xF1, 0x12, 0xF3];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.IsNotEmpty(result);
        Assert.AreEqual(0x42, result[0]); // Normal byte
        Assert.AreEqual(0xFF, result[1]); // Escaped 0xFF
        Assert.AreEqual(0xF1, result[2]); // 0xF1 split: high nibble
        Assert.AreEqual(0xF2, result[3]); // 0xF1 split: low nibble
        Assert.AreEqual(0x00, result[4]); // First zero from 0xF3
        Assert.AreEqual(0x00, result[5]); // Second zero
        Assert.AreEqual(0x00, result[6]); // Third zero
    }

    [TestMethod]
    public void Decompress_WithOffsetAndCount_WorksCorrectly()
    {
        byte[] input = [0xFF, 0x12, 0xF3, 0x34, 0xFF]; // Only decompress middle part
        
        byte[] result = WildcardInflater.Decompress(input, 2, 2);

        byte[] expected = [0x00, 0x00, 0x00, 0x34];
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Decompress_WithEmptyArray_ReturnsEmptyArray()
    {
        byte[] input = [];
        
        byte[] result = WildcardInflater.Inflate(input);

        Assert.IsEmpty(result);
    }



    [TestMethod]
    public void Decompress_WithSpan_WorksCorrectly()
    {
        byte[] input = [0x12, 0xF3, 0x34];
        ReadOnlySpan<byte> span = input.AsSpan();
        
        byte[] result = WildcardInflater.Decompress(span);

        byte[] expected = [0x12, 0x00, 0x00, 0x00, 0x34];
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Decompress_WithAllRunLengthCodes_WorksCorrectly()
    {
        // Test all run-length codes from 0xF2 (2 zeros) to 0xFE (14 zeros)
        for (byte code = 0xF2; code <= 0xFE; code++)
        {
            byte[] input = [code];
            int expectedZeros = code & 0x0F;
            
            byte[] result = WildcardInflater.Inflate(input);

            Assert.HasCount(expectedZeros, result, $"Failed for code 0x{code:X2}");
            foreach (var b in result)
            {
                Assert.AreEqual(0, b, $"Non-zero byte found for code 0x{code:X2}");
            }
        }
    }
}
