# Task 4 Review Package: Projectile Runtime Stats

## Scope

Task 4 adds runtime-configurable projectile stats so later dice-face entries can configure each shot. It must not integrate the revolver, UI, scene, or prefab data yet.

## Changed Files

- `Assets/Scripts/Prototype/ProjectileRuntimeStats.cs`
- `Assets/Scripts/Prototype/ProjectileRuntimeStats.cs.meta`
- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- `Assets/Tests/EditMode/ProjectileStatsTests.cs`
- `Assets/Tests/EditMode/ProjectileStatsTests.cs.meta`
- `.superpowers/sdd/2026-08-16-dice-face-build-system/task-4-report.md`

## Implementation Notes

- `ProjectileRuntimeStats` is an immutable serializable value type with type, tag, damage, distance, speed, and pierce count.
- Distance and speed clamp to positive values; pierce count clamps to `>= 0`.
- `Projectile.Configure(ProjectileRuntimeStats stats)` applies runtime fields and computes runtime lifetime from distance / speed.
- `Projectile` keeps existing serialized `speed` and `lifetime` defaults and uses runtime values for movement/despawn.
- `DiceRevolverShotContext` keeps the old constructor and adds an overload carrying a `DiceFaceEntry` reference for later systems.

## Verification

Unity EditMode tests were run in a temporary project copy:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -projectPath "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests\editmode-tests.log"
```

Result: `Passed`, total `11`, passed `11`, failed `0`.

## Review Focus

- Does `Projectile.Configure` satisfy the task brief without changing serialized tuning values?
- Does runtime lifetime correctly come from `FlightDistance / FlightSpeed`?
- Does `ProjectileRuntimeStats` clamp invalid values safely?
- Does the `DiceRevolverShotContext` overload preserve existing call sites?
- Did this task avoid scene/prefab/sorting/player transform changes?
