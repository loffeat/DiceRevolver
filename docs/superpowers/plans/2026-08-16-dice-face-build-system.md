# Dice Face Build System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现一个可按 `E` 打开/关闭、可点击装备骰面词条、并能驱动骰子左轮子弹属性与事件效果的原型构筑系统。

**Architecture:** 以 ScriptableObject 作为词条数据源，以 `DiceFaceLoadout` 作为运行时装配模型，以 `DiceBuildPageUI` 作为纯 UI 编辑层。`DiceRevolverGun` 只接入 loadout 与 effect runner，不反向依赖 UI，不修改玩家 3C、相机、手臂 rig 或用户已调好的枪械参数。

**Tech Stack:** Unity 6000.3.10f1, C#, UGUI, Unity Input System, Unity Test Framework EditMode tests.

## Global Constraints

- 不覆盖用户在 Inspector 中调好的 `Player.prefab`、`AimRoot`、`ArmVisual`、`GunBody`、`Muzzle`、sorting layer。
- 不擅自修改 `DiceRevolverGun` 的射速、换弹速度、子弹速度等调参字段。
- 不运行会重建场景和 prefab 的 `TopDownPrototypeSceneBuilder.BuildPrototypeScene`，除非用户明确批准。
- UI、构筑数据、左轮发射、玩家 3C 保持低耦合，通过明确接口和事件上下文通信。
- 新增系统必须通过 Unity batchmode 编译；能自动化测试的核心逻辑用 EditMode tests 覆盖。

---

## File Structure

- Create: `Assets/Scripts/Prototype/DiceFaceEntry.cs`  
  定义骰面词条 SO、子弹属性、扩展端口。

- Create: `Assets/Scripts/Prototype/DiceFaceLibrary.cs`  
  定义骰面词条库 SO。

- Create: `Assets/Scripts/Prototype/BulletEventLibrary.cs`  
  定义子弹事件词条库 SO。

- Create: `Assets/Scripts/Prototype/BulletEventEffect.cs`  
  定义事件效果基类与执行上下文。

- Create: `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`  
  开火时额外发射一次当前骰面。

- Create: `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs`  
  击中时生成配置的爆炸 projectile prefab。

- Create: `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs`  
  结束开火时补回并强制下一次抽取骰面 4。

- Create: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`  
  保存六个骰面的运行时装备状态。

- Create: `Assets/Scripts/Prototype/DiceBuildPageUI.cs`  
  控制 `E` 开关构筑页面、生成左右两侧 UI、处理点击装备。

- Create: `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`  
  左侧单个骰面格子的按钮与显示逻辑。

- Create: `Assets/Scripts/Prototype/DiceBuildEntryButtonUI.cs`  
  右侧单个词条按钮与显示逻辑。

- Modify: `Assets/Scripts/Prototype/DiceChamber.cs`  
  增加查询、补回、强制下一次抽取指定骰面的 API。

- Modify: `Assets/Scripts/Prototype/Projectile.cs`  
  增加运行时子弹属性配置。

- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`  
  增加骰面词条、子弹属性、是否允许递归触发额外发射等上下文。

- Modify: `Assets/Scripts/Prototype/DiceRevolverHitContext.cs`  
  增加命中位置读取所需上下文。

- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`  
  接入 `DiceFaceLoadout`、应用子弹属性、执行三类事件效果。

- Create: `Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs`  
  用菜单项创建/补齐 SO 资源和 UI 对象，只添加缺失引用，不重建玩家 prefab。

- Create: `Assets/Tests/EditMode/DiceRevolver.EditMode.asmdef`  
  EditMode 测试程序集。

- Create: `Assets/Tests/EditMode/DiceChamberTests.cs`
- Create: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`
- Create: `Assets/Tests/EditMode/BulletEventEffectTests.cs`

---

### Task 1: EditMode Test Harness

**Files:**
- Create: `Assets/Tests/EditMode/DiceRevolver.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/DiceChamberTests.cs`

**Interfaces:**
- Consumes: existing `DiceChamber`
- Produces: working EditMode test assembly for later tasks

- [ ] **Step 1: Create the EditMode asmdef**

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

- [ ] **Step 2: Write a smoke test that uses existing chamber behavior**

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

- [ ] **Step 3: Run the smoke test**

Run:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity Projects\DiceRevolver" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\Logs\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\Logs\editmode-tests.log"
```

Expected: test run starts and `ResetRestoresAllSixFaces` passes. If the main project is open and Unity refuses project access, run the same command on a temporary project copy and report that verification path.

---

### Task 2: Chamber Forced Draw API

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceChamber.cs`
- Modify: `Assets/Tests/EditMode/DiceChamberTests.cs`

**Interfaces:**
- Produces:
  - `bool ContainsFace(int face)`
  - `bool TryRefillFace(int face)`
  - `bool TryForceNextFace(int face)`
  - `bool TryDrawFace(out int face)` respects forced face first

- [ ] **Step 1: Write failing tests**

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

- [ ] **Step 2: Verify tests fail**

Run the EditMode test command from Task 1. Expected: compile fails because new methods do not exist.

- [ ] **Step 3: Implement minimal chamber API**

Add fields and methods:

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

Update `Reset()` to clear `forcedNextFace`. Update `TryDrawFace()` to remove and return `forcedNextFace` before random draw when it is set and still present.

- [ ] **Step 4: Verify tests pass**

Run the EditMode test command. Expected: all `DiceChamberTests` pass.

---

### Task 3: Dice Face Data and Loadout

**Files:**
- Create: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceLibrary.cs`
- Create: `Assets/Scripts/Prototype/BulletEventLibrary.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Create: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`

**Interfaces:**
- Produces:
  - `DiceFaceEntry` SO with public read-only properties for serialized fields
  - `DiceFaceLibrary.Entries`
  - `BulletEventLibrary.Effects`
  - `DiceFaceLoadout.Equip(int face, DiceFaceEntry entry)`
  - `DiceFaceLoadout.GetEntry(int face)`
  - `DiceFaceLoadout.EntryChanged`

- [ ] **Step 1: Write failing loadout tests**

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

- [ ] **Step 2: Verify tests fail**

Run EditMode tests. Expected: compile fails because new types do not exist.

- [ ] **Step 3: Implement data SOs**

`DiceFaceEntry`:

```csharp
[CreateAssetMenu(menuName = "Dice Revolver/Dice Face Entry")]
public sealed class DiceFaceEntry : ScriptableObject
{
    [SerializeField] private string displayName = "New Dice Face";
    [SerializeField] private string description;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private Projectile projectilePrefabOverride;
    [SerializeField] private string projectileType = "Default";
    [SerializeField] private string projectileTag = "Default";
    [SerializeField] private float damage = 1f;
    [SerializeField] private float flightDistance = 18f;
    [SerializeField] private float flightSpeed = 18f;
    [SerializeField] private int enemyPierceCount;
    [SerializeField] private DiceFaceExtensionPort[] extensionPorts = Array.Empty<DiceFaceExtensionPort>();
    [SerializeField] private BulletEventEffect[] onFireEffects = Array.Empty<BulletEventEffect>();
    [SerializeField] private BulletEventEffect[] onHitEffects = Array.Empty<BulletEventEffect>();
    [SerializeField] private BulletEventEffect[] onFireEndEffects = Array.Empty<BulletEventEffect>();
}
```

Also add read-only properties and `[Serializable] public struct DiceFaceExtensionPort { public string Name; public float Value; }`.

- [ ] **Step 4: Implement loadout**

Use a serialized array of six slots and clamp face lookup to `1..6`. Raise `EntryChanged(int face, DiceFaceEntry entry)` after valid equip.

- [ ] **Step 5: Verify tests pass**

Run EditMode tests. Expected: `DiceChamberTests` and `DiceFaceLoadoutTests` pass.

---

### Task 4: Projectile Runtime Stats

**Files:**
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`

**Interfaces:**
- Produces:
  - `Projectile.Configure(ProjectileRuntimeStats stats)`
  - `ProjectileRuntimeStats` serializable struct or class
  - `Projectile.Damage`, `Projectile.ProjectileTag`, `Projectile.EnemyPierceCount`

- [ ] **Step 1: Write a failing projectile stats test**

Create `Assets/Tests/EditMode/ProjectileStatsTests.cs`:

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

- [ ] **Step 2: Verify test fails**

Run EditMode tests. Expected: compile fails because `Configure` and `ProjectileRuntimeStats` do not exist.

- [ ] **Step 3: Implement runtime stats**

Add immutable `ProjectileRuntimeStats` with fields for projectile type, tag, damage, distance, speed, pierce. In `Projectile.Configure`, clamp speed and distance to positive values, set private runtime fields, and compute lifetime as `distance / speed`.

- [ ] **Step 4: Keep existing default behavior**

Initialize runtime fields from current serialized `speed` and `lifetime` so old shots still work when no dice-face entry is equipped.

- [ ] **Step 5: Verify tests pass**

Run EditMode tests. Expected: projectile stats test passes.

---

### Task 5: Bullet Event Effect Base and Prototype Effects

**Files:**
- Create: `Assets/Scripts/Prototype/BulletEventEffect.cs`
- Create: `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`
- Create: `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs`
- Create: `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs`
- Create: `Assets/Tests/EditMode/BulletEventEffectTests.cs`

**Interfaces:**
- Produces:
  - `BulletEventEffect.Trigger(BulletEventContext context)`
  - `BulletEventContext` with gun, chamber, shot, hit collider, hit position, and recursion flag
  - Effect implementations listed in the spec

- [ ] **Step 1: Write failing force-face-4 test**

```csharp
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class BulletEventEffectTests
    {
        [Test]
        public void ForceFaceFourRefillsMissingFourAndForcesNextDraw()
        {
            DiceChamber chamber = new DiceChamber(6);
            while (chamber.ContainsFace(4))
            {
                chamber.TryDrawFace(out _);
            }

            ForceFaceFourOnFireEndEffect effect = ScriptableObject.CreateInstance<ForceFaceFourOnFireEndEffect>();
            effect.Trigger(new BulletEventContext(null, chamber, null, null, Vector3.zero, false));

            chamber.TryDrawFace(out int face);

            Assert.That(face, Is.EqualTo(4));

            Object.DestroyImmediate(effect);
        }
    }
}
```

- [ ] **Step 2: Verify test fails**

Run EditMode tests. Expected: compile fails because effect types do not exist.

- [ ] **Step 3: Implement effect base**

`BulletEventEffect` should be an abstract ScriptableObject with:

```csharp
public abstract void Trigger(BulletEventContext context);
```

`BulletEventContext` should be a readonly struct with nullable references and no dependency on UI.

- [ ] **Step 4: Implement force-face-4 effect**

If `context.Chamber` is null, return. If face 4 exists, return. Otherwise call `TryRefillFace(4)` and `TryForceNextFace(4)`.

- [ ] **Step 5: Implement extra shot and explosion effects**

`ExtraShotOnFireEffect` calls a non-recursive gun method such as `SpawnConfiguredProjectile(context.Shot, allowTriggeredEffects: false)` when `context.CanTriggerAdditionalShots` is true.

`ExplosionOnHitEffect` exposes:

```csharp
[SerializeField] private Projectile explosionProjectilePrefab;
```

It instantiates that prefab at `context.HitPosition` if present; otherwise logs a warning and returns.

- [ ] **Step 6: Verify tests pass**

Run EditMode tests. Expected: all event tests pass.

---

### Task 6: Revolver Integration

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverHitContext.cs`

**Interfaces:**
- Consumes:
  - `DiceFaceLoadout.GetEntry(int face)`
  - `DiceFaceEntry` bullet stats and event arrays
  - `BulletEventEffect.Trigger(BulletEventContext context)`
- Produces:
  - Shot context includes `DiceFaceEntry Entry` and `ProjectileRuntimeStats Stats`
  - Gun can spawn one configured projectile without recursively triggering event effects

- [ ] **Step 1: Add serialized loadout reference without changing tuning fields**

Add:

```csharp
[SerializeField] private DiceFaceLoadout loadout;
```

In `Awake`, if `loadout == null`, use `GetComponentInParent<DiceFaceLoadout>()`.

- [ ] **Step 2: Resolve shot entry and stats**

After drawing `face`, call `DiceFaceEntry entry = loadout != null ? loadout.GetEntry(face) : null;`. Build `ProjectileRuntimeStats` from entry if present; otherwise use defaults derived from current projectile behavior.

- [ ] **Step 3: Spawn configured projectile**

Extract projectile spawning into:

```csharp
private Projectile SpawnProjectile(Vector3 origin, Vector3 direction, Quaternion rotation, Projectile prefab, ProjectileRuntimeStats stats)
```

This method instantiates, configures, launches, and bridges hit reporting.

- [ ] **Step 4: Execute event arrays at existing timing**

Call entry `onFireEffects` after primary shot context creation, call `onHitEffects` from `ProjectileHitReporter`, and call `onFireEndEffects` where `FireEnded` currently fires.

- [ ] **Step 5: Preserve public C# events**

Keep existing `FireStarted`, `ProjectileHit`, `FireEnded`, `ReloadStarted`, `ReloadCompleted` events so current ammo UI keeps working.

- [ ] **Step 6: Compile check**

Run Unity batchmode compile or EditMode tests. Expected: no compiler errors.

---

### Task 7: Build Page UI

**Files:**
- Create: `Assets/Scripts/Prototype/DiceBuildPageUI.cs`
- Create: `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`
- Create: `Assets/Scripts/Prototype/DiceBuildEntryButtonUI.cs`

**Interfaces:**
- Consumes:
  - `DiceFaceLibrary.Entries`
  - `DiceFaceLoadout.Equip(int face, DiceFaceEntry entry)`
  - `DiceFaceLoadout.EntryChanged`
- Produces:
  - `E` toggles page visibility
  - Click entry then click face equips entry

- [ ] **Step 1: Implement face slot UI component**

`DiceBuildFaceSlotUI` has serialized references to `Button`, face label, and entry label. It exposes:

```csharp
public void Bind(int face, DiceFaceEntry entry, Action<int> clicked);
public void SetEntry(DiceFaceEntry entry);
```

- [ ] **Step 2: Implement entry button UI component**

`DiceBuildEntryButtonUI` has serialized references to `Button`, name label, description label, and background image. It exposes:

```csharp
public void Bind(DiceFaceEntry entry, Action<DiceFaceEntry> clicked);
public void SetSelected(bool selected);
```

- [ ] **Step 3: Implement page controller**

`DiceBuildPageUI` stores references to page root, loadout, library, face slot parent, entry list parent, and template prefabs. It toggles `pageRoot.SetActive(...)` on `Keyboard.current.eKey.wasPressedThisFrame`.

- [ ] **Step 4: Generate the six-face layout**

Use the same coordinates as ammo UI:

```csharp
face 1: column 1, row 0
face 2: column 0, row 1
face 3: column 1, row 1
face 4: column 2, row 1
face 5: column 3, row 1
face 6: column 1, row 2
```

- [ ] **Step 5: Update UI after equip**

When an entry is selected and a face is clicked, call `loadout.Equip(face, selectedEntry)` and update the face slot label. If no entry is selected, do nothing.

- [ ] **Step 6: Manual UI verification**

In Play Mode, press `E`, select a right-side entry, click a left-side face, and confirm the label changes.

---

### Task 8: Prototype Assets and Scene Wiring

**Files:**
- Create: `Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs`
- Create assets under:
  - `Assets/Data/DiceFaces`
  - `Assets/Data/BulletEvents`
  - `Assets/Data/Libraries`
- Modify only necessary scene objects in `Assets/Scenes/TopDownShooterPrototype.unity`
- Modify only necessary references on `Assets/Prefab/Player.prefab`

**Interfaces:**
- Consumes all runtime scripts from earlier tasks
- Produces wired prototype available in the current scene

- [ ] **Step 1: Create editor utility menu**

Add `[MenuItem("Dice Revolver/Setup Dice Face Build Prototype")]`.

- [ ] **Step 2: Create folders and assets if missing**

Use `AssetDatabase.IsValidFolder` and `AssetDatabase.CreateAsset`. Only create missing assets; if an asset already exists, load it and preserve its serialized values.

- [ ] **Step 3: Create three sample dice-face entries**

Examples:

- `Double Tap`: uses `ExtraShotOnFireEffect`
- `Blast Round`: uses `ExplosionOnHitEffect`
- `Loaded Four`: uses `ForceFaceFourOnFireEndEffect`

- [ ] **Step 4: Add or find `DiceFaceLoadout`**

Attach `DiceFaceLoadout` to the player only if missing. Do not modify transform values or renderer sorting.

- [ ] **Step 5: Assign revolver loadout reference**

Set only the new `loadout` serialized field on `DiceRevolverGun`. Do not set `shotsPerSecond`, `reloadDuration`, `reloadDropDistance`, `reloadBlinkSpeed`, projectile speed, or hand rig values.

- [ ] **Step 6: Create UI under HUD canvas**

Find existing `DiceRevolverHUD`; if missing, create a new overlay canvas. Add the build page root hidden by default, left dice net, right entry list, and wire `DiceBuildPageUI` references.

- [ ] **Step 7: Save assets and scene**

Call `AssetDatabase.SaveAssets()` and `EditorSceneManager.SaveScene(...)` only after targeted changes.

---

### Task 9: Final Verification

**Files:**
- Read/check all modified files

**Interfaces:**
- Confirms the end-to-end prototype works

- [ ] **Step 1: Run EditMode tests**

Run:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity Projects\DiceRevolver" -runTests -testPlatform EditMode -testResults "D:\Unity Projects\DiceRevolver\Logs\editmode-results.xml" -logFile "D:\Unity Projects\DiceRevolver\Logs\editmode-tests.log"
```

Expected: all EditMode tests pass. If direct project access is blocked because the editor is open, run on a temporary copy and report that route.

- [ ] **Step 2: Run compile verification**

Run Unity batchmode without scene builder:

```powershell
& "D:\Unity\6000.3.10f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity Projects\DiceRevolver" -logFile "D:\Unity Projects\DiceRevolver\Logs\dice-face-build-compile.log"
```

Expected: log includes `Tundra build success` and final return code `0`.

- [ ] **Step 3: Verify protected values were not overwritten**

Read `Assets/Prefab/Player.prefab` and confirm the implementation did not overwrite:

- `Body` local scale
- `Body` local Y position
- `AimRoot`, `ArmVisual`, `GunBody`, `Muzzle` local position/rotation
- renderer sorting layer/order
- `DiceRevolverGun` existing tuning fields

- [ ] **Step 4: Manual Play Mode checklist**

In Unity:

- Press `E` opens build page.
- Press `E` again closes build page.
- Right-side library entries appear.
- Click an entry, then click face 1-6; the face label updates.
- Fire with left mouse; selected face entry changes projectile stats.
- Extra-shot effect fires once and does not chain infinitely.
- Explosion effect spawns configured projectile on hit.
- Fire-end effect refills face 4 only when missing and forces the next draw to 4.

- [ ] **Step 5: Report remaining gaps**

Report any manual verification that could not be completed, especially if Play Mode could not be controlled from batchmode.
