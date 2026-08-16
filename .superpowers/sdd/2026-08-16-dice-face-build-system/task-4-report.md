# Task 4 Report: Projectile Runtime Stats

## Status

Implemented and verified.

## Files changed

- `Assets/Scripts/Prototype/ProjectileRuntimeStats.cs`
- `Assets/Scripts/Prototype/ProjectileRuntimeStats.cs.meta`
- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- `Assets/Tests/EditMode/ProjectileStatsTests.cs`
- `Assets/Tests/EditMode/ProjectileStatsTests.cs.meta`
- `.superpowers/sdd/2026-08-16-dice-face-build-system/task-4-report.md`

## Verification command and result

Ran against a temporary project copy, without `-quit`:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -projectPath "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask4Tests\editmode-tests.log"
```

Result: `Passed`, total `11`, passed `11`, failed `0`. The scene builder was not run.

## Concerns

- Pierce collision behavior is intentionally not implemented in this task; `EnemyPierceCount` is stored for later event/combat integration.
