using AsaSavegameToolkit.Plumbing;
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
    public long? OwnerPlayerDataId{ get; set; }

    public string[] MemberNames { get; set; } = [];
    public string[] MemberIds { get; set; } = [];
    public string[] LogLines { get; set; } = [];
    
    public override string ToString() => TribeName;


    /// <summary>
    /// Creates a new Tribe instance from a record.
    /// </summary>
    internal static Tribe? Create(GameObjectRecord tribeRecord)
    {
        var properties = tribeRecord.Properties.Get<StructProperty>("TribeData").Value as List<Property>;
        if (properties == null || properties.Count == 0) return null;

        var tribeId = properties.Get<int>("TribeID");
        var tribeName = properties.Get<string>("TribeName")??"";
        var ownerPlayerId = properties.Get<uint>("OwnerPlayerDataId");
        string[] memberNames = [];
        string[] memberIds = [];

        var memberIdsProperty = properties.Get<ArrayProperty?>("MembersPlayerDataID");
        var memberNamesProperty = properties.Get<ArrayProperty>("MembersPlayerName");
        if (memberIdsProperty != null && memberNamesProperty!=null)
        {
            var idCount = memberNamesProperty.Value.Count;
            memberIds = new string[idCount];
            memberNames = new string[idCount];


            for (int x = 0; x < idCount; x++)
            {
                memberIds[x] = memberIdsProperty.Value[x].ToString();
                memberNames[x] = memberNamesProperty.Value[x].ToString();
            }
        }

        string[] tribeLogs = [];
        var tribeLogProperty = properties.Get<ArrayProperty?>("TribeLog");
        if(tribeLogProperty != null)
        {
            var lineCount = tribeLogProperty.Value.Count;
            tribeLogs = new string[lineCount];
            for (int x = 0; x < lineCount; x++)
            {
                tribeLogs[x] = tribeLogProperty.Value[x].ToString();
            }
        }

        var logIndex = properties.Get<int>("LogIndex");        

        return new Tribe
        {
            Id = tribeRecord.Uuid,
            TribeId = tribeId,
            TribeName = tribeName,
            OwnerPlayerDataId = ownerPlayerId,
            MemberIds = memberIds,
            MemberNames = memberNames,
            LogLines = tribeLogs


        };

    }

}
