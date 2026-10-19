# Public API changes

This update adds player currency and Genesis Part 1 best mission scores. All additions are in the `AsaSavegameToolkit.Porcelain` namespace. No existing public members were removed or renamed.

## New player properties

```csharp
public int Hexagons { get; set; }
public Dictionary<string, MissionScore> Genesis1MissionScores { get; set; }
```

### `Player.Hexagons`

- Exposes the current hexagon balance.
- Profiles read the top-level `HexagonCount` property, which is a sibling of `MyData`, not nested inside it.
- Pawn records read `PlayerHexagonCount`.
- When loading a profile with a matching pawn, an explicitly saved pawn balance takes precedence, including zero. An absent pawn balance preserves the profile balance.
- Defaults to zero when no balance is saved.

### `Player.Genesis1MissionScores`

- Maps full mission-tag strings (blueprint paths) to the mission's saved best score.
- Reads `MyData.LatestMissionScores`, using each valid entry's `BestScore`, not `LatestScore`.
- Preserves both score fields without combining them or choosing the numerically greatest value. Their interpretation depends on the mission; a higher number does not necessarily mean a better result.
- Defaults to an empty dictionary when no mission scores are saved.
- Excludes entries marked as having no valid score. Numeric fields omitted from a saved score default to zero.
- Does **not** represent the mission-completion ledger. A completed mission may have no score entry.

## New public type

```csharp
public record MissionScore(int IntValue, float FloatValue);
```

The record exposes `IntValue` as `int` and `FloatValue` as `float`, with record value equality and deconstruction.

Example:

```csharp
foreach (var (missionTag, bestScore) in player.Genesis1MissionScores)
{
    Console.WriteLine($"{missionTag}: {bestScore.IntValue}, {bestScore.FloatValue}");
}
```

## Existing API behavior fixes

- `Player.TotalSkillPoints` now reads the correctly spelled serialized field `PlayerState_TotalSkillPoints`. Previously, populated saves could incorrectly report zero.
- `ObjectReference.Read` now recognizes the world-save `00 01` asset-path marker in addition to the previously accepted `01 00` marker. This fixes parsing failures for dropped cryopod mesh references without changing the method signature.

## Scope and compatibility

- Existing callers require no source changes to continue using the API.
- Serializers that automatically include public properties may now emit the two additional player properties.
- Genesis mission completions, repaired glitches, and persistent buff details in secondary profile objects are still not exposed as typed properties by the normal profile reader. See [profile-format notes](RecordTypes/ProfileFile.md#secondary-profile-objects).
- Save fixture consolidation and added v16 fixture coverage do not introduce further public API signatures.

## Validation

The full test suite passed: **305 passed, 3 existing debug tests skipped, 0 failed**.
