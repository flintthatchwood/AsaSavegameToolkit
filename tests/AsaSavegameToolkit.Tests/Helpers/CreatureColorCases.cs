namespace AsaSavegameToolkit.Tests.Helpers;

public static class CreatureColorCases
{
    public static IEnumerable<object[]> All()
    {
        foreach (var version in new[] { 13, 14, 16 })
        {
            // [version, objectId, isCryopodded, expected color values]
            // Wild Dodo, 6 color regions
            yield return [version, "9521483c-488a-a56d-83a8-01b3b91dcba8", false, new byte?[] { 63, 71, 64, 77, 37, 71 }];
            // Wild Megatherium, 2 color regions
            yield return [version, "6785e1f4-47b0-ef48-21a9-c79fb4d46686", false, new byte?[] { 37, null, null, null, null, 32 }];
            // Cryopodded Megalodon, 3 color regions
            yield return [version, "742d59d7-4ccd-e1cf-7448-f0bf7c18a22d", true, new byte?[] { 66, null, null, null, 210, 28 }];
            // Cryopodded Ptera, 5 color regions
            yield return [version, "b92a238a-4fae-b437-14b2-67aacf6f78e8", true, new byte?[] { 81, null, 56, 53, 36, 36 }];
        }
    }
}
