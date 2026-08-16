# Task 2 Brief: Chamber Forced Draw API

## Context

This task extends the existing `DiceChamber` random pool so event effects can refill a face and force the next draw. It must keep `TryDrawFace` as the only method that removes a face from the pool.

## Global Constraints

- 不覆盖用户在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer。
- 不擅自修改 `DiceRevolverGun` 的射速、换弹速度、子弹速度等调参字段。
- 不运行会重建场景和 prefab 的 `TopDownPrototypeSceneBuilder.BuildPrototypeScene`，除非用户明确批准。
- UI、构筑数据、左轮发射、玩家 3C 保持低耦合，通过明确接口和事件上下文通信。
- 新增系统必须通过 Unity batchmode 编译；能自动化测试的核心逻辑用 EditMode tests 覆盖。

## Files

- Modify: `Assets/Scripts/Prototype/DiceChamber.cs`
- Modify: `Assets/Tests/EditMode/DiceChamberTests.cs`

## Interfaces

Produce:

- `bool ContainsFace(int face)`
- `bool TryRefillFace(int face)`
- `bool TryForceNextFace(int face)`
- `bool TryDrawFace(out int face)` respects forced face first

## Tests

Add these tests to `DiceChamberTests`:

```csharp
[Test]
public void TryRefillFaceAddsMissingFaceOnce()
{
    DiceChamber chamber = new DiceChamber(6);

    while (chamber.ContainsFace(4))
    {
        chamber.TryDrawFace(out _);
    }

    Assert.That(chamber.TryRefillFace(4), Is.True);
    Assert.That(chamber.TryRefillFace(4), Is.False);
    Assert.That(chamber.ContainsFace(4), Is.True);
}

[Test]
public void TryForceNextFaceMakesNextDrawReturnThatFace()
{
    DiceChamber chamber = new DiceChamber(6);

    Assert.That(chamber.TryForceNextFace(4), Is.True);

    chamber.TryDrawFace(out int face);

    Assert.That(face, Is.EqualTo(4));
    Assert.That(chamber.ContainsFace(4), Is.False);
}

[Test]
public void TryForceNextFaceFailsWhenFaceIsNotInPool()
{
    DiceChamber chamber = new DiceChamber(1);
    chamber.TryDrawFace(out _);

    Assert.That(chamber.TryForceNextFace(1), Is.False);
}
```

## Implementation

Add:

```csharp
private int? forcedNextFace;

public bool ContainsFace(int face)
{
    return remainingFaces.Contains(face);
}

public bool TryRefillFace(int face)
{
    if (face < 1 || face > faceCount || remainingFaces.Contains(face))
    {
        return false;
    }

    remainingFaces.Add(face);
    remainingFaces.Sort();
    return true;
}

public bool TryForceNextFace(int face)
{
    if (!remainingFaces.Contains(face))
    {
        return false;
    }

    forcedNextFace = face;
    return true;
}
```

Update `Reset()` to clear `forcedNextFace`.

Update `TryDrawFace()` so if `forcedNextFace` is set and still present, it removes and returns that face before random draw.

If `forcedNextFace` is set but no longer present, clear it and fall back to normal random draw.

## Verification

Run EditMode tests if possible. If Unity cannot run against the active project, use a temporary copy. Do not run the scene builder.

## Report

Write a report to `.superpowers/sdd/2026-08-16-dice-face-build-system/task-2-report.md` with:

- Status
- Files changed
- Verification command and result
- Concerns

