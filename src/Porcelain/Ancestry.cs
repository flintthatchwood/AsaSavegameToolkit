namespace AsaSavegameToolkit.Porcelain;

/// <summary>
/// Represents an entry in creature ancestry lineage.
/// </summary>
public record Ancestry(int TreeDepth, long MaleId, string MaleName, long FemaleId, string FemaleName);
