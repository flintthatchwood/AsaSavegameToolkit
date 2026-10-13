using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;

namespace AsaSavegameToolkit.Plumbing.Utilities;
public static class PlumbingExtensions
{
    private static readonly Dictionary<int, int> ArkGuidTranslation = new()
    {
        { 0, 0 }, { 1, 1 }, { 2, 2 }, { 3, 3 },
        { 4, 6 }, { 5, 7 }, { 6, 4 }, { 7, 5 },
        { 8, 11 }, { 9, 10 }, { 10, 9 }, { 11, 8 },
        { 12, 15 }, { 13, 14 }, { 14, 13 }, { 15, 12 }
    };

    public static byte[] ToArkByteArray(this Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        byte[] result = new byte[16];

        foreach (var (key, value) in ArkGuidTranslation)
        {
            result[key] = bytes[value];
        }

        return result;
    }

    public static Guid ToArkGuid(this byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length != 16)
        {
            throw new ArgumentException("Byte array must be exactly 16 bytes", nameof(bytes));
        }

        byte[] temp = new byte[16];

        foreach (var (key, value) in ArkGuidTranslation)
        {
            temp[value] = bytes[key];
        }

        return new Guid(temp);
    }

    public static string ToHexString(this int value, string separator = "-") => BitConverter.GetBytes(value).ToHexString(separator, !BitConverter.IsLittleEndian);

    public static string ToHexString(this IEnumerable<byte> bytes, string separator = "-", bool reverse = false) => bytes.ToArray().ToHexString(separator, reverse);

    public static string ToHexString(this byte[] bytes, string separator = "-", bool reverse = false)
    {
        if (reverse)
        {
            var newBytes = new byte[bytes.Length];
            bytes.CopyTo(newBytes, 0);
            Array.Reverse(newBytes);
            bytes = newBytes;
        }

        var hexString = BitConverter.ToString(bytes);
        return separator == "-" ? hexString : hexString.Replace("-", separator);
    }

    /// <summary>
    /// Determines whether a GameObjectRecord represents a cryopod that actually holds a creature.
    /// Criteria:
    ///   - CustomItemDatas contains an entry with CustomDataName == "Dino" AND CustomDataBytes[0] has data
    /// </summary>
    public static bool HasCryoCreature(this GameObjectRecord record)
    {
        var customItemDatas = record.Properties.Get<object[]>("CustomItemDatas")?.OfType<CustomItemDataRecord>();
        if (customItemDatas == null)
            return false;

        return customItemDatas.Any(cid => cid.CustomDataBytes.Length > 0
                                       && cid.CustomDataBytes[0].Length > 0);
    }

    public static bool TryGet<T>(this IEnumerable<Property> propertyContainer, string name, int index, [NotNullWhen(true)] out T? result) where T : notnull
    {
        foreach (var property in propertyContainer)
        {
            if (property.Tag.Name.FullName == name && property.Tag.ArrayIndex == index)
            {
                if (property is T matchingPropertyType)
                {
                    result = matchingPropertyType;
                    return true;
                }

                if (property.GetValue() is T matchingValueType)
                {
                    result = matchingValueType;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    public static bool TryGet<T>(this IEnumerable<Property> propertyContainer, string name, [NotNullWhen(true)] out T? result) where T : notnull
    {
        return TryGet(propertyContainer, name, 0, out result);
    }

    public static T? Get<T>(this IEnumerable<Property> propertyContainer, string name, int index = 0) where T : notnull
    {
        TryGet<T>(propertyContainer, name, index, out var result);

        return result;
    }

    public static bool HasAny(this IEnumerable<Property> propertyContainer, string name)
    {
        return propertyContainer.Any(p => p.Tag.Name.FullName == name);
    }
}
