# Contributing

## Project Layout

```
src/
  Plumbing/   # Low-level binary readers and primitives
  Porcelain/  # High-level, typed domain models
tests/        # MSTest unit tests
docs/         # Architecture notes and format references
```

## Plumbing vs. Porcelain

**Plumbing** (`src/Plumbing/`) is the low-level parsing layer. It contains `AsaArchive` (the raw byte reader), the property and record readers in `Properties/`, `Records/`, and `Readers/`, and primitive value types like `FName`, `FVector`, and `FColor`. Plumbing code is responsible for decoding bytes from the archive and returning parsed primitives or property objects. Think of it as a raw deserialization layer. It has no knowledge of domain concepts like "Creature" or "Player".

**Porcelain** (`src/Porcelain/`) is the high-level domain layer. It contains the typed models (`Creature`, `Player`, `Structure`, `Inventory`, `Tribe`, etc.), the `AsaSaveGame` class that serves as the main entry point for library consumers, and extension helpers on `GameObject`. Porcelain code queries game objects and their properties to produce strongly-typed domain objects — it should never read from the archive directly.

**Rule of thumb:** if it decodes bytes, it belongs in Plumbing. If it interprets game data and builds domain objects, it belongs in Porcelain.

---

## Version-Layered Parsing (Pre-Fallback Strategy)

Save files can come from different game versions. The project uses a clean delegation pattern — see [docs/VersioningStrategy.md](docs/VersioningStrategy.md) for full details.

### Pattern

Each reader implements a `Read()` method for the current format. At the top of that method, it should delegate immediately to an independent legacy reader if the save version is older rather than interleaving different versions' parsing needs in the same method:

```csharp
public static AsaPropertyHeader? Read(AsaArchive archive)
{
    if (archive.SaveVersion < 14)
        return ReadPre14(archive);

    // Current format implementation — read everything from scratch
    var name = archive.ReadFName();
    // ...
}

private static AsaPropertyHeader? ReadPre14(AsaArchive archive)
{
    // Completely independent legacy implementation
    var name = archive.ReadFName();
    // ...
}
```

### Rules

- **Delegate at the top.** The version check and delegation to the legacy reader must be the very first thing in `Read()`, before any bytes are read.
- **Legacy readers are fully independent.** A legacy reader such as `ReadPre14` must not call `Read()` or build on its output — it implements its version's format entirely from scratch.
- **No delta logic.** Do not implement a legacy reader as a patch or override of the current format. Each reader reads everything it needs directly from the archive.
- **One bounds check.** Individual readers should only throw exceptions if the data being can't be handled by any pre{Version} fallback. They shouldn't perform any "too new" checks on the version.  The outer "Reader" classes should perform any max version guards.
- **Name legacy readers by the version they predate.** For example, `ReadPre14` handles saves from before version 14, and `ReadPre11` handles saves from before version 11. This will prevent churn in method names every time a new version is added.

If `Read()` from v14 continues to work for v15, then none of the class's methods need to change names.  If v15 breaks reading for a class, rename the `Read()` method to `ReadPre15()` and implement a new, complete `Read()` method

---

## Adding New Code

### New property type (Plumbing)

1. Add a class under `src/Plumbing/Properties/`.
2. Implement a static `Read(AsaArchive archive)` method. If the property's binary format differs across save versions, apply the Pre-fallback pattern described above.
3. Integrate the new reader into the relevant dispatcher or parent reader so it is invoked when the corresponding property type name is encountered.

### New domain model (Porcelain)

1. Add a class under `src/Porcelain/`.
2. Populate it by reading properties from `GameObject` using the existing extension helpers. Do not access the archive directly from Porcelain code.
3. If it represents a top-level concept (e.g. `Creature`, `Player`), expose it as a collection on `AsaSaveGame`.

---

## Tests

**All new code must be accompanied by tests.** Use the **MSTest** framework and place tests under `tests/`. Any output files, dumps, or generated data produced during testing must go in `.work/`, which is gitignored — do not commit those files.

### Guidelines

- Cover the public surface of each reader or model, including both the happy path and relevant edge cases.
- For version-sensitive parsing, write at least one test per supported version boundary — for example, a test against a pre-14 save and a separate test against a v14 save for the same property type.
- For Porcelain types, verify that expected fields are correctly populated from a real or synthetic save fixture.
- Keep tests small and focused on a single behavior rather than writing large end-to-end integration tests.

---

## Code Style

Target **C# 10+** language features where they improve clarity. Prefer `var` for local variables when the assigned type is obvious from context. Use nullable reference types consistently — ensure `#nullable enable` is respected throughout the file. For diagnostic output, use `ILogger` with `LogDebug` or `LogTrace`; do not use `Console.WriteLine` in library code.
