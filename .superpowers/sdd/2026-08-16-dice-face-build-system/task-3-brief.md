# Task 3 Brief: Dice Face Data and Loadout

## Context

This task adds ScriptableObject data types for dice-face entries and libraries, plus a runtime loadout component for six equip slots. It must not integrate with the gun or UI yet.

## Global Constraints

- 不覆盖用户在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer。
- 不擅自修改 `DiceRevolverGun` 的射速、换弹速度、子弹速度等调参字段。
- 不运行会重建场景和 prefab 的 `TopDownPrototypeSceneBuilder.BuildPrototypeScene`，除非用户明确批准。
- UI、构筑数据、左轮发射、玩家 3C 保持低耦合，通过明确接口和事件上下文通信。
- 新增系统必须通过 Unity batchmode 编译；能自动化测试的核心逻辑用 EditMode tests 覆盖。

## Files

- Create: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceLibrary.cs`
- Create: `Assets/Scripts/Prototype/BulletEventLibrary.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Create: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`

## Interfaces

Produce:

- `DiceFaceEntry` ScriptableObject with public read-only properties for serialized fields
- `DiceFaceLibrary.Entries`
- `BulletEventLibrary.Effects`
- `DiceFaceLoadout.Equip(int face, DiceFaceEntry entry)`
- `DiceFaceLoadout.GetEntry(int face)`
- `DiceFaceLoadout.EntryChanged`

## Tests

Create `DiceFaceLoadoutTests`:

```csharp
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceFaceLoadoutTests
    {
        [Test]
        public void EquipStoresEntryForFace()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();

            loadout.Equip(3, entry);

            Assert.That(loadout.GetEntry(3), Is.SameAs(entry));

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void EquipIgnoresFacesOutsideOneToSix()
        {
            GameObject owner = new GameObject("LoadoutOwner");
            DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();

            loadout.Equip(0, entry);
            loadout.Equip(7, entry);

            Assert.That(loadout.GetEntry(0), Is.Null);
            Assert.That(loadout.GetEntry(7), Is.Null);

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(entry);
        }
    }
}
```

## Implementation

`DiceFaceEntry` must be:

```csharp
[CreateAssetMenu(menuName = "Dice Revolver/Dice Face Entry")]
public sealed class DiceFaceEntry : ScriptableObject
```

Serialized fields:

- `string displayName = "New Dice Face"`
- `string description`
- `Color displayColor = Color.white`
- `Projectile projectilePrefabOverride`
- `string projectileType = "Default"`
- `string projectileTag = "Default"`
- `float damage = 1f`
- `float flightDistance = 18f`
- `float flightSpeed = 18f`
- `int enemyPierceCount`
- `DiceFaceExtensionPort[] extensionPorts`
- `BulletEventEffect[] onFireEffects`
- `BulletEventEffect[] onHitEffects`
- `BulletEventEffect[] onFireEndEffects`

Add read-only public properties for all fields.

Add:

```csharp
[Serializable]
public struct DiceFaceExtensionPort
{
    public string Name;
    public float Value;
}
```

`DiceFaceLibrary` must be:

```csharp
[CreateAssetMenu(menuName = "Dice Revolver/Dice Face Library")]
public sealed class DiceFaceLibrary : ScriptableObject
```

with serialized `DiceFaceEntry[] entries` and public `IReadOnlyList<DiceFaceEntry> Entries`.

`BulletEventLibrary` must be:

```csharp
[CreateAssetMenu(menuName = "Dice Revolver/Bullet Event Library")]
public sealed class BulletEventLibrary : ScriptableObject
```

with serialized `BulletEventEffect[] effects` and public `IReadOnlyList<BulletEventEffect> Effects`.

If `BulletEventEffect` does not exist yet, create a minimal abstract base:

```csharp
public abstract class BulletEventEffect : ScriptableObject
{
}
```

Do not implement concrete effects in this task.

`DiceFaceLoadout` should use six serialized slots and clamp face lookup to `1..6`. `Equip` should ignore invalid faces, store valid entries, and raise:

```csharp
public event Action<int, DiceFaceEntry> EntryChanged;
```

after valid equip.

## Verification

Run EditMode tests using the Task 1 working pattern. Do not run the scene builder.

## Report

Write a report to `.superpowers/sdd/2026-08-16-dice-face-build-system/task-3-report.md` with:

- Status
- Files changed
- Verification command and result
- Concerns

