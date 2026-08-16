# Task 5 Review Package: Bullet Event Effects

## Scope

Task 5 adds the event-effect base API and the first three prototype bullet event effects. It must not integrate the revolver or UI yet, and must not modify scene/prefab values or user-tuned `DiceRevolverGun` inspector fields.

## Changed Files

- `Assets/Scripts/Prototype/BulletEventEffect.cs`
- `Assets/Scripts/Prototype/BulletEventContext.cs`
- `Assets/Scripts/Prototype/BulletEventContext.cs.meta`
- `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`
- `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs.meta`
- `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs`
- `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs.meta`
- `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs`
- `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs.meta`
- `Assets/Tests/EditMode/BulletEventEffectTests.cs`
- `Assets/Tests/EditMode/BulletEventEffectTests.cs.meta`
- `.superpowers/sdd/2026-08-16-dice-face-build-system/task-5-report.md`

## Implementation Notes

- `BulletEventEffect` now exposes `Trigger(BulletEventContext context)`.
- `BulletEventContext` is a readonly struct containing optional gun, chamber, shot, hit collider, hit position, recursion flag, and an optional additional-shot callback.
- `ExtraShotOnFireEffect` asks the context to request exactly one additional shot when allowed. Actual spawning is deferred to Task 6.
- `ExplosionOnHitEffect` exposes an explosion projectile prefab port, instantiates it at hit position when present, and warns/skips when missing.
- `ForceFaceFourOnFireEndEffect` skips if face 4 is still in the chamber, otherwise refills face 4 and forces the next draw to return 4.

## Verification

Unity EditMode tests were run in a temporary project copy:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -projectPath "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask5Tests" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask5Tests\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask5Tests\editmode-tests.log"
```

Result: `Passed`, total `15`, passed `15`, failed `0`.

## Review Focus

- Does the event API stay low-coupled enough for later gun/UI integration?
- Does `ForceFaceFourOnFireEndEffect` exactly match the spec?
- Does `ExtraShotOnFireEffect` prevent recursion through the context flag?
- Does `ExplosionOnHitEffect` expose a projectile prefab port and safely skip missing prefab?
- Did this task avoid scene/prefab/sorting/player transform changes?
