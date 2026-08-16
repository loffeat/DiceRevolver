# Task 3 Review Package: Dice Face Data and Loadout

## Scope

Task 3 adds the first data layer for the dice-face build system:

- `DiceFaceEntry` stores bullet stats, projectile override, extension ports, and three bullet-event effect lists.
- `DiceFaceLibrary` stores all available dice-face entries.
- `BulletEventLibrary` stores all available bullet event effects.
- `DiceFaceLoadout` stores the six equipped faces on the player and broadcasts entry changes.
- `DiceFaceLoadoutTests` covers valid equip, invalid face guards, and change events.

The implementation must not touch scene/prefab values, sorting layers, `DiceRevolverGun` inspector values, or aim rig transforms.

## Verification

Unity EditMode tests were run in a temporary project copy:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -projectPath "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask3Tests" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask3Tests\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\CodexUnityTempDiceFaceTask3Tests\editmode-tests.log"
```

Result: `Passed`, total `7`, passed `7`, failed `0`.

## Changed Files

- `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- `Assets/Scripts/Prototype/BulletEventEffect.cs`
- `Assets/Scripts/Prototype/DiceFaceLibrary.cs`
- `Assets/Scripts/Prototype/BulletEventLibrary.cs`
- `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`
- `.superpowers/sdd/2026-08-16-dice-face-build-system/task-3-report.md`

## Review Focus

- Does the API satisfy the Chinese spec for dice-face entries and the two SO libraries?
- Is it low-coupled enough for later `DiceRevolverGun`, UI, and bullet event integration?
- Are there null/serialization/API risks in returning serialized arrays as `IReadOnlyList<T>`?
- Is declaring `BulletEventEffect` in `BulletEventLibrary.cs` acceptable now, or should it be split before Task 5?
- Are tests sufficient for Task 3, given later tasks will cover runtime stat application and UI?

## Review Round 1 Fixes

- Data asset collection getters now return empty collections instead of null.
- `DiceFaceLoadout` repairs malformed serialized slot array length before valid slot access.
- `BulletEventEffect` was split into `BulletEventEffect.cs`.
- Unity EditMode verification after fixes: `Passed`, total `9`, passed `9`, failed `0`.
