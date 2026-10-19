# Properties

There are 19 predefined property types used in the save files.

## Simple Properties

| Type                 | .NET Type | Size    | Signed | Name Table ID             | Range/Description           |
| -------------------- | --------- | ------- | ------ | ------------------------- | --------------------------- |
| **`Int8Property`**   | SByte     | 1 byte  | true   | F2 75 A8 38 / 950564338   | -128 to 127                 |
| **`ByteProperty`**   | Byte      | 1 bytes | false  | B2 B4 4F 96 / -1773161294 | 0 to 255                    |
| **`Int16Property`**  | Int16     | 2 bytes | true   | B5 75 0B A0 / -1609861707 | -32,768 to 32,767           |
| **`UInt16Property`** | UInt16    | 2 bytes | false  | 5D 60 EB 5A / 1525375069  | 0 to 65,535                 |
| **`IntProperty`**    | Int32     | 4 bytes | true   | 71 B2 FA F3 / -201674127  | -2.1B to 2.1B               |
| **`UInt32Property`** | UInt32    | 4 bytes | false  | 5B 9E 09 B5 / -1257660837 | 0 to 4.3B                   |
| **`Int64Property`**  | Int64     | 8 bytes | true   | 9C 90 73 48 / 1215533212  | -9.2 * 10^18 to 9.2 * 10^18 |
| **`UInt64Property`** | UInt64    | 8 bytes | false  | 74 85 93 B2 / -1298954892 | 0 to 1.8 × 10^19            |
| **`FloatProperty`**  | Single    | 4 bytes | true   | 3A 23 FD 69 / 1778197306  | 32-bit floating point       |
| **`DoubleProperty`** | Double    | 8 bytes | true   | 0D D9 8C 90 / -1869817587 | 64-bit floating point       |

Simple properties store single primitive values in ARK save files. These are the most common property types and have straightforward binary formats without nested structures or complex data.

## Complex Properties

| Type                     | .NET Type | Name Table ID             | Description                                                                                     |
| ------------------------ | --------- | ------------------------- | ----------------------------------------------------------------------------------------------- |
| **`BoolProperty`**       | Bool      | AA 26 82 4D / 1300375210  | Value is stored in the property's Flags field                                                   |
| **`ByteProperty<T>`**    | String    | B2 B4 4F 96 / -1773161294 | An enum stored using a Name pointer to its string form.                                         |
| **`StrProperty`**        | String    | 80 FF 9F A8 / -1465909376 | A length field followed by a nul-terminated string stored as either 1 or 2 bytes per character. |
| **`NameProperty`**       | AsaName   | C1 22 B7 8F / -1883823423 | The typical int32 pair used for NameTable lookups                                               |
| **`ArrayProperty<T>`**   | T[]       | 6B C5 FD 58 / 1493026155  | The type T is specified in the property's generic type field.                                   |
| **`MapProperty`**        |           | 65 15 1E 9B / -1692527259 |                                                                                                 |
| **`ObjectProperty`**     |           | 3B DA 82 0C / 209902139   |                                                                                                 |
| **`SetProperty<T>`**     | T[]       | B5 DF F7 4A / 1257758645  | Like an array, but with an addition `Skip` int32 in its data                                    |
| **`SoftObjectProperty`** |           | 10 21 84 ED / -310107888  |                                                                                                 |
| **`StructProperty<T>`**  | T         | 70 C6 A4 FA / -89864592   | T may be a known type like Vector or Color, or it could be a self-defining complex type         |

Complex properties require conditional processing or have multiple or a variable number of 

### World object references

An `ObjectProperty` in a world save contains a two-byte discriminator followed by either a 16-byte object GUID (`00 00`) or an 8-byte name-table `FName` path (`00 01`). The reader also accepts the `01 00` path marker for compatibility.

The v16 Lost Colony fixture has two dropped cryopods whose `NetDroppedMeshOverride` values use `00 01` followed by the mesh path `/Game/Extinction/WeaponDinoCryopod/SM_DinoCryopod.SM_DinoCryopod`. Each payload is 10 bytes, not an 18-byte GUID reference.

Standalone profile/tribe and cryopod archives have separate object-reference layouts and do not use this world-save discriminator.

## Binary Structure

### Standard Format

```
                                           [Property Header]
70 5D A9 F4 00 00 00 00                    [Name] Property Name: NumNPCSpawned(0)
71 B2 FA F3 00 00 00 00                    [Name] Property Type: IntProperty(0)
00 00 00 00                                [Int32] SubType count: 0
04 00 00 00                                [Int32] Data size: 4
00                                         [Byte] Flags: 0
15 00 00 00                                [Int32] Value: 21
```

## Field Descriptions

### Property Name
Identifies the property using a lookup into the name table.
- **Type**: AsaName (8 bytes)
- **Format**: Name ID (Int32) + Instance (Int32)
- **Examples**: "NumNPCSpawned", "TribeID", "CharacterLevel", "PlayerName"

### Generic Property Type

#### Type name
The type of the property, resolved from the name table.
- **Type**: AsaName (8 bytes)
- **Format**: Type ID (Int32) + Instance (Int32)
- **Values**: "`IntProperty`, `FloatProperty`, `StrProperty`, etc.

### SubType Count
Number of generic type parameters. Always 0 for simple properties.
- **Type**: Int32
- **Value**: 0 (simple properties are not generic types)

If subtype count > 0, the generic sub-types will be read sequentially using the same generic property type schema.  This allows property to form a generic type with multiple type properties, like `Array<Struct<SomeStructType>>` or `MapProperty<Name, Float>`

### Data Size
Size of the property value in bytes.
- **Type**: Int32
- **Values**:
  - `BoolProperty`: 0 (value stored in flags)
  - `Int8Property`: 1
  - `Int16Property`: 2
  - `IntProperty`, UInt32Property, FloatProperty: 4
  - `Int64Property`, UInt64Property, DoubleProperty: 8
  - `StrProperty`: variable (length + string bytes)

### Flags
Control flags for the property.
- **Type**: Byte
- **bit 0 (0x01)**: If set, a position field follows
- **Common values**: 
  - `0x00` = No position field
  - `0x01` or `0x09` = Position field present

**Note**: `BoolProperty` doesn't use flags and stores its value in the Flags field instead; the Flags field will be `0` for false and non-zero for true.

### Position (Optional)
Array index for indexed properties. Only present if flags & 0x01.
- **Type**: Int32
- **Usage**: Rare for simple properties, used for property arrays

### Value
The actual property value.
- **Type**: Varies by property type (see table above)
- **Endianness**: Little-endian
- **Format**: Binary representation of the value
  
**Note**: `BoolProperty` doesn't have a Value field

## Every type I've encountered in save files:
```
BoolProperty
Int8Property
ByteProperty
Int16Property
UInt16Property
IntProperty
UInt32Property
Int64Property
UInt64Property
FloatProperty
DoubleProperty

NameProperty
ObjectProperty
SoftObjectProperty
StrProperty

ByteProperty[EPrimalEquipmentType[/Script/Engine]]
ByteProperty[EBabyCuddleType[/Script/ShooterGame]]
ByteProperty[ESeedCropPhase[/Script/ShooterGame]]

MapProperty[IntProperty, DoubleProperty]
MapProperty[NameProperty, StructProperty[PrimalWirelessReferences[/Script/ShooterGame]]]

StructProperty[LinearColor[/Script/CoreUObject]]
StructProperty[Quat[/Script/CoreUObject]]
StructProperty[Rotator[/Script/CoreUObject]]
StructProperty[Vector[/Script/CoreUObject]]
StructProperty[UniqueNetIdRepl[/Script/Engine]]
StructProperty[ItemNetID[/Script/ShooterGame]]
StructProperty[SkinNetMessageParams[/Script/ShooterGame]]
StructProperty[ARKDinoData[/Script/ShooterGame]]

ArrayProperty[DoubleProperty]
ArrayProperty[IntProperty]
ArrayProperty[NameProperty]
ArrayProperty[ObjectProperty]
ArrayProperty[StructProperty[CustomItemData[/Script/ShooterGame]]]
ArrayProperty[StructProperty[DinoAncestorsEntry[/Script/ShooterGame]]]
ArrayProperty[StructProperty[HibernationCountInfo[/Script/ShooterGame]]]
ArrayProperty[StructProperty[ItemCraftQueueEntry[/Script/ShooterGame]]]
ArrayProperty[StructProperty[LinearColor[/Script/CoreUObject]]]
ArrayProperty[StructProperty[PaintingKeyValue[/Script/ShooterGame]]]
ArrayProperty[StructProperty[PlayerDeathReason[/Script/ShooterGame]]]
ArrayProperty[StructProperty[PrimalCharacterStatusValueModifier[/Script/ShooterGame]]]
ArrayProperty[StructProperty[PrimalSaddleStructure[/Script/ShooterGame]]]
ArrayProperty[StructProperty[ServerCustomFolder[/Script/ShooterGame]]]

ArrayProperty[StructProperty[Gacha_ResourceStruct[/Game/Extinction/Dinos/Gacha/Gacha_ResourceStruct], 688a73e0-3316-ad3c-ca82-2ea9258c16cc]]
ArrayProperty[StructProperty[GeneTraitStruct[/Game/Packs/Steampunk/Weapons/GeneScanner/Gameplay/GeneTraits/GeneTraitStruct], c645c0c9-4a55-03da-1c38-13a3cb71e2ab]]
ArrayProperty[StructProperty[Struct_PatrolGroupSavedData[/Game/LostColony/CoreBlueprints/Patrols/Structs/Struct_PatrolGroupSavedData], b87007fd-454a-d37a-8643-a49d275a3b14]]


```

## Related Code

- [AsaPropertyRegistry.cs](d:\repos\AsaSavegameToolkit\src\Serialization\Properties\AsaPropertyRegistry.cs) - Property parsing logic
- [AsaProperty.cs](d:\repos\AsaSavegameToolkit\src\Serialization\Properties\AsaProperty.cs) - Property wrapper class
- [GameObject.md](../RecordTypes/GameObject.md) - Example usage in game objects
