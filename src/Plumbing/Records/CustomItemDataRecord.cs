
using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Readers;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Plumbing.Records;

public class CustomItemDataRecord
{
    public byte[][] CustomDataBytes { get; set; } = [];
    public double[] CustomDataDoubles { get; set; } = [];
    public string[] CustomDataStrings { get; set; } = [];
    public float[] CustomDataFloats { get; set; } = [];
    public ObjectReference[] CustomDataObjects { get; set; } = [];
    public ObjectReference[] CustomDataClasses { get; set; } = [];
    public FName[] CustomDataNames { get; set; } = [];
    public FName CustomDataName { get; set; }
    public FSoftObjectPath[] CustomDataSoftClasses { get; set; } = [];

    internal static CustomItemDataRecord Read(AsaArchive archive)
    {
        var properties = Property.ReadList(archive);
        var record = new CustomItemDataRecord();

        // CustomDataBytes: CustomItemByteArrays {
        //   ByteArrays: CustomItemByteArray[] {
        //     Bytes: byte[]
        //   }
        // }
        var customDataBytes = properties.Get<IList<Property>>("CustomDataBytes");
        var byteArrays = customDataBytes?.Get<object[]>("ByteArrays") ?? [];

        record.CustomDataBytes = new byte[byteArrays.Length][];
        for(var i = 0; i < byteArrays.Length; i++)
        {
            var customItemByteArray = byteArrays[i] as IList<Property>;
            record.CustomDataBytes[i] = customItemByteArray?.Get<byte[]>("Bytes") ?? [];
        }

        // CustomDataDoubles: CustomItemDoubles {
        //   Doubles: Double[]
        // }
        var customDataDoubles = properties.Get<IList<Property>>("CustomDataDoubles");
        record.CustomDataDoubles = customDataDoubles?.Get<object[]>("Doubles")?
            .OfType<double>()
            .ToArray() ?? [];

        record.CustomDataStrings = properties.Get<object[]>("CustomDataStrings")?
            .OfType<string>()
            .ToArray() ?? [];

        record.CustomDataFloats = properties.Get<object[]>("CustomDataFloats")?
            .OfType<float>()
            .ToArray() ?? [];

        record.CustomDataObjects = properties.Get<object[]>("CustomDataObjects")?
            .OfType<ObjectReference>()
            .ToArray() ?? [];

        record.CustomDataClasses = properties.Get<object[]>("CustomDataClasses")?
            .OfType<ObjectReference>()
            .ToArray() ?? [];

        record.CustomDataNames = properties.Get<object[]>("CustomDataNames")?
            .OfType<FName>()
            .ToArray() ?? [];

        record.CustomDataSoftClasses = properties.Get<object[]>("CustomDataSoftClasses")?
            .OfType<FSoftObjectPath>()
            .ToArray() ?? [];

        record.CustomDataName = properties.Get<FName>("CustomDataName");

        return record;
    }
}
