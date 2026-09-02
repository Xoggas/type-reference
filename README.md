# Type Reference

A small, dependency-free Unity package that lets you serialize a reference to a `System.Type`
and pick it from a searchable dropdown in the Inspector, optionally constrained to types that
inherit a base class or implement one or more interfaces.

Written from scratch against modern Unity APIs (`UnityEditor.IMGUI.Controls.AdvancedDropdown`),
with no dependency on `com.solidalloy.util` / `com.solidalloy.unity-dropdown`. It is a drop-in
replacement for SolidAlloy's `TypeReferences` package: same namespace (`TypeReferences`), same
`TypeReference` field type, same `[Inherits]` attribute, same serialized field names, so it can
read data that was already saved by the old package.

## Usage

```csharp
using TypeReferences;
using UnityEngine;

public class EnemyMetadata : ScriptableObject
{
    [SerializeField, Inherits(typeof(IEnemyModel))]
    private TypeReference modelType;
}
```

Reading the value at runtime:

```csharp
Type type = modelType; // implicit conversion to System.Type
var instance = (IEnemyModel)Activator.CreateInstance(type);
```

## Attributes

`[Inherits(typeof(BaseType), params Type[] additionalBaseTypes)]`
- `IncludeBaseType` (bool, default `false`) — include the base type itself in the dropdown.
- `AllowAbstract` (bool, default `false`) — allow abstract classes/interfaces to be selected.

`[TypeOptions]` (base attribute, `InheritsAttribute` derives from it)
- `ShowNoneElement` (bool, default `true`) — show a `None` entry to clear the reference.
- `IncludeTypes` / `ExcludeTypes` (`Type[]`) — manually add/remove candidate types.
- `ShortName` (bool, default `false`) — display the short type name instead of the full name.
- `SerializableOnly` (bool, default `false`) — only show types Unity can serialize.
- `AllowInternal` (bool, default `false`) — include non-public types.
- `DropdownHeight` (int) — override the dropdown's height (clamped to 100–600 px).

## How type references survive renames

When a type is picked from the dropdown, the package also stores the GUID of the `.cs` file
that declares it (the same way Unity does for `MonoScript` references). If the type is later
renamed, the reference resolves the new name via that GUID the next time it's read in the Editor.
