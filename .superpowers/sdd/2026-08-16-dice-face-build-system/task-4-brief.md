# Task 4 Brief: Projectile Runtime Stats

## Context

This task adds runtime-configurable projectile stats so future dice-face entries can configure each shot. It must not integrate the revolver or UI yet.

## Global Constraints

- 不覆盖用户在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer。
- 不擅自修改 `DiceRevolverGun` 的射速、换弹速度、子弹速度等调参字段。
- 不运行会重建场景和 prefab 的 `TopDownPrototypeSceneBuilder.BuildPrototypeScene`，除非用户明确批准。
- UI、构筑数据、左轮发射、玩家 3C 保持低耦合，通过明确接口和事件上下文通信。
- 新增系统必须通过 Unity batchmode 编译；能自动化测试的核心逻辑用 EditMode tests 覆盖。

## Files

- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- Create: `Assets/Tests/EditMode/ProjectileStatsTests.cs`

## Interfaces

Produce:

- `Projectile.Configure(ProjectileRuntimeStats stats)`
- `ProjectileRuntimeStats` serializable struct or class
- `Projectile.ProjectileType`
- `Projectile.ProjectileTag`
- `Projectile.Damage`
- `Projectile.EnemyPierceCount`

## Tests

Create `ProjectileStatsTests`:

```csharp
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileStatsTests
    {
        [Test]
        public void ConfigureAppliesRuntimeStats()
        {
            GameObject owner = new GameObject("Projectile");
            Projectile projectile = owner.AddComponent<Projectile>();

            projectile.Configure(new ProjectileRuntimeStats("Piercing", "PlayerBullet", 7f, 12f, 24f, 2));

            Assert.That(projectile.ProjectileType, Is.EqualTo("Piercing"));
            Assert.That(projectile.ProjectileTag, Is.EqualTo("PlayerBullet"));
            Assert.That(projectile.Damage, Is.EqualTo(7f));
            Assert.That(projectile.EnemyPierceCount, Is.EqualTo(2));

            Object.DestroyImmediate(owner);
        }
    }
}
```

## Implementation

Add an immutable `ProjectileRuntimeStats` type in the `DiceRevolver.Prototype` namespace.

Constructor signature:

```csharp
public ProjectileRuntimeStats(
    string projectileType,
    string projectileTag,
    float damage,
    float flightDistance,
    float flightSpeed,
    int enemyPierceCount)
```

Fields/properties:

- `ProjectileType`
- `ProjectileTag`
- `Damage`
- `FlightDistance`
- `FlightSpeed`
- `EnemyPierceCount`

Clamp `FlightDistance` and `FlightSpeed` to positive values, and clamp `EnemyPierceCount` to `>= 0`.

In `Projectile`, keep the existing serialized `speed` and `lifetime` fields. Add runtime fields initialized from those defaults:

- projectile type defaults to `"Default"`
- projectile tag defaults to `"Default"`
- damage defaults to `1f`
- flight speed defaults to serialized `speed`
- enemy pierce count defaults to `0`
- runtime lifetime defaults to serialized `lifetime`

`Configure(ProjectileRuntimeStats stats)` should apply the runtime fields and set runtime lifetime to `stats.FlightDistance / stats.FlightSpeed`.

`Launch` and `OnEnable` should use the runtime lifetime, not always the serialized `lifetime`.

Do not implement pierce collision logic in this task; only store the value.

## Verification

Run EditMode tests using the established temporary-copy/no-quit pattern. Do not run the scene builder.

## Report

Write a report to `.superpowers/sdd/2026-08-16-dice-face-build-system/task-4-report.md` with:

- Status
- Files changed
- Verification command and result
- Concerns

