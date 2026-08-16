# Task 1 Brief: EditMode Test Harness

## Context

This task starts the automated test foundation for the dice-face build system. It must only add EditMode test files.

## Global Constraints

- 不覆盖用户在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer。
- 不擅自修改 `DiceRevolverGun` 的射速、换弹速度、子弹速度等调参字段。
- 不运行会重建场景和 prefab 的 `TopDownPrototypeSceneBuilder.BuildPrototypeScene`，除非用户明确批准。
- UI、构筑数据、左轮发射、玩家 3C 保持低耦合，通过明确接口和事件上下文通信。
- 新增系统必须通过 Unity batchmode 编译；能自动化测试的核心逻辑用 EditMode tests 覆盖。

## Files

- Create: `Assets/Tests/EditMode/DiceRevolver.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/DiceChamberTests.cs`

## Requirements

Create an EditMode test assembly and one smoke test for existing `DiceChamber` behavior.

`Assets/Tests/EditMode/DiceRevolver.EditMode.asmdef` should contain:

```json
{
  "name": "DiceRevolver.EditMode.Tests",
  "rootNamespace": "DiceRevolver.Tests",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Assets/Tests/EditMode/DiceChamberTests.cs` should contain a test equivalent to:

```csharp
using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class DiceChamberTests
    {
        [Test]
        public void ResetRestoresAllSixFaces()
        {
            DiceChamber chamber = new DiceChamber(6);

            chamber.TryDrawFace(out _);
            chamber.Reset();

            Assert.That(chamber.RemainingCount, Is.EqualTo(6));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, chamber.RemainingFaces);
        }
    }
}
```

## Verification

Run Unity EditMode tests if possible:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity Projects\DiceRevolver" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\Logs\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\Logs\editmode-tests.log"
```

If the main project is locked by an open Unity editor, use a temporary copy and report that route. Do not run `TopDownPrototypeSceneBuilder.BuildPrototypeScene`.

## Report

Write a report to `.superpowers/sdd/2026-08-16-dice-face-build-system/task-1-report.md` with:

- Status: DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT, or BLOCKED
- Files changed
- Verification command and result
- Concerns, if any

