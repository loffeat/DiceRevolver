# DiceRevolverGun 核心重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在玩法表现不变的前提下，以固定六面规则、`DiceRevolverRuntime` 和 `DiceShotPipeline` 重建左轮底层，并让 `DiceRevolverGun` 只保留 Unity 适配职责。

**Architecture:** `DiceRevolverRuntime` 独占抽面、冷却和换弹机械状态；`DiceShotPipeline` 独占一次骰面激活的配置快照、阶段执行、预算、延迟和命中派发；`DiceRevolverGun` 读取角色意图并完成弹丸实例化、视觉和公开事件转发。`Projectile` 自己广播命中，事件上下文只持有生成、调度和“补回并强制骰面”能力，不再暴露 Gun 或可变弹巢。

**Tech Stack:** Unity `6000.3.10f1`、C#、Unity Test Framework `1.6.0`、NUnit EditMode tests、Unity YAML Prefab。

**Spec:** `docs/superpowers/specs/2026-08-20-dice-revolver-core-refactor-design.md`

## Global Constraints

- 固定使用 `DiceRevolverRules.FaceCount = 6`；不得新增可配置面数端口。
- `eventBudgetPerActivation` 的 Inspector 名称为“单次骰面事件预算”，默认 `32`，最小 `1`，并在每次激活创建时固化。
- 保持顺序：`FireStarted` → Base → OnFire → `FireEnded` → OnFireEnd；OnHit 在对应弹丸后续命中时执行。
- 自动换弹只可在 OnFireEnd 完成后判断，确保 LoadedFour 能补回并强制骰面 4。
- 命中顺序固定为：捕获命中 → Projectile 命中广播 → `ProjectileHit` → OnHit → 直接伤害 → 销毁。
- 保持六面不放回抽取、射速、换弹时序、同帧换弹完成后可开火、配置快照、DoubleTap、BlastRound、LoadedFour、主弹/附加弹命中资格和异常隔离。
- Player 与 TestRobot Prefab 只允许删除 `faceCount`、删除 `reloadDropDistance`、新增 `eventBudgetPerActivation: 32`；其余枪械数值、引用、Transform 和 Sorting Layer 不变。
- 删除 `ProjectileHitReporter` 脚本、meta、Prefab 组件和 Builder 接线，不保留兼容壳。
- 不实现任何雷电构筑，不新增全局事件总线、时间单例、对象池或阵营系统。
- 每个任务遵循红—绿—重构；聚焦测试通过后才能提交。最终完整 EditMode 回归只允许具名已知的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities` Ground Y 失败且不得新增失败，结果必须如实记为 `[failed]`；项目上下文检查必须通过。

## File Structure

- Create `Assets/Scripts/Prototype/DiceRevolverRules.cs`: 项目唯一六面规则来源。
- Create `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`: 抽面、冷却、手动/自动换弹和 LoadedFour 受限操作。
- Create `Assets/Scripts/Prototype/DiceShotPipeline.cs`: 激活创建、阶段执行、预算、调度、生成请求和命中派发。
- Create `Assets/Tests/EditMode/DiceRevolverRulesTests.cs`: 固定规则和边界使用验证。
- Create `Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs`: 纯机械状态测试，替代 `DiceChamberTests`。
- Create `Assets/Tests/EditMode/DiceShotPipelineTests.cs`: 公开 Pipeline 接口测试，替代 Gun 私有实现反射测试。
- Modify `Assets/Scripts/Prototype/DiceFaceActivation.cs`: 删除 Gun/Chamber，注入受限能力和预算 warning。
- Modify `Assets/Scripts/Prototype/BulletEventContext.cs`: 只公开词条需要的能力。
- Modify `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs`: 使用受限补回/强制请求。
- Modify `Assets/Scripts/Prototype/DiceRevolverGun.cs`: 缩减为 Runtime/Pipeline 的 MonoBehaviour 适配器。
- Modify `Assets/Scripts/Prototype/Projectile.cs`: 合并命中广播、直接伤害和销毁顺序。
- Modify `Assets/Scripts/Prototype/DiceFaceLoadout.cs`, `DiceBuildPageUI.cs`, `DiceBuildRuntimeView.cs`: 使用统一 `FaceCount`。
- Modify `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`, `TestRobotPrototypeBuilder.cs`, `ProjectileDefinitionPrototypeBuilder.cs`: 使用统一规则、写入预算、移除 Reporter 接线。
- Modify `Assets/Tests/EditMode/DiceFaceActivationTests.cs`, `BulletEventEffectTests.cs`, `DiceRevolverGunIntegrationTests.cs`, `ProjectileCollisionTests.cs`, `ProjectileDefinitionAssetTests.cs`, `CombatInspectorLocalizationTests.cs`, `DiceFaceLoadoutTests.cs`, `DiceBuildUITests.cs`, `TestRobotAssetTests.cs`: 迁移到新公开契约并增加资源保护断言。
- Modify `Assets/Prefab/Player.prefab`, `Assets/Prefab/TestRobot.prefab`, `Assets/Prefab/Projectiles/BasicRevolverBullet.prefab`: 执行限定 YAML 迁移。
- Delete in Task 6: `Assets/Scripts/Prototype/DiceChamber.cs`, `Assets/Scripts/Prototype/DiceChamber.cs.meta`, `Assets/Tests/EditMode/DiceChamberTests.cs`, `Assets/Tests/EditMode/DiceChamberTests.cs.meta`: Gun 完成切换后移除旧机械状态。
- Delete in Task 7: `Assets/Scripts/Prototype/ProjectileHitReporter.cs`, `Assets/Scripts/Prototype/ProjectileHitReporter.cs.meta`: Prefab、Builder 和测试全部迁移时一并移除旧命中组件。

---

### Task 1: 记录基线并统一固定六面规则

**Files:**
- Create: `Assets/Scripts/Prototype/DiceRevolverRules.cs`
- Create: `Assets/Tests/EditMode/DiceRevolverRulesTests.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildPageUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildRuntimeView.cs`
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Modify: `Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs`
- Modify: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`
- Modify: `Assets/Tests/EditMode/DiceBuildUITests.cs`
- Modify: `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`
- Modify: `Assets/Tests/EditMode/TestRobotAssetTests.cs`

**Interfaces:**
- Consumes: 现有 `DiceFaceLoadout.Equip/GetSnapshot/GetBaseEffect` 和构筑页生成行为。
- Produces: `public static class DiceRevolverRules`，公开 `public const int FaceCount = 6`；后续 Runtime、Gun、UI 和 Builder 都依赖该常量。

- [ ] **Step 1: 在改代码前运行完整 EditMode 基线**

```powershell
New-Item -ItemType Directory -Force -Path .\Logs | Out-Null
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults .\Logs\core-refactor-baseline.xml -logFile .\Logs\core-refactor-baseline.log
Select-String -LiteralPath .\Logs\core-refactor-baseline.xml -Pattern '<test-run '
```

Expected: Unity 返回 `0`，结果为 `139/139`、`failed="0"`、`skipped="0"`。若仓库基线已经自然增加测试，只接受“全部通过”，并把实际总数写入工作流验证记录。

- [ ] **Step 2: 写固定规则红灯测试**

```csharp
[Test]
public void FaceCountIsTheDomainFixedSix()
{
    Assert.That(DiceRevolverRules.FaceCount, Is.EqualTo(6));
}

[Test]
public void LoadoutRejectsFacesOutsideTheFixedRule()
{
    GameObject owner = new("Loadout");
    DiceFaceLoadout loadout = owner.AddComponent<DiceFaceLoadout>();
    DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
    loadout.Equip(0, entry);
    loadout.Equip(DiceRevolverRules.FaceCount + 1, entry);
    Assert.That(loadout.GetEntry(0, DiceFaceSlotType.Base), Is.Null);
    Assert.That(loadout.GetEntry(DiceRevolverRules.FaceCount + 1, DiceFaceSlotType.Base), Is.Null);
    Object.DestroyImmediate(entry);
    Object.DestroyImmediate(owner);
}
```

- [ ] **Step 3: 运行规则和 Loadout 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverRulesTests;DiceRevolver.Tests.DiceFaceLoadoutTests" -testResults .\Logs\rules-red.xml -logFile .\Logs\rules-red.log
```

Expected: FAIL，编译错误明确指出 `DiceRevolverRules` 不存在。

- [ ] **Step 4: 添加唯一规则并替换生产代码中的六面魔数**

```csharp
namespace DiceRevolver.Prototype
{
    public static class DiceRevolverRules
    {
        public const int FaceCount = 6;
    }
}
```

将数组长度、合法面判断和循环上限统一改为：

```csharp
new DiceFaceConfiguration[DiceRevolverRules.FaceCount]
face < 1 || face > DiceRevolverRules.FaceCount
for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
Array.Resize(ref entries, DiceRevolverRules.FaceCount)
```

测试循环也使用常量，但保留 `Assert.That(DiceRevolverRules.FaceCount, Is.EqualTo(6))` 作为领域契约。

- [ ] **Step 5: 运行固定规则、Loadout、UI 和资源聚焦测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverRulesTests;DiceRevolver.Tests.DiceFaceLoadoutTests;DiceRevolver.Tests.DiceBuildUITests;DiceRevolver.Tests.ProjectileDefinitionAssetTests;DiceRevolver.Tests.TestRobotAssetTests" -testResults .\Logs\rules-green.xml -logFile .\Logs\rules-green.log
```

Expected: PASS，六个骰面仍全部可装备和渲染。

- [ ] **Step 6: 提交固定规则**

```powershell
git add -- Assets/Scripts/Prototype/DiceRevolverRules.cs Assets/Scripts/Prototype/DiceRevolverRules.cs.meta Assets/Scripts/Prototype/DiceFaceLoadout.cs Assets/Scripts/Prototype/DiceBuildPageUI.cs Assets/Scripts/Prototype/DiceBuildRuntimeView.cs Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs Assets/Tests/EditMode/DiceRevolverRulesTests.cs Assets/Tests/EditMode/DiceRevolverRulesTests.cs.meta Assets/Tests/EditMode/DiceFaceLoadoutTests.cs Assets/Tests/EditMode/DiceBuildUITests.cs Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs Assets/Tests/EditMode/TestRobotAssetTests.cs
git commit -m "统一左轮固定六面规则"
```

### Task 2: 用 DiceRevolverRuntime 集中机械状态

**Files:**
- Create: `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`
- Create: `Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs`

**Interfaces:**
- Consumes: `DiceRevolverRules.FaceCount`。
- Produces: `DiceRevolverRuntime`, `DiceRevolverDrawStatus`, `DiceRevolverDrawResult`, `DiceRevolverRuntimeUpdate`；Gun 和 Pipeline 只通过这些公开接口访问机械状态。

- [ ] **Step 1: 写 Runtime 红灯测试**

```csharp
[Test]
public void DrawsAllSixFacesWithoutReplacement()
{
    DiceRevolverRuntime runtime = new(5f, 1.8f, true, true);
    HashSet<int> faces = new();
    for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
    {
        DiceRevolverDrawResult result = runtime.TryBeginShot(i);
        Assert.That(result.Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
        faces.Add(result.Face);
    }
    Assert.That(faces, Has.Count.EqualTo(DiceRevolverRules.FaceCount));
    Assert.That(runtime.RemainingRounds, Is.Zero);
}

[Test]
public void CompleteShotChecksAutomaticReloadAfterLoadedFourCapability()
{
    DiceRevolverRuntime runtime = new(100f, 2f, true, true);
    for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
        runtime.TryBeginShot(i * 0.02f);

    Assert.That(runtime.TryRefillAndForceNextFace(4), Is.True);
    Assert.That(runtime.CompleteShot(1f).ReloadStarted, Is.False);
    Assert.That(runtime.TryBeginShot(1.1f).Face, Is.EqualTo(4));
}
```

同一测试文件还必须覆盖：冷却时返回 `CoolingDown`、换弹中返回 `Reloading`、空膛返回 `Empty` 且不自行开始换弹、`CompleteShot` 是自动换弹唯一入口、手动换弹仅在未满膛时开始、换弹完成恢复六面、完成换弹的同一时间点允许射击。

- [ ] **Step 2: 运行 Runtime 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter DiceRevolver.Tests.DiceRevolverRuntimeTests -testResults .\Logs\runtime-red.xml -logFile .\Logs\runtime-red.log
```

Expected: FAIL，缺少 Runtime 类型。

- [ ] **Step 3: 实现明确的 Runtime 结果类型和公开接口**

```csharp
public enum DiceRevolverDrawStatus
{
    Fired,
    CoolingDown,
    Reloading,
    Empty
}

public readonly struct DiceRevolverDrawResult
{
    public DiceRevolverDrawResult(DiceRevolverDrawStatus status, int face = 0)
    {
        Status = status;
        Face = face;
    }
    public DiceRevolverDrawStatus Status { get; }
    public int Face { get; }
}

public readonly struct DiceRevolverRuntimeUpdate
{
    public DiceRevolverRuntimeUpdate(bool reloadStarted, bool reloadCompleted)
    {
        ReloadStarted = reloadStarted;
        ReloadCompleted = reloadCompleted;
    }
    public bool ReloadStarted { get; }
    public bool ReloadCompleted { get; }
}
```

`DiceRevolverRuntime` 的最终公开面固定为：

```csharp
public DiceRevolverRuntime(float shotsPerSecond, float reloadDuration,
    bool automaticReloadWhenEmpty, bool allowManualReload);
public int RemainingRounds { get; }
public bool IsReloading { get; }
public float ReloadDuration { get; set; } // setter 保持 Mathf.Max(0.05f, value)
public DiceRevolverRuntimeUpdate Tick(float currentTime, bool manualReloadRequested);
public DiceRevolverDrawResult TryBeginShot(float currentTime);
public DiceRevolverRuntimeUpdate CompleteShot(float currentTime);
public bool TryRefillAndForceNextFace(int face);
public float GetReloadProgress(float currentTime);
```

内部用长度固定为六面的 `List<int>` 管理不放回抽取；`TryRefillAndForceNextFace` 先验证 `1..FaceCount`，缺失时补回、排序并设为下次强制面，已存在时返回 `false`。`CompleteShot` 是唯一“本发结束后自动换弹”入口。`GetReloadProgress` 在非换弹状态返回 `0`，换弹状态返回基于起始时间和 `ReloadDuration` 的 `0..1` 值。

- [ ] **Step 4: 保持旧 Gun 路径不动，完成纯 Runtime 最小实现**

本任务不修改 Gun、Activation 或旧 `DiceChamber`，避免产生半迁移状态。新 Runtime 自己拥有一套固定六面机械状态，并由纯模块测试证明完整行为；Gun 在 Task 6 一次性切换后再删除旧 Chamber。

- [ ] **Step 5: 运行 Runtime 和现有 Gun 集成测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverRuntimeTests;DiceRevolver.Tests.DiceChamberTests" -testResults .\Logs\runtime-green.xml -logFile .\Logs\runtime-green.log
```

Expected: PASS；新 Runtime 行为完整，旧 Chamber 回归仍通过。

- [ ] **Step 6: 提交 Runtime**

```powershell
git add -- Assets/Scripts/Prototype/DiceRevolverRuntime.cs Assets/Scripts/Prototype/DiceRevolverRuntime.cs.meta Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs.meta
git commit -m "抽取左轮运行时机械状态"
```

### Task 3: 收窄 DiceFaceActivation 与事件上下文权限

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventContext.cs`
- Modify: `Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Modify: `Assets/Tests/EditMode/DiceFaceActivationTests.cs`
- Modify: `Assets/Tests/EditMode/BulletEventEffectTests.cs`

**Interfaces:**
- Consumes: `DiceRevolverRuntime.TryRefillAndForceNextFace(int)`。
- Produces: 不含 Gun/Chamber 的 `DiceFaceActivation` 构造函数，以及 `BulletEventContext.RequestRefillAndForceNextFace(int)`。

- [ ] **Step 1: 写权限和预算快照红灯测试**

```csharp
[Test]
public void ActivationClampsBudgetToAtLeastOneAndWarnsOnce()
{
    List<string> warnings = new();
    DiceFaceActivation activation = CreateActivation(eventBudget: 0, warningAction: warnings.Add);
    Assert.That(activation.RemainingEventBudget, Is.EqualTo(1));
    Assert.That(activation.TryConsumeEventBudget(), Is.True);
    Assert.That(activation.TryConsumeEventBudget(), Is.False);
    Assert.That(activation.TryConsumeEventBudget(), Is.False);
    Assert.That(warnings, Has.Count.EqualTo(1));
}

[Test]
public void ForceFaceFourUsesOnlyBoundedCapability()
{
    int requestedFace = 0;
    DiceFaceActivation activation = CreateActivation(
        refillAndForceNextFaceAction: face => { requestedFace = face; return true; });
    ForceFaceFourOnFireEndEffect effect = ScriptableObject.CreateInstance<ForceFaceFourOnFireEndEffect>();
    effect.Trigger(new BulletEventContext(activation, null, null, Vector3.zero));
    Assert.That(requestedFace, Is.EqualTo(4));
    Object.DestroyImmediate(effect);
}
```

增加反向编译契约检查：`typeof(DiceFaceActivation).GetProperty("Gun")`、`GetProperty("Chamber")`、`typeof(BulletEventContext).GetProperty(...)` 均为 `null`。

- [ ] **Step 2: 运行 Activation 与 Effect 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceFaceActivationTests;DiceRevolver.Tests.BulletEventEffectTests" -testResults .\Logs\activation-red.xml -logFile .\Logs\activation-red.log
```

Expected: FAIL，因为旧构造函数仍要求 Gun/Chamber，且没有受限请求。

- [ ] **Step 3: 将 Activation 构造函数改为只接收能力回调**

```csharp
public DiceFaceActivation(
    int face,
    DiceFaceConfigurationSnapshot configuration,
    Vector3 origin,
    Vector3 direction,
    Action<float, Action> scheduleAction,
    Action<ProjectileSpawnRequest> spawnAction,
    Func<int, bool> refillAndForceNextFaceAction,
    Action<string> warningAction,
    int eventBudget = DefaultEventBudget)
```

删除 `Gun` 和 `Chamber` 属性；预算初始化使用 `Mathf.Max(1, eventBudget)`；耗尽时只调用一次：

```csharp
warningAction?.Invoke($"Dice face {Face} stopped because its event budget was exhausted.");
```

新增：

```csharp
public bool RequestRefillAndForceNextFace(int face)
{
    return refillAndForceNextFaceAction != null && refillAndForceNextFaceAction.Invoke(face);
}
```

- [ ] **Step 4: 收窄 BulletEventContext 并迁移 LoadedFour**

```csharp
public bool RequestRefillAndForceNextFace(int face)
{
    return Activation != null && Activation.RequestRefillAndForceNextFace(face);
}
```

`ForceFaceFourOnFireEndEffect.Trigger` 只保留：

```csharp
context.RequestRefillAndForceNextFace(4);
```

在 Gun 尚未切换 Runtime 的这一过渡提交中，新 Activation 构造函数的能力回调接到现有 Chamber：验证 4 不存在后调用 `TryRefillFace(4)` 和 `TryForceNextFace(4)`。Task 6 将这段过渡 lambda 替换为 `runtime.TryRefillAndForceNextFace`。

- [ ] **Step 5: 运行 Activation 与 Effect 聚焦测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceFaceActivationTests;DiceRevolver.Tests.BulletEventEffectTests" -testResults .\Logs\activation-green.xml -logFile .\Logs\activation-green.log
```

Expected: PASS，DoubleTap、Explosion、ProjectileSpawn 和 LoadedFour 测试全部通过。

- [ ] **Step 6: 提交受限事件能力**

```powershell
git add -- Assets/Scripts/Prototype/DiceFaceActivation.cs Assets/Scripts/Prototype/BulletEventContext.cs Assets/Scripts/Prototype/ForceFaceFourOnFireEndEffect.cs Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Tests/EditMode/DiceFaceActivationTests.cs Assets/Tests/EditMode/BulletEventEffectTests.cs
git commit -m "收窄骰面事件上下文能力"
```

### Task 4: 提取可直接测试的 DiceShotPipeline

**Files:**
- Create: `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- Create: `Assets/Tests/EditMode/DiceShotPipelineTests.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventTimeScheduler.cs` only if a public behavior required by Pipeline tests is currently unavailable; do not expose its queue.

**Interfaces:**
- Consumes: Activation 的受限构造函数、`DiceRevolverRuntime.TryRefillAndForceNextFace`、现有 `BulletEventEffect.Trigger`。
- Produces: `DiceShotPipeline.ExecuteShot/Tick/HandleHit/Clear`，以及生成回调 `Action<DiceFaceActivation, ProjectileSpawnRequest>`。

- [ ] **Step 1: 写 Pipeline 阶段、快照和命中红灯测试**

```csharp
[Test]
public void ExecuteShotKeepsApprovedStageOrder()
{
    List<string> order = new();
    DiceShotPipeline pipeline = CreatePipeline(order);
    DiceFaceConfigurationSnapshot snapshot = CreateSnapshot(
        Effect("base", order), Effect("on-fire", order), null, Effect("on-fire-end", order));

    pipeline.ExecuteShot(2, snapshot, Vector3.zero, Vector3.forward, 32,
        _ => order.Add("fire-started"), _ => order.Add("fire-ended"));

    Assert.That(order, Is.EqualTo(new[]
    {
        "fire-started", "base", "on-fire", "fire-ended", "on-fire-end"
    }));
}

[Test]
public void HandleHitNotifiesObserverBeforeQualifiedOnHit()
{
    List<string> order = new();
    DiceShotPipeline pipeline = CreatePipeline(order);
    DiceRevolverShotContext shot = CreateShotWithOnHit(Effect("on-hit", order), true);
    pipeline.HandleHit(shot, null, Vector3.one, _ => order.Add("projectile-hit"));
    Assert.That(order, Is.EqualTo(new[] { "projectile-hit", "on-hit" }));
}
```

同一文件必须通过公开接口覆盖：四槽位快照在装备改变后不变、`CanTriggerHitEffects=false` 不执行 OnHit、延迟回调仍持有原激活、全部阶段共享同一预算、预算耗尽 warning 一次、后续执行使用新预算而在途激活保留旧预算、单个 Effect 抛异常后 Pipeline 仍可处理其他激活。

- [ ] **Step 2: 运行 Pipeline 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter DiceRevolver.Tests.DiceShotPipelineTests -testResults .\Logs\pipeline-red.xml -logFile .\Logs\pipeline-red.log
```

Expected: FAIL，`DiceShotPipeline` 不存在。

- [ ] **Step 3: 实现 Pipeline 的最终公开接口**

```csharp
public sealed class DiceShotPipeline
{
    public DiceShotPipeline(
        Func<float> currentTime,
        Action<DiceFaceActivation, ProjectileSpawnRequest> spawnProjectile,
        Func<int, bool> refillAndForceNextFace,
        Action<string> logWarning,
        Action<Exception, UnityEngine.Object> logException);

    public DiceFaceActivation ExecuteShot(
        int face, DiceFaceConfigurationSnapshot configuration,
        Vector3 origin, Vector3 direction, int eventBudget,
        Action<DiceRevolverShotContext> fireStarted,
        Action<DiceRevolverShotContext> fireEnded);

    public void HandleHit(
        DiceRevolverShotContext shot, Collider hitCollider, Vector3 hitPosition,
        Action<DiceRevolverHitContext> hitObserved);

    public void Tick(float currentTime);
    public void Clear();
}
```

`ExecuteShot` 创建 Activation 和 face-trigger ShotContext，并严格按批准顺序调用回调与 `TriggerEffect`。`HandleHit` 先调用 `hitObserved`，仅当 `shot.CanTriggerHitEffects` 为真时触发快照 OnHit。`TriggerEffect` 是 Pipeline 内部唯一的效果执行入口，统一消费预算并捕获异常。

生成回调用局部 Activation 自引用接线，确保 Gun 收到请求时能建立正确 ShotContext：

```csharp
DiceFaceActivation activation = null;
activation = new DiceFaceActivation(
    face, configuration, origin, direction,
    (delay, callback) => scheduler.Schedule(currentTime.Invoke(), delay, callback),
    request => spawnProjectile?.Invoke(activation, request),
    refillAndForceNextFace, logWarning, eventBudget);
```

- [ ] **Step 4: 让 Pipeline 自己拥有延迟调度**

Activation 的 `scheduleAction` 固定接线为：

```csharp
(delay, callback) => scheduler.Schedule(currentTime.Invoke(), delay, callback)
```

`Tick(currentTime)` 调用 `scheduler.Tick(currentTime, exception => logException?.Invoke(exception, null))`；`Clear()` 清空队列。测试只调用 `pipeline.Tick(...)`，禁止反射获取 scheduler。

- [ ] **Step 5: 运行 Pipeline、Activation 和 Scheduler 测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceShotPipelineTests;DiceRevolver.Tests.DiceFaceActivationTests;DiceRevolver.Tests.BulletEventTimeSchedulerTests;DiceRevolver.Tests.BulletEventEffectTests" -testResults .\Logs\pipeline-green.xml -logFile .\Logs\pipeline-green.log
```

Expected: PASS，无测试读取 Pipeline 私有字段。

- [ ] **Step 6: 提交 Pipeline**

```powershell
git add -- Assets/Scripts/Prototype/DiceShotPipeline.cs Assets/Scripts/Prototype/DiceShotPipeline.cs.meta Assets/Scripts/Prototype/DiceFaceActivation.cs Assets/Scripts/Prototype/BulletEventTimeScheduler.cs Assets/Tests/EditMode/DiceShotPipelineTests.cs Assets/Tests/EditMode/DiceShotPipelineTests.cs.meta
git commit -m "提取骰面射击事件管线"
```

### Task 5: 让 Projectile 统一拥有命中生命周期

**Files:**
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Tests/EditMode/ProjectileCollisionTests.cs`

**Interfaces:**
- Consumes: `Projectile.ShouldIgnoreCollision(Collider)` 和 `IDamageReceiver.ReceiveDamage(DamageInfo)`。
- Produces: `public event Action<Collider, Vector3> Hit`；Gun 后续直接订阅该事件。

- [ ] **Step 1: 写命中顺序红灯测试**

```csharp
[Test]
public void HitBroadcastOccursBeforeDirectDamage()
{
    List<string> order = new();
    GameObject projectileOwner = new("Projectile");
    Projectile projectile = projectileOwner.AddComponent<Projectile>();
    GameObject target = new("Target");
    BoxCollider collider = target.AddComponent<BoxCollider>();
    RecordingDamageReceiver receiver = target.AddComponent<RecordingDamageReceiver>();
    receiver.Order = order;
    projectile.Hit += (_, _) => order.Add("hit");

    InvokeTrigger(projectile, collider);

    Assert.That(order, Is.EqualTo(new[] { "hit", "damage" }));
    Object.DestroyImmediate(target);
    Object.DestroyImmediate(projectileOwner);
}

public sealed class RecordingDamageReceiver : MonoBehaviour, IDamageReceiver
{
    public List<string> Order { get; set; }
    public void ReceiveDamage(DamageInfo damageInfo) => Order.Add("damage");
}
```

同时断言被忽略的弹丸/Player 碰撞既不广播也不伤害，以及 Hit 给出的坐标等于碰撞瞬间 `projectile.transform.position`。

- [ ] **Step 2: 运行 Projectile 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter DiceRevolver.Tests.ProjectileCollisionTests -testResults .\Logs\projectile-hit-red.xml -logFile .\Logs\projectile-hit-red.log
```

Expected: FAIL，`Projectile.Hit` 尚不存在。

- [ ] **Step 3: 在 Projectile 中实现唯一命中入口**

```csharp
public event Action<Collider, Vector3> Hit;

private void OnTriggerEnter(Collider other)
{
    if (ShouldIgnoreCollision(other))
        return;

    Vector3 hitPosition = transform.position;
    Hit?.Invoke(other, hitPosition);
    IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
    receiver?.ReceiveDamage(new DamageInfo(damage, hitPosition, gameObject));
    Destroy(gameObject);
}
```

不要吞掉订阅者异常；Pipeline 自己隔离词条异常。不要在这一任务修改穿透语义。

- [ ] **Step 4: 保留 Reporter 类型直到资源迁移任务**

Task 5 只建立 Projectile 的新命中契约。GUID `bc5c763f8ba93ae41bb8ee166d047733` 对应的 Reporter 暂时保留，使现有 Prefab、Builder 和旧集成测试在 Task 7 前仍可编译；不要给 Reporter 增加新职责。

- [ ] **Step 5: 运行 Projectile 与伤害测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.ProjectileCollisionTests;DiceRevolver.Tests.ProjectileStatsTests;DiceRevolver.Tests.TargetDummyTests;DiceRevolver.Tests.AreaExplosionProjectileTests" -testResults .\Logs\projectile-hit-green.xml -logFile .\Logs\projectile-hit-green.log
```

Expected: PASS，普通直击和范围伤害仍按原数值结算。

- [ ] **Step 6: 提交 Projectile 命中归并**

```powershell
git add -- Assets/Scripts/Prototype/Projectile.cs Assets/Tests/EditMode/ProjectileCollisionTests.cs
git commit -m "统一弹丸命中生命周期"
```

### Task 6: 将 DiceRevolverGun 缩减为 Runtime/Pipeline 的 Unity 适配器

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Modify: `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`
- Modify: `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`
- Delete: `Assets/Scripts/Prototype/DiceChamber.cs`
- Delete: `Assets/Scripts/Prototype/DiceChamber.cs.meta`
- Delete: `Assets/Tests/EditMode/DiceChamberTests.cs`
- Delete: `Assets/Tests/EditMode/DiceChamberTests.cs.meta`

**Interfaces:**
- Consumes: Runtime 的 `Tick/TryBeginShot/CompleteShot`，Pipeline 的 `ExecuteShot/Tick/HandleHit/Clear`，Projectile 的 `Hit`。
- Produces: 保持原公开事件和属性；新增序列化字段 `eventBudgetPerActivation`；删除公开 `SpawnConfiguredProjectile` 和所有旧私有 Pipeline 实现。

- [ ] **Step 1: 改写 Gun 集成测试，使其只验证 Unity 接线**

删除或迁移以下测试路径：对 `eventTimeScheduler`、`CreateEventContext`、`SpawnActivationProjectile`、`BridgeProjectileHit` 的反射，以及对公开 `SpawnConfiguredProjectile` 的直接调用。保留并改写为真实行为测试：

```csharp
[Test]
public void PlayerPrefabShotConsumesOneRoundAndSpawnsConfiguredProjectile()
{
    // 实例化 Player Prefab、调用 Unity 生命周期、注入左键输入。
    // 断言 RemainingRounds 从 6 变 5，且生成的 Projectile 属性来自定义。
}

[Test]
public void GunRelaysProjectileHitBeforeOnHitAndDirectDamage()
{
    // 通过 Gun 实际生成的 Projectile 触发碰撞。
    // 记录 ProjectileHit、测试 OnHit effect、IDamageReceiver 的顺序。
    Assert.That(order, Is.EqualTo(new[] { "projectile-hit", "on-hit", "damage" }));
}
```

增加字段契约：`faceCount` 与 `reloadDropDistance` 不存在；`eventBudgetPerActivation` 存在并具有 `[Min(1)]` 与 `[InspectorName("单次骰面事件预算")]`。

还要增加三个安全契约：缺少角色控制器时 `Update/LateUpdate` 不抛异常；缺少枪口时开火不消耗骰面；弹丸定义或 Prefab 缺失时跳过生成并只记录 warning，不中断后续激活。

- [ ] **Step 2: 运行 Gun 测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverGunIntegrationTests;DiceRevolver.Tests.CombatInspectorLocalizationTests" -testResults .\Logs\gun-adapter-red.xml -logFile .\Logs\gun-adapter-red.log
```

Expected: FAIL，新预算端口和新适配路径尚未完成。

- [ ] **Step 3: 初始化 Runtime 与 Pipeline，并委托公开状态**

```csharp
[SerializeField, Min(1), InspectorName("单次骰面事件预算")]
private int eventBudgetPerActivation = DiceFaceActivation.DefaultEventBudget;

private DiceRevolverRuntime runtime;
private DiceShotPipeline shotPipeline;

public int RemainingRounds => runtime?.RemainingRounds ?? 0;
public bool IsReloading => runtime?.IsReloading ?? false;
public float ReloadDuration
{
    get => runtime?.ReloadDuration ?? reloadDuration;
    set
    {
        reloadDuration = Mathf.Max(0.05f, value);
        if (runtime != null) runtime.ReloadDuration = reloadDuration;
    }
}
```

`Awake` 创建 Runtime 和 Pipeline；Pipeline 回调使用 `Time.time`、`SpawnActivationProjectile`、`runtime.TryRefillAndForceNextFace`、`Debug.LogWarning`、`Debug.LogException`。`OnDestroy` 只调用 `shotPipeline.Clear()`。

- [ ] **Step 4: 把 Update/LateUpdate 改成适配流程**

`Update` 调用 `runtime.Tick(Time.time, player.ReloadPressedThisFrame)`，换弹中用 `runtime.GetReloadProgress(Time.time)` 驱动 `AnimateReload`，并根据 `ReloadStarted/ReloadCompleted` 转发事件和重置视觉。`LateUpdate` 刷新瞄准，调用 `TryFire`，再调用 `shotPipeline.Tick(Time.time)`。

`TryFire` 的核心必须是：

```csharp
DiceRevolverDrawResult draw = runtime.TryBeginShot(Time.time);
if (draw.Status != DiceRevolverDrawStatus.Fired)
    return;

DiceFaceConfigurationSnapshot snapshot = loadout != null
    ? loadout.GetSnapshot(draw.Face) : default;
shotPipeline.ExecuteShot(draw.Face, snapshot, shotOrigin, shotDirection,
    Mathf.Max(1, eventBudgetPerActivation),
    shot => FireStarted?.Invoke(shot),
    shot => FireEnded?.Invoke(shot));

DiceRevolverRuntimeUpdate completion = runtime.CompleteShot(Time.time);
if (completion.ReloadStarted)
    NotifyReloadStarted();
```

必须保持“换弹完成后同一帧可以在 LateUpdate 开火”。

- [ ] **Step 5: 将生成弹丸与命中接线限制在 Gun 内**

保留一个私有 `SpawnActivationProjectile(DiceFaceActivation, ProjectileSpawnRequest)`；它实例化、应用 stats、Launch，并创建 `DiceRevolverShotContext`。订阅：

```csharp
projectile.Hit += (hitCollider, hitPosition) =>
    shotPipeline.HandleHit(shot, hitCollider, hitPosition,
        hit => ProjectileHit?.Invoke(hit));
```

删除 `SpawnConfiguredProjectile`、`CreateEventContext`、`TriggerEffect`、`BridgeProjectileHit`、Gun 自有 scheduler、Gun 自有 chamber、`nextShotTime/reloadStartedAt/isReloading`。删除旧 `DiceChamber.cs/.meta` 和 `DiceChamberTests.cs/.meta`；Gun 仍保留姿态、Prefab 实例化、reload blink 和公开事件。

- [ ] **Step 6: 运行 Gun、Runtime、Pipeline 联合测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverGunIntegrationTests;DiceRevolver.Tests.DiceRevolverRuntimeTests;DiceRevolver.Tests.DiceShotPipelineTests;DiceRevolver.Tests.CombatInspectorLocalizationTests" -testResults .\Logs\gun-adapter-green.xml -logFile .\Logs\gun-adapter-green.log
```

Expected: PASS；Gun 集成测试不再反射四个旧私有管线成员。

- [ ] **Step 7: 提交 Gun 适配器**

```powershell
git add -- Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Scripts/Prototype/DiceChamber.cs Assets/Scripts/Prototype/DiceChamber.cs.meta Assets/Tests/EditMode/DiceChamberTests.cs Assets/Tests/EditMode/DiceChamberTests.cs.meta Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs
git commit -m "收敛左轮 Unity 适配职责"
```

### Task 7: 精确迁移 Prefab 和 Editor Builder

**Files:**
- Modify: `Assets/Prefab/Player.prefab`
- Modify: `Assets/Prefab/TestRobot.prefab`
- Modify: `Assets/Prefab/Projectiles/BasicRevolverBullet.prefab`
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Modify: `Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs`
- Modify: `Assets/Scripts/Editor/ProjectileDefinitionPrototypeBuilder.cs`
- Modify: `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`
- Modify: `Assets/Tests/EditMode/TestRobotAssetTests.cs`
- Create: `Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs`
- Delete: `Assets/Scripts/Prototype/ProjectileHitReporter.cs`
- Delete: `Assets/Scripts/Prototype/ProjectileHitReporter.cs.meta`

**Interfaces:**
- Consumes: Gun 的新序列化字段、Projectile 的内建 Hit、`DiceRevolverRules.FaceCount`。
- Produces: 没有 Reporter 组件且预算为 32 的 Player/TestRobot/BasicRevolverBullet 资源。

- [ ] **Step 1: 写资源迁移红灯测试**

```csharp
[TestCase("Assets/Prefab/Player.prefab")]
[TestCase("Assets/Prefab/TestRobot.prefab")]
public void GunPrefabContainsOnlyApprovedSerializedMigration(string path)
{
    string yaml = File.ReadAllText(path);
    Assert.That(yaml, Does.Not.Contain("faceCount:"));
    Assert.That(yaml, Does.Not.Contain("reloadDropDistance:"));
    Assert.That(yaml, Does.Contain("eventBudgetPerActivation: 32"));
}

[Test]
public void ProjectileResourcesAndBuildersDoNotReferenceReporter()
{
    string[] paths =
    {
        "Assets/Prefab/Projectiles/BasicRevolverBullet.prefab",
        "Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs",
        "Assets/Scripts/Editor/ProjectileDefinitionPrototypeBuilder.cs"
    };
    foreach (string path in paths)
        Assert.That(File.ReadAllText(path), Does.Not.Contain("ProjectileHitReporter"), path);
}
```

资源测试还要断言 Player/TestRobot 的 `shotsPerSecond=2`、`reloadDuration=2`、`holdDistance=0.85`、`holdHeight=0.72` 和所有既有对象引用保持不变。

- [ ] **Step 2: 运行资源测试确认红灯**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverCoreAssetMigrationTests;DiceRevolver.Tests.ProjectileDefinitionAssetTests;DiceRevolver.Tests.TestRobotAssetTests" -testResults .\Logs\assets-red.xml -logFile .\Logs\assets-red.log
```

Expected: FAIL，旧字段和 Reporter 仍在资源中。

- [ ] **Step 3: 只对三个 Prefab 做限定 YAML 修改**

Player 与 TestRobot Gun 段：

```yaml
  driveWeaponPose: 1 # TestRobot 保持原值 0；不得统一改写
  shotsPerSecond: 2
  reloadDuration: 2
  automaticReloadWhenEmpty: 1
  allowManualReload: 1
  eventBudgetPerActivation: 32
  reloadBlinkSpeed: 8
```

逐个删除 `faceCount: 6` 和 `reloadDropDistance: 0.22`。BasicRevolverBullet 只删除脚本 GUID `bc5c763f8ba93ae41bb8ee166d047733` 的 MonoBehaviour 块及 GameObject `m_Component` 引用，不重存整个 Prefab。

- [ ] **Step 4: 更新三个 Builder**

删除所有 `AddComponent<ProjectileHitReporter>()` 和相关存在性检查。Gun 创建逻辑写入：

```csharp
serializedGun.FindProperty("eventBudgetPerActivation").intValue =
    DiceFaceActivation.DefaultEventBudget;
```

删除对 `faceCount`、`reloadDropDistance` 的写入；所有六面循环使用 `DiceRevolverRules.FaceCount`。不要运行会重建 Player 或场景的菜单命令。

随后删除 `ProjectileHitReporter.cs` 与其 `.meta`，并把旧资产/集成测试中 `GetComponent<ProjectileHitReporter>()` 的断言改成基于 `Projectile.Hit` 和“Reporter 字符串/GUID 不存在”的契约。

- [ ] **Step 5: 运行资源和 Inspector 聚焦测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverCoreAssetMigrationTests;DiceRevolver.Tests.ProjectileDefinitionAssetTests;DiceRevolver.Tests.TestRobotAssetTests;DiceRevolver.Tests.CombatInspectorLocalizationTests" -testResults .\Logs\assets-green.xml -logFile .\Logs\assets-green.log
```

Expected: PASS；资源中不存在 Reporter GUID，Player/TestRobot 预算均为 32。

- [ ] **Step 6: 用路径限定 diff 审核 Prefab**

```powershell
git diff -- Assets/Prefab/Player.prefab Assets/Prefab/TestRobot.prefab Assets/Prefab/Projectiles/BasicRevolverBullet.prefab
rg -n "faceCount|reloadDropDistance|ProjectileHitReporter|bc5c763f8ba93ae41bb8ee166d047733" Assets/Scripts Assets/Prefab Assets/Tests --glob '!*.meta'
```

Expected: Prefab diff 只含批准字段/组件变化；`rg` 无生产或资源命中，测试只允许出现在“确认类型不存在”的字符串断言中。

- [ ] **Step 7: 提交资源迁移**

```powershell
git add -- Assets/Prefab/Player.prefab Assets/Prefab/TestRobot.prefab Assets/Prefab/Projectiles/BasicRevolverBullet.prefab Assets/Scripts/Prototype/ProjectileHitReporter.cs Assets/Scripts/Prototype/ProjectileHitReporter.cs.meta Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs Assets/Scripts/Editor/ProjectileDefinitionPrototypeBuilder.cs Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs Assets/Tests/EditMode/TestRobotAssetTests.cs Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs.meta
git commit -m "迁移左轮与弹丸资源接线"
```

### Task 8: 清理旧测试耦合并执行完整回归

**Files:**
- Modify: `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`
- Modify: `.project-context/project/PROJECT.md`
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/HANDOFF.md`

**Interfaces:**
- Consumes: 全部新公开模块和资源契约。
- Produces: 无旧私有管线反射、完整回归证据、可跨设备恢复的完成上下文。

- [ ] **Step 1: 扫描旧实现与测试耦合**

```powershell
rg -n "DiceChamber|ProjectileHitReporter|SpawnConfiguredProjectile|CreateEventContext|SpawnActivationProjectile|BridgeProjectileHit|eventTimeScheduler|faceCount|reloadDropDistance" Assets/Scripts Assets/Tests Assets/Prefab --glob '!*.meta'
```

Expected: `DiceChamber`、Reporter、旧 Gun 方法/字段和旧序列化字段没有生产引用；`SpawnActivationProjectile` 只允许作为 Gun 当前私有 Unity 适配方法存在，测试不得反射它。

- [ ] **Step 2: 运行分层聚焦测试**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.DiceRevolverRulesTests;DiceRevolver.Tests.DiceRevolverRuntimeTests;DiceRevolver.Tests.DiceFaceActivationTests;DiceRevolver.Tests.DiceShotPipelineTests;DiceRevolver.Tests.ProjectileCollisionTests;DiceRevolver.Tests.DiceRevolverGunIntegrationTests;DiceRevolver.Tests.DiceRevolverCoreAssetMigrationTests" -testResults .\Logs\core-refactor-focused.xml -logFile .\Logs\core-refactor-focused.log
Select-String -LiteralPath .\Logs\core-refactor-focused.xml -Pattern '<test-run '
```

Expected: 全部 PASS、`failed="0"`、`skipped="0"`。

- [ ] **Step 3: 运行完整 EditMode 回归**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults .\Logs\core-refactor-full.xml -logFile .\Logs\core-refactor-full.log
Select-String -LiteralPath .\Logs\core-refactor-full.xml -Pattern '<test-run '
```

Expected: 除具名已知的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities` Ground Y 失败外不得有其他失败，`skipped="0"`，总数不少于基线 `139`；结果必须如实记录为 `[failed]`，不得暗示全绿。

- [ ] **Step 4: 更新项目上下文为真实结果**

在 PROJECT 中把旧数据流：

```text
DiceChamber 抽面 -> DiceFaceLoadout -> ...
ProjectileHitReporter -> Gun -> OnHit
```

更新为：

```text
DiceRevolverRuntime 抽面/换弹 -> DiceShotPipeline 激活四槽位 -> DiceRevolverGun Unity 适配
Projectile 命中广播 -> DiceShotPipeline OnHit -> Projectile 直接伤害
```

STATE 记录每条实际测试命令和数量；仅在完整回归没有新增失败、唯一失败仍为具名 Ground Y 豁免项时改为 `completed`，同时保持该回归为 `[failed]`。HANDOFF 指向雷电构筑待办，并保留“PlayMode 手感尚需人工验收”。

- [ ] **Step 5: 验证上下文、diff 和工作区**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
git diff --check
git status --short
```

Expected: `[context:ok]`；`git diff --check` 无错误；状态只含本任务上下文更新。

- [ ] **Step 6: 提交完成上下文**

```powershell
git add -- .project-context/project/PROJECT.md .project-context/project/STATUS.md .project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/STATE.md .project-context/project/workstreams/2026-08-20-dice-revolver-core-refactor/HANDOFF.md
git commit -m "完成左轮核心底层重构"
```

- [ ] **Step 7: 进行人工 PlayMode 验收并如实记录**

用 Unity `6000.3.10f1` 打开 `Assets/Scenes/TopDownShooterPrototype.unity`，确认玩家与机器人均能：六发不重复射击、空膛换弹、手动换弹、DoubleTap 延迟、BlastRound 直击加爆炸、LoadedFour 下一发固定为 4。若当前会话不能人工操作，将验证标为 `[not-run]`，不得据此阻止已通过自动化的结构重构完成，也不得声称手感已经验收。
