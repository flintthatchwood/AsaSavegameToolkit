
namespace AsaSavegameToolkit.Porcelain;

internal class TeamInfo
{
    private const int PlayerStart = 50_000;
    private const int TribeStart = 1_000_000_000;
    private const int BreedingId = 2_000_000_000;


    // non-player teams are not tamed, but all player, tribe, and breeding teams are
    internal static bool IsTamed(int teamId) => teamId >= PlayerStart;

    public static TeamType GetTeamType(int teamId)
    {
        return teamId switch
        {
            < PlayerStart => TeamType.NonPlayer,
            < TribeStart => TeamType.Player,
            < BreedingId => TeamType.Tribe,
            BreedingId => TeamType.Breeding,
            _ => TeamType.Unknown
        };
    }
}

public enum TeamType
{
    NonPlayer,
    Player,
    Tribe,
    Breeding,
    Unknown
}