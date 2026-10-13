using AsaSavegameToolkit.Plumbing.Readers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsaSavegameToolkit.Plumbing.Records;

/// <summary>
/// Represents the GameModeCustomBytes record from the custom table.
/// Contains tribe and player profile data embedded in the main save file
/// (used when the server runs with the -usestore flag instead of separate .arktribe/.arkprofile files).
/// </summary>
public class GameModeCustomBytesRecord
{
    /// <summary>Flags field from the outer header (always 1 in observed saves).</summary>
    public int Flags { get; init; }

    /// <summary>Tribe entries parsed from the index.</summary>
    public required List<EmbeddedTribeEntry> Tribes { get; init; }

    /// <summary>Player profile entries parsed from the index.</summary>
    public required List<EmbeddedProfileEntry> Profiles { get; init; }

    /// <summary>
    /// Reads the GameModeCustomBytes record from the archive.
    ///
    /// Outer layout:
    ///   [4] flags
    ///   [4] tribeHeaderOffset  — byte offset from record start to the index area (before the +12 base)
    ///   [4] tribeCount
    ///   [tribe data blobs starting here at tribeDataStart = 12]
    ///   ...
    ///   [tribe header index at tribeHeaderOffset + 12]
    ///     for each tribe (16 bytes): UInt32 TribeId, Int32 Unknown, Int32 Offset, Int32 Size
    ///   [profile section header immediately after tribe entries]
    ///     [4] profileHeaderRelOffset  — relative to (this field's start + 4)
    ///     [4] profileCount
    ///   [profile data blobs starting at profileSectionHeader + 8]
    ///   [profile header index at profileSectionHeader + 4 + profileHeaderRelOffset]
    ///     for each profile (16 bytes): Int64 EosId, Int32 Offset, Int32 Size
    ///
    /// Each tribe/profile blob uses the old archive-version-7 format
    /// (not the ASA GameObjectRecord format) — raw bytes are stored for deferred parsing.
    /// </summary>
    public static GameModeCustomBytesRecord Read(Readers.AsaArchive archive)
    {
        var startPosition = archive.Position;


        // --- Outer header (12 bytes) ---
        var flags             = archive.ReadInt32();
        var tribeHeaderOffset = archive.ReadInt32();
        var tribeCount        = archive.ReadInt32();

        // Tribe blob data starts right after the 12-byte header
        var tribeDataStart = archive.Position;
        var tribeHeaderStart = tribeDataStart + tribeHeaderOffset;

        //the size of a tribe header entry is 16 bytes, so the total size of the tribe header index is tribeCount * 16
        if (tribeCount * 16 > archive.Length - tribeHeaderStart)
        {
            throw new AsaDataException($"Tribe count {tribeCount} is too large for the remaining data length {archive.Length - tribeHeaderStart}.");
        }

        // --- Tribe header index ---
        archive.Position = tribeHeaderStart;
        var tribeHeaders = new List<TribeHeaderEntry>(tribeCount);
        for (int i = 0; i < tribeCount; i++)
        {
            tribeHeaders.Add(new TribeHeaderEntry(
                TribeId: archive.ReadUInt32(),
                Unknown: archive.ReadInt32(),
                Offset:  archive.ReadInt32(),
                Size:    archive.ReadInt32()
            ));
        }

        // --- Profile section header (immediately after tribe entries) ---
        var profileHeaderOffset  = archive.ReadInt32();
        // The profile header offset is relative to the end of this field
        var profileHeaderStart = archive.Position;
        var profileHeaderPosition  = profileHeaderStart + profileHeaderOffset;

        var profileCount         = archive.ReadInt32();
        // the minimim size of the profile header is profileCount * 16, so we can do a quick check to avoid seeking to an invalid position later
        //if (profileCount * 16 > archive.Length - profileHeaderPosition)
        //{
        //    throw new AsaDataException($"Profile count {profileCount} is too large for the remaining data length {archive.Length - profileHeaderPosition}.");
        //}

        // Profile blob data starts right after the 8-byte profile section header
        var profileDataStart = archive.Position;

        archive.Position = profileHeaderPosition;
        var someInt = archive.ReadInt32();

        var profileHeaders = new List<ProfileHeaderEntry>(profileCount);
        for (int i = 0; i < profileCount; i++)
        {
            profileHeaders.Add(new ProfileHeaderEntry(
                EntryId: archive.ReadUInt64(), 
                Offset: archive.ReadInt32(),                
                Size: archive.ReadInt32()
            ));
        }

        // --- Slice tribe blobs ---
        // tribe byte checks get complicated, so lets just cap it at the length of the archive to avoid seeking to an invalid position
        // tribe data has size internally, so we'll just sum and check
        var totalTribeDataSize = tribeHeaders.Sum(hdr => hdr.Size);
        var diffSize = archive.Length - tribeDataStart;
        if (totalTribeDataSize > archive.Length - tribeDataStart)
        {
            throw new AsaDataException($"Total tribe data size {totalTribeDataSize} is too large for the remaining data length {archive.Length - tribeDataStart}.");
        }

        // profiles are checked the same way
        var totalProfileDataSize = profileHeaders.Sum(hdr => hdr.Size);
        if (totalProfileDataSize > archive.Length - profileDataStart)
        {
            throw new AsaDataException($"Total profile data size {totalProfileDataSize} is too large for the remaining data length {archive.Length - profileDataStart}.");
        }


        List<EmbeddedTribeEntry> tribes = new List<EmbeddedTribeEntry>();
        foreach (var hdr in tribeHeaders)
        {
            if (hdr.TribeId == 0) continue; // deleted tribe entry, skip

            archive.Position = tribeDataStart + hdr.Offset;
            
            tribes.Add(new EmbeddedTribeEntry()
            {
                TribeId = hdr.TribeId,
                RawBlob = archive.ReadBytes(hdr.Size)
            });
        }


        // --- Slice profile blobs ---
        List<EmbeddedProfileEntry> profiles = new List<EmbeddedProfileEntry>();
        foreach (var hdr in profileHeaders)
        {
            if (hdr.EntryId == 0) continue; // deleted profile entry, skip

            archive.Position = profileDataStart + hdr.Offset;
            profiles.Add(new EmbeddedProfileEntry()
            {
                EosId = (long)hdr.EntryId,
                RawBlob = archive.ReadBytes(hdr.Size)
            });
        }


        return new GameModeCustomBytesRecord
        {
            Flags    = flags,
            Tribes   = tribes,
            Profiles = profiles,
        };
    }


    private readonly record struct TribeHeaderEntry(uint TribeId, int Unknown, int Offset, int Size);
    private readonly record struct ProfileHeaderEntry(ulong EntryId, int Offset, int Size);
}
public class EmbeddedTribeEntry
{
    /// <summary>Unique tribe identifier.</summary>
    public required uint TribeId { get; init; }

    /// <summary>Raw bytes of the embedded tribe archive (archive-version-7 format).</summary>
    public required byte[] RawBlob { get; init; }
}

/// <summary>A player profile blob sliced from the GameModeCustomBytes record.</summary>
public class EmbeddedProfileEntry
{
    /// <summary>Epic Online Services player ID.</summary>
    public required long EosId { get; init; }

    /// <summary>Raw bytes of the embedded profile archive (archive-version-7 format), empty if the entry is deleted.</summary>
    public required byte[] RawBlob { get; init; }
}