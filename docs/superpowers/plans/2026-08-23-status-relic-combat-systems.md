# 状态·遗物·收尾者·特斯拉·呼应协同 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现五组战斗能力：敌人负面效果框架（点燃 DoT + 有限生命/死亡）、遗物框架（出千）、收尾者重做（最后出现+穿甲弹）、特斯拉开火时增伤、呼应协同相邻触发——支撑用户预想 Build 循环。

**Architecture:** 敌人层（EnemyHealth/EnemyStatusHost）→ 通用模块（施加状态/状态条件）→ 遗物层（RelicDefinition/RelicRuntime）→ 骰面机制层（抽牌评估扩展、收尾者/特斯拉/呼应协同规则重做）→ 回归门禁。

**Tech Stack:** Unity 6000.3.10f1 / C# / Unity Test Framework EditMode / MSBuild 编译门禁。

**Spec:** `docs/superpowers/specs/2026-08-23-status-relic-combat-systems-design.md`

## Global Constraints

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、DiceRevolverGun 调参（受保护资产；迁移后 SHA256 门禁一致）。
- 规则资产迁移幂等（重复执行结果稳定）；既有受保护资产哈希不变。
- 每个任务结束：MSBuild 编译三程序集 exit 0 + 聚焦 EditMode 测试绿（Unity 测试经用户 Test Runner 或编辑器关闭后批处理）。
- 新枚举/信号成员追加不破坏既有序列化数据。

---

### Task 1: 敌人有限生命与死亡

**Files:**
- Create: `Assets/Scripts/Prototype/EnemyHealth.cs`
- Modify: `Assets/Scripts/Prototype/TargetDummy.cs`
- Test: `Assets/Tests/EditMode/EnemyHealthTests.cs`

**Interfaces:**
- Consumes: `IDamageReceiver`、`DamageInfo`。
- Produces: `EnemyHealth`（`int MaxHealth`、`int CurrentHealth`、`bool IsDead`、`event Action<EnemyHealth> Died`、`event Action<DamageInfo> DamageReceived`、`void ReceiveDamage(DamageInfo)`、`void ResetHealth()`）。

- [ ] **Step 1: 写失败测试**

```csharp
public sealed class EnemyHealthTests
{
    [Test]
    public void DamageReducesHealthAndDeathFiresOnce()
    {
        GameObject go = new GameObject("Enemy");
        EnemyHealth health = go.AddComponent<EnemyHealth>();
        health.MaxHealth = 10;
        int deaths = 0;
        health.Died += _ => deaths++;
        health.ReceiveDamage(new DamageInfo(6f, Vector3.zero, null));
        Assert.That(health.CurrentHealth, Is.EqualTo(4));
        Assert.That(health.IsDead, Is.False);
        health.ReceiveDamage(new DamageInfo(5f, Vector3.zero, null));
        Assert.That(health.IsDead, Is.True);
        health.ReceiveDamage(new DamageInfo(1f, Vector3.zero, null));
        Assert.That(deaths, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(go);
    }
}
```

（`DamageInfo` 构造签名以现有 `DamageInfo.cs` 为准，测试前通读该文件。）

- [ ] **Step 2: 运行确认失败**（`EnemyHealth` 不存在 → 编译失败）
- [ ] **Step 3: 实现**

```csharp
using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, InspectorName("最大生命")] private int maxHealth = 20;
        [SerializeField, InspectorName("死亡后禁用")] private bool disableOnDeath = true;

        public int MaxHealth
        {
            get => maxHealth;
            set { maxHealth = Mathf.Max(1, value); if (CurrentHealth > maxHealth) CurrentHealth = maxHealth; }
        }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public event Action<EnemyHealth> Died;
        public event Action<DamageInfo> DamageReceived;

        private void Awake() => CurrentHealth = maxHealth;

        public void ReceiveDamage(DamageInfo damage)
        {
            if (IsDead || damage == null || damage.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.CeilToInt(damage.Amount));
            DamageReceived?.Invoke(damage);
            if (CurrentHealth == 0)
            {
                IsDead = true;
                Died?.Invoke(this);
                if (disableOnDeath)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void ResetHealth()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            gameObject.SetActive(true);
        }
    }
}
```

（`DamageInfo.Amount` 等成员名以 `DamageInfo.cs` 实际为准。）

`TargetDummy.cs`：改为组合 `EnemyHealth`（保留 `HitCount`/`LastDamage` 兼容；死亡后自动 `ResetHealth()` 继续可打）：

```csharp
public void ReceiveDamage(DamageInfo damage)
{
    LastDamage = damage;
    HitCount++;
    DamageReceived?.Invoke(damage);
    if (health == null)
    {
        health = GetComponent<EnemyHealth>() ?? gameObject.AddComponent<EnemyHealth>();
    }
    health.ReceiveDamage(damage);
}
```

（`health` 为 `[SerializeField]` 或 `GetComponent` 懒加载；死亡重置：订阅 `health.Died` → `health.ResetHealth()` 或 TargetDummy 场景脚本处理，以最小改动为准。）

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/EnemyHealth.cs Assets/Scripts/Prototype/TargetDummy.cs Assets/Tests/EditMode/EnemyHealthTests.cs
git commit -m "feat: 敌人有限生命与死亡"
```

---

### Task 2: 负面效果状态框架

**Files:**
- Create: `Assets/Scripts/Prototype/EnemyStatusDefinition.cs`
- Create: `Assets/Scripts/Prototype/EnemyStatusHost.cs`
- Test: `Assets/Tests/EditMode/EnemyStatusHostTests.cs`

**Interfaces:**
- Consumes: `EnemyHealth`（Task 1）。
- Produces: `EnemyStatusDefinition`（SO：`string StatusId`、`string DisplayName`、`float DurationSeconds`、`float DamagePerSecond`、`int MaxStacks`、`Color VisualColor`）；`EnemyStatusHost`（`void ApplyStatus(EnemyStatusDefinition)`、`bool HasStatus(string id)`、`int GetStacks(string id)`、`event Action<EnemyStatusHost, EnemyStatusDefinition> StatusApplied`）。

- [ ] **Step 1: 写失败测试**

```csharp
[Test]
public void StatusTicksDamagePerSecondAndExpires()
{
    GameObject go = new GameObject("Host");
    EnemyHealth health = go.AddComponent<EnemyHealth>();
    health.MaxHealth = 100;
    EnemyStatusHost host = go.AddComponent<EnemyStatusHost>();
    EnemyStatusDefinition ignite = ScriptableObject.CreateInstance<EnemyStatusDefinition>();
    ignite.StatusId = "ignite";
    ignite.DurationSeconds = 2f;
    ignite.DamagePerSecond = 5f;
    ignite.MaxStacks = 1;
    host.ApplyStatus(ignite);
    Assert.That(host.HasStatus("ignite"), Is.True);
    host.TickForTesting(1f); // 或直接调用内部 DoT 结算（测试用公开/内部方法）
    Assert.That(health.CurrentHealth, Is.EqualTo(95));
    host.TickForTesting(1f);
    Assert.That(host.HasStatus("ignite"), Is.False); // 到期移除
    UnityEngine.Object.DestroyImmediate(go);
}
```

（若 `Update` 驱动不便测试，暴露 `internal void Tick(float delta)` 供测试与 Update 共用。）

- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`EnemyStatusDefinition.cs`：ScriptableObject（上述字段 + `CreateAssetMenu`）。

`EnemyStatusHost.cs` 核心：

```csharp
public sealed class EnemyStatusHost : MonoBehaviour
{
    private readonly List<ActiveStatus> active = new();
    private EnemyHealth health;
    public event Action<EnemyStatusHost, EnemyStatusDefinition> StatusApplied;

    public void ApplyStatus(EnemyStatusDefinition definition)
    {
        if (definition == null || health == null) return;
        ActiveStatus existing = Find(definition.StatusId);
        if (existing != null)
        {
            if (definition.MaxStacks > 1 && existing.Stacks < definition.MaxStacks)
            {
                existing.Stacks++;
            }
            existing.RemainingSeconds = definition.DurationSeconds; // 刷新
        }
        else
        {
            active.Add(new ActiveStatus(definition));
        }
        StatusApplied?.Invoke(this, definition);
    }

    public bool HasStatus(string id) => Find(id) != null;

    private void Update()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveStatus status = active[i];
            status.RemainingSeconds -= Time.deltaTime;
            if (status.Definition.DamagePerSecond > 0f)
            {
                health.ReceiveDamage(new DamageInfo(
                    status.Definition.DamagePerSecond * status.Stacks * Time.deltaTime,
                    transform.position, null));
            }
            if (status.RemainingSeconds <= 0f)
            {
                active.RemoveAt(i);
            }
        }
    }

    private sealed class ActiveStatus
    {
        public ActiveStatus(EnemyStatusDefinition definition) { Definition = definition; RemainingSeconds = definition.DurationSeconds; Stacks = 1; }
        public EnemyStatusDefinition Definition { get; }
        public float RemainingSeconds;
        public int Stacks;
    }
}
```

（DoT 伤害经 `EnemyHealth.ReceiveDamage` → 触发飘字；`DamageInfo` 构造按实际签名。）

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/EnemyStatusDefinition.cs Assets/Scripts/Prototype/EnemyStatusHost.cs Assets/Tests/EditMode/EnemyStatusHostTests.cs
git commit -m "feat: 敌人负面效果状态框架"
```

---

### Task 3: 通用状态模块 + 点燃资产

**Files:**
- Create: `Assets/Scripts/Prototype/ApplyEnemyStatusResultModule.cs`
- Create: `Assets/Scripts/Prototype/HasEnemyStatusConditionModule.cs`
- Create（资产）: `Assets/Resources/DiceFacePrototype/Statuses/Ignite.asset`
- Test: `Assets/Tests/EditMode/EnemyStatusModuleTests.cs`

**Interfaces:**
- Consumes: `EnemyStatusHost`、`EnemyStatusDefinition`（Task 2）、`EventEvaluationContext`。
- Produces: `ApplyEnemyStatusResultModule`（字段 `EnemyStatusDefinition statusDefinition`）；`HasEnemyStatusConditionModule`（字段 `EnemyStatusDefinition statusDefinition`，判定命中目标 `HitCollider` 上的 `EnemyStatusHost`）。

- [ ] **Step 1: 写失败测试**：命中对象带 `EnemyStatusHost` → 施加状态 → `HasStatus` 为真；无 Host 的目标 → 条件失败/结果跳过。
- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`ApplyEnemyStatusResultModule.Execute`：`context.Signal.HitCollider?.GetComponentInParent<EnemyStatusHost>()` → `host.ApplyStatus(statusDefinition)` → `Success`；无 Host → `Skipped`。

`HasEnemyStatusConditionModule.Evaluate`：`context.Signal.HitCollider?.GetComponentInParent<EnemyStatusHost>()?.HasStatus(statusDefinition.StatusId)`。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/ApplyEnemyStatusResultModule.cs Assets/Scripts/Prototype/HasEnemyStatusConditionModule.cs Assets/Resources/DiceFacePrototype/Statuses/Ignite.asset Assets/Tests/EditMode/EnemyStatusModuleTests.cs
git commit -m "feat: 通用状态施加/条件模块与点燃资产"
```

---

### Task 4: 遗物框架 + 出千 + 被动面守卫

**Files:**
- Create: `Assets/Scripts/Prototype/RelicDefinition.cs`
- Create: `Assets/Scripts/Prototype/RelicRuntime.cs`
- Create: `Assets/Scripts/Prototype/LoadedFirstFaceRelicDefinition.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`（`SetFirstDrawForce` + `TryRefillAndForceNextFace` 被动面守卫）
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`（RelicRuntime 接线 + 换弹完成钩子）
- Test: `Assets/Tests/EditMode/RelicTests.cs` + `DiceRevolverRuntimeTests.cs`

**Interfaces:**
- Consumes: `DiceRevolverRuntime`（池级排除）、`DiceFaceLoadout.GetPassiveFaceSet`。
- Produces: `RelicContext`（`Runtime`、`IReadOnlyList<int> PassiveFaces`、`int FaceCount`）；`RelicDefinition.ApplyAtRoundStart(RelicContext)`；`RelicRuntime.ApplyRoundStart()`；`LoadedFirstFaceRelicDefinition`（`int Face`）。

- [ ] **Step 1: 写失败测试**

```csharp
[Test]
public void FirstDrawRelicForcesFaceAtRoundStartUnlessPassive()
{
    DiceRevolverRuntime runtime = new(5f, 2f, true, true);
    runtime.RebuildActiveFaces(new[] { 3 }); // 面3被动
    LoadedFirstFaceRelicDefinition relic = ScriptableObject.CreateInstance<LoadedFirstFaceRelicDefinition>();
    relic.Face = 4;
    relic.ApplyAtRoundStart(new RelicContext(runtime, new[] { 3 }, 6));
    Assert.That(runtime.TryBeginShot(0f).Face, Is.EqualTo(4));
}

[Test]
public void FirstDrawRelicIgnoresPassiveTargetFace()
{
    DiceRevolverRuntime runtime = new(5f, 2f, true, true);
    runtime.RebuildActiveFaces(new[] { 4 });
    LoadedFirstFaceRelicDefinition relic = ScriptableObject.CreateInstance<LoadedFirstFaceRelicDefinition>();
    relic.Face = 4;
    relic.ApplyAtRoundStart(new RelicContext(runtime, new[] { 4 }, 6));
    // 面4被动：不强制，首抽为随机活动面
    DiceRevolverDrawResult result = runtime.TryBeginShot(0f);
    Assert.That(result.Face, Is.Not.EqualTo(4));
}

[Test]
public void ForceFaceRejectsPassiveFaces()
{
    DiceRevolverRuntime runtime = new(5f, 2f, true, true);
    runtime.RebuildActiveFaces(new[] { 4 });
    Assert.That(runtime.TryRefillAndForceNextFace(4), Is.False);
}
```

- [ ] **Step 2: 运行确认失败**（`SetFirstDrawForce`/`RelicContext` 不存在；`TryRefillAndForceNextFace(4)` 目前返回 true）
- [ ] **Step 3: 实现**

`DiceRevolverRuntime`：

```csharp
public bool SetFirstDrawForce(int face)
{
    if (face < 1 || face > DiceRevolverRules.FaceCount || passiveFaces.Contains(face) ||
        !remainingFaces.Contains(face))
    {
        return false;
    }
    forcedNextFace = face;
    return true;
}
```

`TryRefillAndForceNextFace` 增加被动面守卫：`if (... || passiveFaces.Contains(face)) return false;`

`RelicDefinition.cs` / `RelicContext.cs` / `LoadedFirstFaceRelicDefinition.cs`：见 Spec 第 2 节；`LoadedFirstFaceRelicDefinition.ApplyAtRoundStart`：`if (!context.PassiveFaces.Contains(Face)) context.Runtime.SetFirstDrawForce(Face);`

`RelicRuntime.cs`：持有 `List<RelicDefinition> relics`；`ApplyRoundStart(RelicContext)` 遍历调用。

`DiceRevolverGun`：新增 `[SerializeField] List<RelicDefinition> relics` + `RelicRuntime relicRuntime`（或直接在 Gun 内遍历）；换弹完成（`update.ReloadCompleted` 分支）后调用：

```csharp
ApplyRelicsAtRoundStart();
// 内部：relicRuntime?.ApplyRoundStart(new RelicContext(runtime, loadout.GetPassiveFaceSet(), DiceRevolverRules.FaceCount));
```

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/RelicDefinition.cs Assets/Scripts/Prototype/RelicRuntime.cs Assets/Scripts/Prototype/LoadedFirstFaceRelicDefinition.cs Assets/Scripts/Prototype/DiceRevolverRuntime.cs Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Tests/EditMode/RelicTests.cs Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs
git commit -m "feat: 遗物框架与出千遗物，修复被动面强制守卫"
```

---

### Task 5: 抽牌评估覆盖所有面

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs`
- Test: `Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs`（或新建 `DrawEvaluationTests.cs`）

**Interfaces:**
- Consumes: 无新签名。
- Produces: `FilterDrawCandidates` 的 DrawCandidate 信号评估改为遍历**所有面**的 base 槽规则（普通面也参与）；`ExecutePassive` 不变（仍只遍历被动面）。

- [ ] **Step 1: 写失败测试**

```csharp
[Test]
public void NormalFaceBaseRuleParticipatesInDrawPriority()
{
    // 面 2（普通面 snapshot，IsPassiveFace=false）基础槽装收尾者规则（DrawCandidate 触发 + SetDrawPriority(1)）
    // FilterDrawCandidates({1,2,3}, {1,2,3}, null) → 面2优先级高 → candidates 不含2（{1,3}）
}
```

- [ ] **Step 2: 运行确认失败**（当前普通面规则不参与 → candidates 含 2）
- [ ] **Step 3: 实现**

`DiceEventRuleRuntimeSet.FilterDrawCandidates` 内对候选面发 DrawCandidate 信号时，`ExecutePassive(signal, services)` 改为"评估所有面的 base 槽规则"（新增私有 `ExecuteDrawEvaluation(signal, services)`：遍历全部面，取 `runtimes[faceIndex, (int)DiceFaceSlotType.Base]`，不再要求 `passiveFaces[faceIndex]`）。`ExecutePassive` 保持原语义（被动信号监听）。

- [ ] **Step 4: 运行测试确认通过**（既有被动面抽牌测试应仍绿——被动面规则同时被两种路径评估，行为一致）
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs Assets/Tests/EditMode/
git commit -m "feat: 抽牌评估覆盖所有面基础规则"
```

---

### Task 6: 收尾者新规则 + 穿甲弹资产

**Files:**
- Modify（规则资产）: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/FinisherRule.asset`
- Create（资产）: `Assets/Resources/DiceFacePrototype/Projectiles/ArmorPiercingBullet.asset`（或复用现有穿甲定义）
- Modify: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`（收尾者规则构建：双触发 + 穿甲弹生成）
- Test: `Assets/Tests/EditMode/EventRuleLightningMigrationTests.cs`

**Interfaces:**
- Consumes: `SignalTypeTriggerModule`（掩码）、`SignalTypeConditionModule`、`SetDrawPriorityResultModule`、`SpawnProjectileResultModule`、`ProjectileDefinition`。
- Produces: 收尾者规则 = 触发器(DrawCandidate|Base) + 结果1[SetDrawPriority | 条件 DrawCandidate] + 结果2[SpawnProjectile(穿甲弹 primary) | 条件 Base]。

- [ ] **Step 1: 写失败测试**：收尾者规则（`FinisherRule.asset`）——`DrawCandidate` 信号 → `PassiveEventRuleServices.HighestDrawPriority == 1`；`Base` 信号 → 请求生成穿甲弹（`ProjectileRequests` 含穿甲弹定义，`IsPrimary == true`）。
- [ ] **Step 2: 运行确认失败**（当前收尾者只有 SetDrawPriority）
- [ ] **Step 3: 实现**

`LightningBuildPrototypeBuilder.MigrateFinisher`：构建新结构（触发器掩码 DrawCandidate|Base；结果 1 SetDrawPriority+SignalType(DrawCandidate) 条件；结果 2 SpawnProjectile(穿甲弹)+SignalType(Base) 条件），幂等；创建穿甲弹 `ProjectileDefinition` 资产（长型视觉引用一个既有弹丸 Prefab 或新增；穿透参数高于基础弹丸）。

- [ ] **Step 4: 运行迁移 + 测试确认**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs Assets/Resources/DiceFacePrototype/Projectiles/ Assets/Resources/DiceFacePrototype/EventRules/Lightning/FinisherRule.asset Assets/Tests/EditMode/
git commit -m "feat: 收尾者重做为最后出现+穿甲弹基础事件"
```

---

### Task 7: 特斯拉开火时增伤

**Files:**
- Create: `Assets/Scripts/Prototype/RoundProjectileStatistic.cs`
- Create: `Assets/Scripts/Prototype/ScaleActivationDamageFromStatisticResultModule.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`（统计递增/重置接线）
- Modify: `Assets/Scripts/Prototype/DiceFaceActivation.cs`（`DamageMultiplier`）
- Modify（规则资产）: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset`
- Modify: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- Test: `Assets/Tests/EditMode/EventRuleLightningMigrationTests.cs`

**Interfaces:**
- Consumes: `ProjectileHandle`、`ProjectileTagDefinition`、`DiceFaceActivation`。
- Produces: `RoundProjectileStatistic`（`void Increment(ProjectileTagDefinition tag)`、`int Count(ProjectileTagDefinition tag)`、`void Reset()`）；`ScaleActivationDamageFromStatisticResultModule`（`ProjectileTagDefinition statisticTag`、`float damagePerCount`；执行时 `activation.DamageMultiplier *= 1f + count * damagePerCount`）；`DiceFaceActivation.DamageMultiplier`（默认 1）。

- [ ] **Step 1: 写失败测试**：统计递增/计数/重置；`ScaleActivationDamageFromStatisticResultModule` 执行后 `activation.DamageMultiplier` 变化；激活生成弹丸伤害被倍率放大（`SpawnProjectileResultModule` 请求的弹丸伤害 × 倍率）。
- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`RoundProjectileStatistic`：按 `ProjectileTagDefinition` 字典计数；`Reset()` 清空。

`DiceFaceActivation`：新增 `public float DamageMultiplier { get; set; } = 1f;`；弹丸生成路径（`SpawnProjectileResultModule` 或管线生成请求）应用 `activation.DamageMultiplier` 到弹丸伤害。

`DiceRevolverGun`：`NotifyProjectileSpawnedPassives` 中按弹丸标签递增统计；换弹完成时 `statistic.Reset()`。

`TeslaRule` 重构（`LightningBuildPrototypeBuilder.MigrateTesla`）：触发器=OnFire；结果=`ScaleActivationDamageFromStatisticResultModule`（statisticTag=Lightning）+ 局部条件 `SignalTypeCondition(OnFire)`；幂等迁移旧结构。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/RoundProjectileStatistic.cs Assets/Scripts/Prototype/ScaleActivationDamageFromStatisticResultModule.cs Assets/Scripts/Prototype/DiceFaceActivation.cs Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset Assets/Tests/EditMode/
git commit -m "feat: 特斯拉开火时按雷电弹幕统计增伤"
```

---

### Task 8: 呼应协同相邻触发

**Files:**
- Create: `Assets/Scripts/Prototype/DiceFaceAdjacency.cs`
- Create: `Assets/Scripts/Prototype/TriggerAdjacentFacesResultModule.cs`
- Modify: `Assets/Scripts/Prototype/EventRuleTypes.cs`（`EventSignalType.EnemyStatusApplied` + `EventSignalMask.EnemyStatusApplied`）
- Modify: `Assets/Scripts/Prototype/EventSignal.cs`（携带状态目标引用，尾部可选参数保持兼容）
- Modify: `Assets/Scripts/Prototype/EnemyStatusHost.cs`（发出 `EnemyStatusApplied` 信号）
- Modify（规则资产）: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/EchoSynergyRule.asset`
- Modify: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- Test: `Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs`

**Interfaces:**
- Consumes: `EnemyStatusHost.StatusApplied`、`ExecuteBonusShot`。
- Produces: `DiceFaceAdjacency.AdjacentFaces(int face)`（静态表：1→{2,3,4}、2→{1,3,6}、3→{1,2,4,6}、4→{1,3,5,6}、5→{4}、6→{2,3,4}，与构筑 UI 布局 8 向邻接一致）；`TriggerAdjacentFacesResultModule`（`int maximumTriggers`、`string counterKey`；对每个相邻面请求 `RequestBonusActivation(face, ...)` 完整激活）。

- [ ] **Step 1: 写失败测试**：`DiceFaceAdjacency.AdjacentFaces(3) == {1,2,4,6}`；`TriggerAdjacentFacesResultModule` 对相邻面逐面请求奖励激活（捕获服务记录请求面集合），不消耗；次数上限。
- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`DiceFaceAdjacency`：静态字典硬编码（注释引用 `DiceBuildPageUI.FacePositions`）。

`TriggerAdjacentFacesResultModule.Execute`：校验（同 RequestBonusActivationResultModule 模式）→ 对 `DiceFaceAdjacency.AdjacentFaces(context.Signal.EquippedFace)` 每个面调用 `context.Services.RequestBonusActivation(face, ...)`；用 `counterKey` 计数限制每轮次数。

`EventRuleTypes`：`EventSignalMask`/`EventSignalType` 增加 `EnemyStatusApplied`（追加位，不破坏既有值）。

`EventSignal`：尾部追加可选参数（如 `EnemyStatusHost statusTarget = null`），既有调用点不变。

`EnemyStatusHost.ApplyStatus`：施加后广播——由 gun/运行时把状态事件转成 `EnemyStatusApplied` 信号（`DiceEventRuleRuntimeSet.NotifyEnemyStatusApplied(host, definition)` → `ExecutePassive`）。

`EchoSynergyRule` 重构（`MigrateEcho`）：触发器=`EnemyStatusApplied`；规则级条件=`HasEnemyStatusConditionModule`(点燃)；结果=`TriggerAdjacentFacesResultModule`（每轮上限沿用原计数键）；幂等迁移旧结构（移除 SameProjectileType/ProjectileHit 旧结果）。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceFaceAdjacency.cs Assets/Scripts/Prototype/TriggerAdjacentFacesResultModule.cs Assets/Scripts/Prototype/EventRuleTypes.cs Assets/Scripts/Prototype/EventSignal.cs Assets/Scripts/Prototype/EnemyStatusHost.cs Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs Assets/Resources/DiceFacePrototype/EventRules/Lightning/EchoSynergyRule.asset Assets/Tests/EditMode/
git commit -m "feat: 呼应协同相邻骰面触发"
```

---

### Task 9: 全量回归 + 静态门禁 + 上下文同步

**Files:**
- Test: 全 EditMode 套件
- 校验：受保护资产 SHA256；`DiceFaceSlotMask.Passive` 等零引用保持

- [ ] **Step 1: 静态门禁**：受保护 10 文件 SHA256 与基线一致；`DiceFaceSlotType.Passive` 仅迁移工具 legacy 读取；新信号位不破坏序列化（`EventSignalMask` 既有位值不变）。
- [ ] **Step 2: 全量 EditMode 回归**（隔离副本或用户 Test Runner）：全部通过，唯一允许失败为既有 Ground 豁免项。
- [ ] **Step 3: 上下文同步**：更新 `2026-08-23-status-relic-combat-systems` STATE/HANDOFF、STATUS.md；`check.ps1` 返回 `[context:ok]`；提交。

---

## Self-Review 记录（计划作者自查）

- **规格覆盖**：D1→T2/T3，D2→T1，D3→T5/T6，D4/D5→T8，D6→T4；被动面守卫（规格第 2 节）→T4；统计（第 4 节）→T7；信号（第 5 节）→T8；受保护资产→T9。无遗漏。
- **占位符扫描**：无 TBD/TODO；涉及未通读文件（`DamageInfo`、`EventSignal` 构造、`DiceFaceActivation`）的步骤均注明"以实际签名为准"，实现时先读。
- **类型一致性**：`EnemyHealth`/`EnemyStatusHost`/`EnemyStatusDefinition`/`RelicContext`/`SetFirstDrawForce`/`RoundProjectileStatistic`/`DiceFaceAdjacency`/`TriggerAdjacentFacesResultModule` 在跨任务引用处签名一致；相邻表与规格一致。
