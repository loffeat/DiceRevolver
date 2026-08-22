# 被动事件迁移为被动型基础事件 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把被动事件从"骰面第五被动槽"迁移为"被动型基础事件"：词条级 `isPassiveBase` 标志 + 基础槽；被动面永不入抽池（池级排除），每轮可抽面数 = 6 − N；被动规则按信号随时生效。

**Architecture:** 数据模型（删被动槽、词条标志、快照 IsPassiveFace）→ 运行时（DiceRevolverRuntime 池级排除 + DiceEventRuleRuntimeSet 被动绑定改为被动面 base 槽）→ 编辑器/UI（4 分类、被动徽标）→ 迁移工具（3 词条 + 3 规则资产）→ 回归门禁。

**Tech Stack:** Unity 6000.3.10f1 / C# / Unity Test Framework EditMode。

**Spec:** `docs/superpowers/specs/2026-08-23-passive-base-events-design.md`（计划从规格论证；执行者需同时阅读两者）

## Global Constraints

- 不修改 Player、TestRobot、TargetDummy Prefab、AimRoot、sorting layer、DiceRevolverGun 调参（受保护资产；迁移后 SHA256 门禁必须一致）。
- 保留抽象 `BulletEventEffect`/`PassiveEventEffect`、hidden legacy 字段/read fallback 与三个 spawn 资产。
- `DiceFaceSlotType.Passive` 枚举成员为序列化兼容保留定义，但**禁止新增任何脚本引用**（静态门禁：Assets 零引用）。
- 被动面数量不限（0–6）；全被动仅警告不报错。
- 构筑改变即重建活动池（重置本轮进度）。
- 每个任务结束运行 EditMode 聚焦测试（见各任务 Run 步骤），全部绿后才进入下一任务。

---

### Task 1: 数据模型——移除被动槽

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceFaceSlotType.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- Modify: `Assets/Scripts/Prototype/EventRuleTypes.cs`
- Test: `Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs`（改造为被动面语义）

**Interfaces:**
- Consumes: `DiceFaceConfigurationSnapshot` 现有 5 槽构造；`DiceFaceEntry.SlotType`。
- Produces: `DiceFaceEntry.IsPassiveBase`（bool，新增）；`DiceFaceConfigurationSnapshot.IsPassiveFace`（bool，新增，由 baseEntry.IsPassiveBase 推导）；`DiceFaceSlotMask` 不再含 `Passive` 位。

- [ ] **Step 1: 写失败测试（词条标志 + 快照推导）**

在 `DiceFacePassiveSlotTests.cs`（先通读该文件，把被动槽断言改为新语义）新增：

```csharp
[Test]
public void SnapshotIsPassiveFaceFollowsBaseEntryFlag()
{
    DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
    SerializedObject serialized = new SerializedObject(entry);
    serialized.FindProperty("isPassiveBase").boolValue = true;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    var snapshot = new DiceFaceConfigurationSnapshot(entry, null, null, null);
    Assert.That(snapshot.IsPassiveFace, Is.True);
    var normal = new DiceFaceConfigurationSnapshot(
        ScriptableObject.CreateInstance<DiceFaceEntry>(), null, null, null);
    Assert.That(normal.IsPassiveFace, Is.False);
}
```

（`DiceFaceEntry` 需要在无 Rule 时可创建；若 CreateInstance 报错则改用既有资产创建方式。测试引用 `isPassiveBase` 字段路径时以 Task 1 Step 3 的实现为准。）

- [ ] **Step 2: 运行确认失败**

Run: Unity EditMode 聚焦 `DiceFacePassiveSlotTests`
Expected: 编译失败（`IsPassiveFace` 不存在）或断言失败。

- [ ] **Step 3: 实现数据模型**

`DiceFaceEntry.cs`：字段区新增

```csharp
[SerializeField, InspectorName("被动型基础")] private bool isPassiveBase;
```
属性区新增 `public bool IsPassiveBase => isPassiveBase;`

`DiceFaceSlotType.cs`：`ToChineseLabel` 的 `Passive => "被动"` 分支删除（成员保留定义）。

`EventRuleTypes.cs`：`DiceFaceSlotMask` 删除 `[InspectorName("被动")] Passive = 1 << 4,`，`All = Active | Passive` 改为 `All = Active`。

`DiceFaceConfiguration.cs`：
- 删除字段 `[SerializeField, InspectorName("被动事件")] private DiceFaceEntry passiveEntry;`
- `GetEntry` 删除 `DiceFaceSlotType.Passive => passiveEntry` 分支
- `SetEntry` 删除 `case DiceFaceSlotType.Passive: passiveEntry = entry; break;`
- `CreateSnapshot` 的 `passiveEntry` 实参删除（含 5 参重载删除或改为 4 参 + 保留 5 参签名但忽略；以最小改动为准）
- 快照结构体：删除 `passiveEntry` 字段与 5 参构造的对应参数；删除 `GetPassiveEffect()`；删除 `FromEntry` 的 `DiceFaceSlotType.Passive` 分支；`FirstEntry` 删除 `passiveEntry` 兜底；`HasAnyEffect` 删除 Passive 相关项；`MergeActiveOverlay` 删除 `passiveEntry` 参数
- 新增 `public bool IsPassiveFace => baseEntry != null && baseEntry.IsPassiveBase;`

- [ ] **Step 4: 运行测试确认通过**

Run: Unity EditMode 聚焦 `DiceFacePassiveSlotTests`（含既有用例改造）
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceFaceSlotType.cs Assets/Scripts/Prototype/DiceFaceEntry.cs Assets/Scripts/Prototype/DiceFaceConfiguration.cs Assets/Scripts/Prototype/EventRuleTypes.cs Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs
git commit -m "feat: 移除被动槽数据模型，词条级被动基础标志"
```

---

### Task 2: Loadout 被动面集合与装备校验

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`（Equip 校验）
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Test: `Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs`

**Interfaces:**
- Consumes: `DiceFaceConfigurationSnapshot.IsPassiveFace`（Task 1）。
- Produces: `DiceFaceLoadout.GetPassiveFaceSet()` → `IReadOnlyList<int>`（被动面号升序）。

- [ ] **Step 1: 写失败测试**

```csharp
[Test]
public void LoadoutRejectsPassiveBaseEntryOutsideBaseSlot()
{
    DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
    SerializedObject serialized = new SerializedObject(entry);
    serialized.FindProperty("isPassiveBase").boolValue = true;
    serialized.FindProperty("slotType").intValue = (int)DiceFaceSlotType.OnFire;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    DiceFaceConfiguration configuration = new DiceFaceConfiguration();
    Assert.That(configuration.Equip(entry), Is.False);
}

[Test]
public void LoadoutCollectsPassiveFacesFromSnapshots()
{
    // 用 EventRuleAssetUtility 或直接构造：面1 基础槽装带被动标志词条，其余空
    // 断言 GetPassiveFaceSet() 返回 {1}
}
```

- [ ] **Step 2: 运行确认失败**

Expected: `Equip` 返回 true（未拒绝）、`GetPassiveFaceSet` 编译失败。

- [ ] **Step 3: 实现**

`DiceFaceConfiguration.Equip` 开头新增：

```csharp
if (entry.IsPassiveBase && entry.SlotType != DiceFaceSlotType.Base)
{
    return false;
}
```

`DiceFaceLoadout` 新增：

```csharp
public IReadOnlyList<int> GetPassiveFaceSet()
{
    List<int> passiveFaces = new();
    for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
    {
        if (GetSnapshot(face).IsPassiveFace)
        {
            passiveFaces.Add(face);
        }
    }
    return passiveFaces.AsReadOnly();
}
```

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceFaceConfiguration.cs Assets/Scripts/Prototype/DiceFaceLoadout.cs Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs
git commit -m "feat: loadout 被动面集合与装备校验"
```

---

### Task 3: 运行时池级排除

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`
- Test: `Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs`（先通读既有池测试，追加用例）

**Interfaces:**
- Consumes: `DiceRevolverRules.FaceCount = 6`。
- Produces: `DiceRevolverRuntime.RebuildActiveFaces(IReadOnlyCollection<int> passiveFaces)`（清空并按非被动面重新填充）；`ActiveFaceCount`（int，= FaceCount − 被动数）。

- [ ] **Step 1: 写失败测试**

在 `DiceRevolverRuntimeTests.cs` 追加：

```csharp
[Test]
public void PassiveFacesAreExcludedFromTheRoundPool()
{
    DiceRevolverRuntime runtime = new DiceRevolverRuntime(2f, 2f, true, true);
    runtime.RebuildActiveFaces(new[] { 2, 5 });
    Assert.That(runtime.RemainingRounds, Is.EqualTo(4));
    Assert.That(runtime.CreateRemainingFacesSnapshot(), Is.EqualTo(new[] { 1, 3, 4, 6 }));
}

[Test]
public void RoundEndsWhenActivePoolIsExhausted()
{
    DiceRevolverRuntime runtime = new DiceRevolverRuntime(2f, 2f, true, true);
    runtime.RebuildActiveFaces(new[] { 2, 5 });
    int fired = 0;
    for (int i = 0; i < 4; i++)
    {
        if (runtime.TryBeginShot(0f + i).Status == DiceRevolverDrawStatus.Fired) { fired++; }
    }
    Assert.That(fired, Is.EqualTo(4));
    Assert.That(runtime.TryBeginShot(5f).Status, Is.EqualTo(DiceRevolverDrawStatus.Empty));
}

[Test]
public void ManualReloadIsAllowedOnceActivePoolIsExhausted()
{
    DiceRevolverRuntime runtime = new DiceRevolverRuntime(2f, 2f, false, true);
    runtime.RebuildActiveFaces(new[] { 2 });
    for (int i = 0; i < 5; i++) { runtime.TryBeginShot(0f + i); }
    DiceRevolverRuntimeUpdate update = runtime.Tick(10f, true);
    Assert.That(update.ReloadStarted, Is.True);
}
```

- [ ] **Step 2: 运行确认失败**

Expected: 编译失败（`RebuildActiveFaces` 不存在）。

- [ ] **Step 3: 实现**

`DiceRevolverRuntime.cs`：
- 新增字段 `private readonly HashSet<int> passiveFaces = new();`
- 新增方法：

```csharp
public int ActiveFaceCount => DiceRevolverRules.FaceCount - passiveFaces.Count;

public void RebuildActiveFaces(IReadOnlyCollection<int> passiveFaceSet)
{
    passiveFaces.Clear();
    if (passiveFaceSet != null)
    {
        passiveFaces.UnionWith(passiveFaceSet);
    }
    RefillAllFaces();
}
```

- `RefillAllFaces()` 改为只加入非被动面：

```csharp
private void RefillAllFaces()
{
    remainingFaces.Clear();
    for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
    {
        if (!passiveFaces.Contains(face))
        {
            remainingFaces.Add(face);
        }
    }
}
```

- 构造函数末尾 `RefillAllFaces()` 保持不变（passiveFaces 初始为空 = 全活动）。
- `TryBeginManualReload` 的满池判断改为 `RemainingRounds < ActiveFaceCount`。
- `TryRefillAndForceNextFace`：`remainingFaces.Contains(face)` 检查保持（被动面不会被填入；若 LoadedFour 强制被动面则返回 false，符合"不消耗/不可强抽被动面"语义）。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceRevolverRuntime.cs Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs
git commit -m "feat: 骰池池级排除被动面"
```

---

### Task 4: Gun 接线——被动面集合推送与 legacy 被动路径摘除

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/`（既有枪械聚焦测试；先通读 `DiceRevolverGunTests` 或等同文件确认运行时创建位置）

**Interfaces:**
- Consumes: `DiceRevolverRuntime.RebuildActiveFaces`（Task 3）、`DiceFaceLoadout.GetPassiveFaceSet`（Task 2）。
- Produces: 无新公共接口；`HandleLoadoutSlotChanged` 在基础槽变化时重建活动池。

- [ ] **Step 1: 通读并定位**

通读 `DiceRevolverGun.cs` 全文，定位：`runtime`（DiceRevolverRuntime）创建/初始化处；`passiveRuntime`（DicePassiveRuntime）的 `RebuildFace` 绑定（约 114–117、528–534 行）；`FilterPassiveDrawCandidates`（约 550 行）。

- [ ] **Step 2: 写失败测试（重建活动池触发）**

在枪械聚焦测试中（如存在 loadout 变更用例）追加：把面 1 的基础槽换成带 `isPassiveBase` 的词条（复用 Task 2 的构造方式）后，断言 `gun` 内部 runtime 的 `RemainingRounds == 5`（通过公开 API 或反射验证；若无公开访问器，则断言 loadout 变更后下一次射击不抽到面 1 的范围，用 `TryBeginShot` + `CreateRemainingFacesSnapshot` 的暴露方式判断——以代码实际结构为准，测试必须能表达"被动面不再可抽"）。

- [ ] **Step 3: 实现**

- 在 runtime 初始化（loadout 绑定完成）后调用：

```csharp
runtime.RebuildActiveFaces(loadout.GetPassiveFaceSet());
```

- `HandleLoadoutSlotChanged`：`slotType == DiceFaceSlotType.Base` 分支改为重建活动池并更新基础弹丸类型：

```csharp
if (slotType == DiceFaceSlotType.Base)
{
    runtime.RebuildActiveFaces(loadout.GetPassiveFaceSet());
    passiveRuntime?.UpdateBaseProjectileType(face, GetBaseProjectileType(snapshot));
    return;
}
```

- 删除 `slotType == DiceFaceSlotType.Passive` 分支（约 528–534）与初始化循环中 `passiveRuntime.RebuildFace(face, snapshot.GetPassiveEffect(), ...)`（约 114–117）；`snapshot.GetPassiveEffect()` 已不存在（Task 1 删除）。
- `FilterPassiveDrawCandidates` 简化为只走规则被动路径（legacy `passiveRuntime.FilterDrawCandidates` 调用删除）：

```csharp
private DiceDrawConstraintResult FilterPassiveDrawCandidates(
    IReadOnlyList<int> realChamberPool, int? forcedFace)
{
    return eventRuleRuntimes.FilterDrawCandidates(realChamberPool, realChamberPool, forcedFace);
}
```

- 全被动警告：初始化与 loadout 变更处，若 `loadout.GetPassiveFaceSet().Count == DiceRevolverRules.FaceCount`，`Debug.LogWarning` 一次（"弹巢全部为被动面，每轮无法射击。"）。
- 若 `passiveRuntime` 字段不再被引用，删除字段与 `ConfigureBonusActivation`/`ConfigureDebugTrace` 的 legacy 调用（以编译零警告为准；`DicePassiveRuntime` 类本身保留）。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Tests/EditMode/
git commit -m "feat: gun 推送被动面集合并摘除 legacy 被动绑定"
```

---

### Task 5: 规则运行时被动绑定改为被动面 base 槽

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs`
- Test: `Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs`（先通读既有被动集成测试）

**Interfaces:**
- Consumes: `DiceFaceConfigurationSnapshot.IsPassiveFace`（Task 1）。
- Produces: 无签名变化；`ExecutePassive`/`ModifyProjectileStats` 语义改为只遍历被动面。

- [ ] **Step 1: 写失败测试（被动规则只从被动面 base 槽执行）**

在 `EventRulePassiveIntegrationTests.cs` 追加（构造方式仿照既有用例）：
- 面 1 基础槽装"特斯拉"类被动规则（ProjectileSpawned 触发），且 `snapshot.IsPassiveFace == true` → 弹丸生成后层数增加（复用既有断言模式）。
- 对照：面 1 基础槽装同一规则但 `IsPassiveFace == false` → 弹丸生成后**不**增加层数（被动服务不再遍历非被动面）。

- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`DiceEventRuleRuntimeSet.cs`：
- `SlotCount` 5 → 4。
- 新增 `private readonly bool[] passiveFaces = new bool[DiceRevolverRules.FaceCount];`
- `RebuildFace`：`passiveFaces[faceIndex] = snapshot.IsPassiveFace;`（放在 baseProjectileTypes 赋值后）。
- `ExecutePassive`（约 300–312）改为：

```csharp
for (int faceIndex = 0; faceIndex < DiceRevolverRules.FaceCount; faceIndex++)
{
    if (!passiveFaces[faceIndex])
    {
        continue;
    }
    EventRuleRuntime runtime = runtimes[faceIndex, (int)DiceFaceSlotType.Base];
    if (runtime == null)
    {
        continue;
    }
    EventSignal equippedSignal = CreateSignalForEquippedFace(signal, faceIndex);
    PassiveEventRuleServices services = sharedServices ?? CreateServices(equippedSignal);
    TryExecute(runtime, equippedSignal, services);
}
```

- `ModifyProjectileStats`（约 152–154）同样改为 `passiveFaces[faceIndex]` 且取 `runtimes[faceIndex, (int)DiceFaceSlotType.Base]`。
- `TryExecute` 异常日志的 definitions 索引 `(int)DiceFaceSlotType.Passive` → `(int)DiceFaceSlotType.Base`。
- `CreateSignal`/`CreateSignalForEquippedFace` 的 equipped slot 参数 `DiceFaceSlotType.Passive` → `DiceFaceSlotType.Base`。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs
git commit -m "feat: 被动规则运行时绑定到被动面基础槽"
```

---

### Task 6: 校验器与规则定义清理

**Files:**
- Modify: `Assets/Scripts/Editor/EventRuleValidator.cs`
- Modify: `Assets/Scripts/Prototype/EventRuleDefinition.cs`
- Test: `Assets/Tests/EditMode/EventRuleEditorInfrastructureTests.cs`

**Interfaces:**
- Consumes: 无。
- Produces: `EventRuleValidationEnvironment` 删除 `PassiveStateSupported`；`EventRuleDefinition.ToMask` 不再处理 Passive。

- [ ] **Step 1: 写失败测试**

在 `EventRuleEditorInfrastructureTests.cs` 追加：基础槽装备"allowedSlots 不含基础位"的规则 → `CollectValidationIssues(DiceFaceSlotType.Base)` 含 `slot-not-allowed`（既有语义，验证不被被动位干扰）；并删除/改造依赖 `PassiveStateSupported` 的既有用例。

- [ ] **Step 2: 运行确认失败**（若既有用例因删除字段编译失败，即为预期红）
- [ ] **Step 3: 实现**

`EventRuleValidator.cs`：
- `EventRuleValidationEnvironment` 删除 `passiveStateSupported` 字段与构造参数；`Default` 改为 `new EventRuleValidationEnvironment(true)`；删除 `PassiveStateSupported` 属性与常量 `PassiveStateUnsupported`。
- 删除 `Validate` 中 `slot == DiceFaceSlotType.Passive && !environment.PassiveStateSupported` 分支（约 79–86）。

`EventRuleDefinition.cs`：
- `ToMask` 删除 `DiceFaceSlotType.Passive => DiceFaceSlotMask.Passive` 分支（返回 `DiceFaceSlotMask.None` 默认）。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Editor/EventRuleValidator.cs Assets/Scripts/Prototype/EventRuleDefinition.cs Assets/Tests/EditMode/EventRuleEditorInfrastructureTests.cs
git commit -m "refactor: 校验器与规则定义移除被动槽分支"
```

---

### Task 7: 规则编辑器与构筑 UI

**Files:**
- Modify: `Assets/Scripts/Editor/EventRuleEditorWindow.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildPageUI.cs`（先通读，找到 `DiceBuildFaceSlotUI.Configure` 调用处）
- Test: `Assets/Tests/EditMode/EventRuleEditorWindowTests.cs`

**Interfaces:**
- Consumes: `DiceFaceConfigurationSnapshot.IsPassiveFace`（Task 1）。
- Produces: 无公共 API 变化。

- [ ] **Step 1: 写失败测试（编辑器分类不再含被动）**

`EventRuleEditorWindowTests.cs` 追加：`window.SelectionState.SlotFilter = DiceFaceSlotType.Passive` 不再有意义 → 断言窗口左栏 `SlotNames` 数组不包含"被动"（通过 `internal` 访问或反射；若无法访问，改为断言 `SlotFilters` 长度 4）。若既有用例设置了 Passive 过滤器，改造为 Base。

- [ ] **Step 2: 运行确认失败**
- [ ] **Step 3: 实现**

`EventRuleEditorWindow.cs`：
- `SlotFilters` 删除 `DiceFaceSlotType.Passive` 项；`SlotNames` 删除 `"被动"` 项。

`DiceBuildFaceSlotUI.cs`：
- `SetConfiguration` 删除 `SetSlotLabel(passiveLabel, DiceFaceSlotType.Passive, configuration);`
- `Bind` 中面标签显示被动徽标：

```csharp
faceLabel.text = configuration.IsPassiveFace ? $"{face}（被动）" : face.ToString();
```

- `DiceBuildPageUI.cs`：通读后删除 `Configure(...)` 调用中被动 Text 实参（`configuredPassiveLabel` 传 null 即可，签名保留），并确认布局不再生成被动行。

- [ ] **Step 4: 运行测试确认通过**
- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Editor/EventRuleEditorWindow.cs Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs Assets/Scripts/Prototype/DiceBuildPageUI.cs Assets/Tests/EditMode/EventRuleEditorWindowTests.cs
git commit -m "feat: 编辑器与构筑 UI 移除被动槽概念"
```

---

### Task 8: 迁移工具与规则资产

**Files:**
- Modify: `Assets/Scripts/Editor/EventRuleMigrationUtility.cs`
- Modify: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- Modify（资产，由迁移工具写入）: `Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset`、`EchoSynergy.asset`、`Finisher.asset`
- Modify（资产）: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset`、`EchoSynergyRule.asset`、`FinisherRule.asset`
- Test: `Assets/Tests/EditMode/EventRuleMigrationTests.cs`（或既有迁移测试文件）

**Interfaces:**
- Consumes: 无。
- Produces: 迁移后 3 词条 `slotType=0` + `isPassiveBase=true`；3 规则 `allowedSlots=1`；重复执行结果稳定（幂等）。

- [ ] **Step 1: 写失败测试（迁移幂等）**

在迁移测试文件追加（仿照既有迁移用例的资产创建/加载方式）：

```csharp
[Test]
public void PassiveBaseMigrationNormalizesEntriesAndRules()
{
    // 调用 EventRuleMigrationUtility 的新迁移入口（Step 3 实现）
    // 断言：Tesla/EchoSynergy/Finisher 三个 DiceFaceEntry 资产
    //   slotType == (int)DiceFaceSlotType.Base 且 isPassiveBase == true
    // 断言：三个规则资产 allowedSlots == (int)DiceFaceSlotMask.Base
    // 再次调用，断言结果一致（幂等）
}
```

- [ ] **Step 2: 运行确认失败**（迁移入口不存在 → 编译失败）
- [ ] **Step 3: 实现**

`EventRuleMigrationUtility.cs` 新增（复用既有 `SerializedObject`/`AssetDatabase` 模式，参照文件内既有迁移方法）：

```csharp
public static void MigratePassiveBaseEntries()
{
    string[] paths = AssetDatabase.FindAssets("t:DiceFaceEntry")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => path.Contains("DiceFacePrototype/DiceFaces/"))
        .ToArray();
    foreach (string path in paths)
    {
        SerializedObject serialized = new SerializedObject(AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(path));
        SerializedProperty slotType = serialized.FindProperty("slotType");
        SerializedProperty isPassiveBase = serialized.FindProperty("isPassiveBase");
        if (slotType != null && slotType.intValue == (int)DiceFaceSlotType.Passive)
        {
            slotType.intValue = (int)DiceFaceSlotType.Base;
            isPassiveBase.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(serialized.targetObject);
        }
    }
    AssetDatabase.SaveAssets();
}

public static void MigratePassiveRuleSlots()
{
    string[] names = { "TeslaRule", "EchoSynergyRule", "FinisherRule" };
    foreach (string ruleName in names)
    {
        string path = AssetDatabase.FindAssets($"t:EventRuleDefinition {ruleName}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.EndsWith($"/{ruleName}.asset", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(path)) { continue; }
        SerializedObject serialized = new SerializedObject(AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path));
        SerializedProperty allowedSlots = serialized.FindProperty("allowedSlots");
        if (allowedSlots != null && allowedSlots.intValue != (int)DiceFaceSlotMask.Base)
        {
            allowedSlots.intValue = (int)DiceFaceSlotMask.Base;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(serialized.targetObject);
        }
    }
    AssetDatabase.SaveAssets();
}
```

（若 `AssetDatabase.FindAssets("t:EventRuleDefinition TeslaRule")` 搜索语法不支持，改用 `FindAssets("t:EventRuleDefinition")` 后按文件名过滤。）

`LightningBuildPrototypeBuilder.cs`：通读后把被动规则创建路径（约 96/109/132 行使用 `DiceFaceSlotType.Passive` 处）改为新语义：规则 `allowedSlots` 用 `DiceFaceSlotMask.Base`，词条 `slotType` 用 `DiceFaceSlotType.Base` 并置 `isPassiveBase=true`；保持"只创建缺失资产、保留既有非空参数"原则。

- [ ] **Step 4: 运行迁移并验证**

Run: Unity 内执行迁移（通过迁移测试或编辑器菜单）；随后：

```powershell
Select-String -Path 'Assets\Resources\DiceFacePrototype\DiceFaces\Tesla.asset','Assets\Resources\DiceFacePrototype\DiceFaces\EchoSynergy.asset','Assets\Resources\DiceFacePrototype\DiceFaces\Finisher.asset' -Pattern 'slotType: 0|isPassiveBase: 1'
Select-String -Path 'Assets\Resources\DiceFacePrototype\EventRules\Lightning\TeslaRule.asset','Assets\Resources\DiceFacePrototype\EventRules\Lightning\EchoSynergyRule.asset','Assets\Resources\DiceFacePrototype\EventRules\Lightning\FinisherRule.asset' -Pattern 'allowedSlots: 1'
```

Expected: 3 词条 `slotType: 0` + `isPassiveBase: 1`；3 规则 `allowedSlots: 1`。

- [ ] **Step 5: 提交**

```bash
git add Assets/Scripts/Editor/EventRuleMigrationUtility.cs Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset Assets/Resources/DiceFacePrototype/DiceFaces/EchoSynergy.asset Assets/Resources/DiceFacePrototype/DiceFaces/Finisher.asset Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset Assets/Resources/DiceFacePrototype/EventRules/Lightning/EchoSynergyRule.asset Assets/Resources/DiceFacePrototype/EventRules/Lightning/FinisherRule.asset Assets/Tests/EditMode/
git commit -m "feat: 迁移被动词条与规则为被动基础语义"
```

> 注意：提交前确认工作区这些 .asset 的差异**只含本次迁移改动**；会话期间用户在编辑器中改过的 `allowedSlots 2/4/1` 会一并被归一为 1（规格已批准）。

---

### Task 9: 静态门禁与全量回归

**Files:**
- Test: 全 EditMode 套件
- 校验：`Assets` 内 `DiceFaceSlotType.Passive` 零引用；受保护文件 SHA256

- [ ] **Step 1: 静态门禁**

```powershell
Select-String -Path 'Assets\Scripts\*','Assets\Scripts\Editor\*' -Pattern 'DiceFaceSlotType\.Passive' -Recurse
Select-String -Path 'Assets\Resources\DiceFacePrototype\*.asset' -Pattern 'slotType: 4' -Recurse
```

Expected: 均无匹配（枚举定义与历史数据读取除外——若 `EventRuleMigrationUtility` 或兼容读取仍引用 Passive 定义，需逐一确认属于"序列化兼容读取"并记录豁免）。

- [ ] **Step 2: 受保护资产 SHA256**

迁移前已记录的 10 个受保护文件（Player、TestRobot、TargetDummy、场景、三个基础弹丸 Prefab、fire_1、BlastExplosion、LightningOrb、LightningChain）哈希必须与基线一致。若本任务实现过程未触碰它们，直接记录 `passed`；如有差异，停止并排查。

- [ ] **Step 3: 全量 EditMode 回归**

Run: 既有全量 EditMode 命令（隔离副本，避免与打开的编辑器冲突）
Expected: 全部通过，唯一允许失败为已获豁免的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`（Ground `Y=-0.01`）。

- [ ] **Step 4: 更新上下文并提交**

更新 `.project-context/project/workstreams/2026-08-23-passive-base-events/STATE.md` 与 `HANDOFF.md`（状态、验证记录、下一步）；`STATUS.md` 活跃工作流状态；运行 `check.ps1` 返回 `[context:ok]`。

```bash
git add .project-context/project/
git commit -m "docs: 被动基础事件工作流状态同步"
```

---

## Self-Review 记录（计划作者自查）

- **规格覆盖**：D1–D6 全部落到任务（D1→T1、D2→T5 语义、D3→T4 警告、D4→T1/T2、D5→T3/T4、D6→T4）；迁移（规格第 4 节）→T8；编辑器/UI（第 3 节）→T7；测试策略（第 5 节）→T1–T3/T5/T8 各任务测试 + T9 回归；受保护资产→T9。无遗漏。
- **占位符扫描**：无 TBD/TODO；涉及未通读文件（DiceBuildPageUI、LightningBuildPrototypeBuilder、迁移测试文件）的步骤均给出明确"先通读定位再改"的指令与目标语义。
- **类型一致性**：`IsPassiveFace`/`IsPassiveBase`/`GetPassiveFaceSet`/`RebuildActiveFaces`/`ActiveFaceCount` 在 T1–T4 间签名一致；`DiceFaceSlotMask.Base == 1` 与资产 `allowedSlots: 1` 一致。
