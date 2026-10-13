using AsaSavegameToolkit.Plumbing.Properties;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;

namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents a tribe in an ARK save file.
/// Tribe records appear in both the main .ark save and .arktribe files.
/// </summary>
/// <remarks>
/// Full tribe data (members, admin list, logs) lives in .arktribe files.
/// This class covers the tribe record from the main .ark save.
/// </remarks>
public class Tribe
{
    public Tribe()
    {
    }

    public Tribe(GameObjectRecord record)
    {
        Record = record;
        Id = record.Uuid;
        PopulateFromRecord(record);
    }

    /// <summary>
    /// The underlying raw record for direct property access.
    /// </summary>
    public GameObjectRecord? Record { get; set; }

    /// <summary>
    /// Unique object ID of this tribe record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The tribe's display name.
    /// </summary>
    public string? TribeName { get; set; }

    /// <summary>
    /// The numeric tribe ID.
    /// </summary>
    public int TribeId { get; set; }

    /// <summary>
    /// Owner player data ID (tribe founder/owner).
    /// </summary>
    public long? OwnerPlayerDataId { get; set; }

    public string[] MemberNames { get; set; } = [];
    public string[] MemberIds { get; set; } = [];
    public string[] LogLines { get; set; } = [];

    public override string ToString() => TribeName ?? Record?.GetClassName() ?? string.Empty;

    public static Tribe Create(GameObjectRecord tribeRecord)
    {
        var tribe = new Tribe
        {
            Record = tribeRecord,
            Id = tribeRecord.Uuid
        };
        tribe.PopulateFromRecord(tribeRecord);
        return tribe;
    }

    private void PopulateFromRecord(GameObjectRecord record)
    {
        var props = record.Properties;
        if (props.Get<StructProperty>("TribeData")?.Value is List<Property> structProps)
        {
            props = structProps;
        }

        TribeId = props.Get<int>("TribeID");
        TribeName = props.Get<string>("TribeName");

        if (props.TryGet<long>("OwnerPlayerDataID", out var ownerLong))
        {
            OwnerPlayerDataId = ownerLong;
        }
        else if (props.TryGet<uint>("OwnerPlayerDataID", out var ownerUint))
        {
            OwnerPlayerDataId = ownerUint;
        }
        else if (props.TryGet<uint>("OwnerPlayerDataId", out var ownerUintAlt))
        {
            OwnerPlayerDataId = ownerUintAlt;
        }

        var memberIdsProp = props.Get<ArrayProperty>("MembersPlayerDataID");
        var memberNamesProp = props.Get<ArrayProperty>("MembersPlayerName");
        if (memberIdsProp?.Value != null && memberNamesProp?.Value != null)
        {
            var idCount = Math.Min(memberNamesProp.Value.Count, memberIdsProp.Value.Count);
            MemberIds = new string[idCount];
            MemberNames = new string[idCount];

            for (var x = 0; x < idCount; x++)
            {
                MemberIds[x] = memberIdsProp.Value[x]?.ToString() ?? "";
                MemberNames[x] = memberNamesProp.Value[x]?.ToString() ?? "";
            }
        }

        var tribeLogProperty = props.Get<ArrayProperty>("TribeLog");
        if (tribeLogProperty?.Value != null)
        {
            var lineCount = tribeLogProperty.Value.Count;
            LogLines = new string[lineCount];
            for (var x = 0; x < lineCount; x++)
            {
                LogLines[x] = tribeLogProperty.Value[x]?.ToString() ?? "";
            }
        }
    }
}
