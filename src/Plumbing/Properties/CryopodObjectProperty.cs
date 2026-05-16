

using AsaSavegameToolkit.Plumbing.Readers;

namespace AsaSavegameToolkit.Plumbing.Properties;

internal class CryopodObjectProperty : Property<int>
{
    internal static CryopodObjectProperty Read(AsaArchive archive, PropertyTag tag)
    {
        return new CryopodObjectProperty
        {
            Tag = tag,
            Value = ReadValue(archive)
        };
    }

    private static int ReadValue(AsaArchive archive)
    {
        var unknown = archive.ReadInt32(); // expect 0

        return unknown == -1 ? unknown : archive.ReadInt32(); // should always be 1
    }
}