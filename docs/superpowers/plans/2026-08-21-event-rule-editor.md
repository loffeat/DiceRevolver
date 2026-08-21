# Event Rule Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a list-based Unity event-rule editor and a low-coupling runtime that can express, debug, and gradually replace the current active and passive dice effects without changing existing combat behavior.

**Architecture:** `EventRuleDefinition` owns read-only Trigger/Condition/Result SubAssets. A per-gun, per-face `DiceEventRuleRuntimeSet` owns all mutable state and evaluates immutable `EventSignal` values through restricted services; `DiceShotPipeline` and `DiceRevolverGun` publish signals but do not know concrete rule modules. The EditorWindow discovers assets and module types through `AssetDatabase` and `TypeCache`, while legacy effects remain the fallback until each resource migration has behavioral parity.

**Tech Stack:** Unity `6000.3.10f1`, C#, ScriptableObject/SubAsset serialization, UnityEditor `EditorWindow`, `TypeCache`, `SerializedObject`, `Undo`, Unity Test Framework `1.6.0`, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-08-21-event-rule-editor-design.md`

## Global Constraints

- First version is a rule list with three columns; do not add a node graph, loops, script expressions, or runtime code generation.
- A rule has one Trigger, AND-combined rule Conditions, and ordered ResultEntries; every ResultEntry has its own AND-combined local Conditions.
- `EventRuleDefinition` and module SubAssets are immutable during play; mutable state lives in one runtime instance per Gun, equipped face, and slot.
- Module discovery uses `TypeCache.GetTypesDerivedFrom`; rule discovery uses `AssetDatabase.FindAssets`.
- Modules may request behavior only through restricted services; they may not search for Player, use scene singletons, invoke arbitrary methods through reflection, or mutate character 3C.
- Existing `BulletEventEffect` and `PassiveEventEffect` remain compatible until their corresponding assets have migrated; when a `DiceFaceEntry` has a Rule, runtime must not also execute its legacy Effect.
- All delayed work, bonus activations, and chains share the originating `DiceEventBudget`; default Gun budget remains `32` and minimum remains `1`.
- A failing Trigger, Condition, or Result stops only the current rule invocation and is recorded; it must not stop another slot, rule, Gun, or character controller.
- Editor validation reports Error, Warning, or Info and never silently overwrites serialized designer data.
- Do not run `TopDownPrototypeSceneBuilder`; do not modify Player, TestRobot, TargetDummy, AimRoot, Renderer, sorting layer, Transform, weapon tuning, projectile speed, or art scale.
- Preserve the approved Ground `Y=-0.01` exception: full EditMode may have only `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities` failing, and the result must remain labeled `[failed]`.
- Visible PlayMode layout and feel are `[not-run]` unless a human actually performs them.

---

### Task 1: Rule definition, immutable signal, and runtime state vocabulary

**Files:**
- Create: `Assets/Scripts/Prototype/EventRuleTypes.cs`
- Create: `Assets/Scripts/Prototype/EventSignal.cs`
- Create: `Assets/Scripts/Prototype/EventRuleModule.cs`
- Create: `Assets/Scripts/Prototype/EventRuleDefinition.cs`
- Create: `Assets/Scripts/Prototype/EventRuleStateStore.cs`
- Create: `Assets/Scripts/Prototype/EventRuleServices.cs`
- Test: `Assets/Tests/EditMode/EventRuleModelTests.cs`

**Interfaces:**
- Produces: `EventSignalType`, `EventSignalMask`, `DiceFaceSlotMask`, `EventRuleRecursionPolicy`, `EventConditionResult`, `EventResult`, `EventRuleValidationSeverity`, `EventRuleValidationIssue`, `EventRuleDefinition`, `EventResultEntry`, `EventTriggerModule`, `EventConditionModule`, `EventResultModule`, `EventRuleStateStore`, `IEventRuleServices`, `EventEvaluationContext`, and `EventExecutionContext`.
- Consumes: `DiceFaceSlotType`, `DiceFaceActivation`, `DiceRevolverShotContext`, `ProjectileHandle`, `ProjectileRuntimeStats`, `DiceEventBudget`, and `CombatDebugScope`.

- [ ] **Step 1: Write model tests for slots, SubAsset references, immutable signals, and isolated state**

```csharp
[Test]
public void RuleDefinitionKeepsOrderedResultsAndAllowedSlots()
{
    EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
    TestTrigger trigger = ScriptableObject.CreateInstance<TestTrigger>();
    TestResult first = ScriptableObject.CreateInstance<TestResult>();
    TestResult second = ScriptableObject.CreateInstance<TestResult>();
    Set(rule, "allowedSlots", DiceFaceSlotMask.OnFire | DiceFaceSlotMask.OnHit);
    Set(rule, "trigger", trigger);
    Set(rule, "results", new List<EventResultEntry>
    {
        new EventResultEntry(Array.Empty<EventConditionModule>(), first),
        new EventResultEntry(Array.Empty<EventConditionModule>(), second)
    });

    Assert.That(rule.AllowsSlot(DiceFaceSlotType.OnFire), Is.True);
    Assert.That(rule.AllowsSlot(DiceFaceSlotType.Passive), Is.False);
    Assert.That(rule.Results.Select(entry => entry.Result), Is.EqualTo(new[] { first, second }));
}

[Test]
public void StateStoresAreIndependentForTheSameModuleAsset()
{
    TestResult module = ScriptableObject.CreateInstance<TestResult>();
    EventRuleStateStore first = new EventRuleStateStore();
    EventRuleStateStore second = new EventRuleStateStore();
    first.SetInt(module, "count", 3);

    Assert.That(first.GetInt(module, "count"), Is.EqualTo(3));
    Assert.That(second.GetInt(module, "count"), Is.Zero);
}
```

- [ ] **Step 2: Run the new model tests and verify RED**

Run:

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testFilter "DiceRevolver.Tests.EventRuleModelTests" -testResults .\Logs\event-rule-model-red.xml -logFile .\Logs\event-rule-model-red.log
```

Expected: compile failure because `EventRuleDefinition`, module bases, signal/context types, restricted services, and state store do not exist.

- [ ] **Step 3: Add the exact public vocabulary**

```csharp
[Flags]
public enum DiceFaceSlotMask
{
    None = 0,
    Base = 1 << 0,
    OnFire = 1 << 1,
    OnHit = 1 << 2,
    OnFireEnd = 1 << 3,
    Passive = 1 << 4,
    Active = Base | OnFire | OnHit | OnFireEnd,
    All = Active | Passive
}

[Flags]
public enum EventSignalMask
{
    None = 0,
    Base = 1 << 0,
    OnFire = 1 << 1,
    OnHit = 1 << 2,
    OnFireEnd = 1 << 3,
    ProjectileSpawned = 1 << 4,
    ProjectileHit = 1 << 5,
    ReloadStarted = 1 << 6,
    ReloadCompleted = 1 << 7,
    FaceConsumed = 1 << 8,
    DrawCandidate = 1 << 9,
    BeforeProjectileStats = 1 << 10
}

public enum EventSignalType
{
    Base,
    OnFire,
    OnHit,
    OnFireEnd,
    ProjectileSpawned,
    ProjectileHit,
    ReloadStarted,
    ReloadCompleted,
    FaceConsumed,
    DrawCandidate,
    BeforeProjectileStats
}

public enum EventRuleRecursionPolicy
{
    DenyReentry = 0,
    AllowWithBudget = 1,
    IgnoreBonusActivation = 2
}

public readonly struct EventConditionResult
{
    public EventConditionResult(bool passed, string description, string failureReason = null)
    {
        Passed = passed;
        Description = description;
        FailureReason = failureReason;
    }

    public bool Passed { get; }
    public string Description { get; }
    public string FailureReason { get; }
}

public enum EventResultStatus { Success, Skipped, Failed }

public readonly struct EventResult
{
    public EventResult(EventResultStatus status, string description)
    {
        Status = status;
        Description = description;
    }

    public EventResultStatus Status { get; }
    public string Description { get; }
}

public enum EventRuleValidationSeverity { Info, Warning, Error }

public readonly struct EventRuleValidationIssue
{
    public EventRuleValidationIssue(EventRuleValidationSeverity severity,
        string code, string message, UnityEngine.Object context)
    {
        Severity = severity;
        Code = code;
        Message = message;
        Context = context;
    }

    public EventRuleValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }
}
```

`EventSignal` must expose get-only properties for signal type, equipped face, source face, slot, activation, shot, projectile, hit collider/position, remaining faces, draw candidate, current stats, event budget, bonus-activation flag, and Debug scope. Constructor parameters are copied directly; callers cannot mutate the signal.

```csharp
public abstract class EventTriggerModule : ScriptableObject
{
    public abstract bool Matches(EventSignal signal);
    public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
}

public abstract class EventConditionModule : ScriptableObject
{
    public abstract EventConditionResult Evaluate(EventEvaluationContext context);
    public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
}

public abstract class EventResultModule : ScriptableObject
{
    public abstract EventResult Execute(EventExecutionContext context);
    public virtual void CollectValidationIssues(List<EventRuleValidationIssue> issues) { }
}
```

Create `EventRuleServices.cs` in the same task so the module signatures compile independently:

```csharp
public interface IEventRuleServices
{
    DiceEventBudget EventBudget { get; }
    bool RequestProjectile(ProjectileDefinition definition, Vector3 origin, Vector3 direction,
        AttackEffectOverride attackEffectOverride, bool isPrimary);
    bool Schedule(float delaySeconds, Action callback);
    bool RequestBonusActivation(int face, float maximumSpreadAngle,
        float minimumSpreadSeparation, EventRuleDefinition sourceRule);
    bool RequestRefillAndForceNextFace(int face);
    bool RequestLightningChain(ProjectileHandle origin,
        IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition);
    bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay);
    IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius,
        ProjectileTagDefinition requiredTag, Projectile excludedProjectile);
    void SetDrawPriority(int priority);
    void RejectDrawCandidate(string reason);
    void MultiplyProjectileDamage(float multiplier);
    void RecordRuleDebug(EventRuleDefinition rule, string stage,
        string description, EventResultStatus status);
    void ReportException(Exception exception, ScriptableObject module);
}
```

`EventEvaluationContext` is an immutable tuple of Signal, State, and Services. `EventExecutionContext` adds an internal scheduling delegate and exposes `ScheduleEntries(float, IReadOnlyList<EventResultEntry>)`; Task 2 supplies that delegate from the executor. A missing scheduling delegate returns false.

`EventRuleDefinition` serializes `displayName`, `description`, `displayColor`, `tags`, `rarity`, `allowedSlots`, `trigger`, `conditions`, ordered `results`, `eventBudgetCost` defaulting to `1`, and `recursionPolicy` defaulting to `DenyReentry`. `EventResultEntry` serializes local Conditions and one Result. Expose read-only properties only. `CollectValidationIssues(slot)` aggregates structural and module issues; `CanEquip(slot)` returns false when that list contains Error and never mutates serialized data.

- [ ] **Step 4: Implement a module-keyed state store without writing to ScriptableObjects**

```csharp
public sealed class EventRuleStateStore
{
    private readonly Dictionary<(ScriptableObject, string), object> values = new();

    public int GetInt(ScriptableObject owner, string key, int fallback = 0) =>
        values.TryGetValue((owner, key), out object value) && value is int number
            ? number
            : fallback;

    public void SetInt(ScriptableObject owner, string key, int value) =>
        values[(owner, key)] = value;

    public bool GetBool(ScriptableObject owner, string key, bool fallback = false) =>
        values.TryGetValue((owner, key), out object value) && value is bool flag
            ? flag
            : fallback;

    public void SetBool(ScriptableObject owner, string key, bool value) =>
        values[(owner, key)] = value;

    public void Clear() => values.Clear();
}
```

Add float accessors with the same owner/key isolation. Reject null owners and blank keys with `ArgumentException` so module mistakes fail inside the rule boundary instead of corrupting shared state.

- [ ] **Step 5: Run model tests and the existing dice-face configuration tests**

Run the new model suite plus `DiceFacePassiveSlotTests` and `DiceFaceLoadoutTests`. Expected: all pass, 0 skipped.

- [ ] **Step 6: Commit Task 1**

```powershell
git add -- Assets/Scripts/Prototype/EventRuleTypes.cs Assets/Scripts/Prototype/EventRuleTypes.cs.meta Assets/Scripts/Prototype/EventSignal.cs Assets/Scripts/Prototype/EventSignal.cs.meta Assets/Scripts/Prototype/EventRuleModule.cs Assets/Scripts/Prototype/EventRuleModule.cs.meta Assets/Scripts/Prototype/EventRuleDefinition.cs Assets/Scripts/Prototype/EventRuleDefinition.cs.meta Assets/Scripts/Prototype/EventRuleStateStore.cs Assets/Scripts/Prototype/EventRuleStateStore.cs.meta Assets/Scripts/Prototype/EventRuleServices.cs Assets/Scripts/Prototype/EventRuleServices.cs.meta Assets/Tests/EditMode/EventRuleModelTests.cs Assets/Tests/EditMode/EventRuleModelTests.cs.meta
git commit -m "建立事件规则数据模型"
```

### Task 2: Deterministic executor, restricted services, budget, and exception isolation

**Files:**
- Create: `Assets/Scripts/Prototype/EventRuleRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DiceEventBudget.cs`
- Test: `Assets/Tests/EditMode/EventRuleRuntimeTests.cs`
- Test: `Assets/Tests/EditMode/DiceFaceActivationTests.cs`

**Interfaces:**
- Consumes: Task 1 model/context/service types and existing `DiceEventBudget`.
- Produces: `EventRuleInvocationResult` and `EventRuleRuntime.TryHandle(EventSignal, IEventRuleServices)`.

- [ ] **Step 1: Write failing executor tests**

Cover these exact cases with recording Trigger/Condition/Result modules and a fake service:

```csharp
[Test]
public void ExecutesRuleConditionsThenLocalConditionsThenResultsInOrder()
{
    List<string> order = new();
    EventRuleDefinition rule = CreateRule(
        CreateRecordingTrigger(order, true),
        new[] { CreateRecordingCondition(order, "rule", true) },
        Entry(CreateRecordingCondition(order, "local-1", true), CreateRecordingResult(order, "result-1")),
        Entry(CreateRecordingCondition(order, "local-2", false), CreateRecordingResult(order, "result-2")),
        Entry(null, CreateRecordingResult(order, "result-3")));

    EventRuleInvocationResult result = new EventRuleRuntime(rule, 2, DiceFaceSlotType.OnFire)
        .TryHandle(Signal(EventSignalType.OnFire), new FakeRuleServices());

    Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
    Assert.That(order, Is.EqualTo(new[]
    {
        "trigger", "rule", "local-1", "result-1", "local-2", "result-3"
    }));
}
```

Also assert: trigger mismatch consumes no budget; rule Conditions use AND and stop at first failure; a skipped local Condition skips only that ResultEntry; a failed Result stops later Results; recursion policy blocks same-runtime reentry; bonus signals are ignored only for `IgnoreBonusActivation`; two runtimes from one asset have independent state; Trigger/Condition/Result exceptions are reported once and do not escape; budget exhaustion stops the rule before results.

- [ ] **Step 2: Run executor tests and verify RED**

Expected: compile failure because executor and invocation-result types do not exist.

- [ ] **Step 3: Add atomic multi-consume to `DiceEventBudget`**

```csharp
public bool TryConsume(int amount, Action exhaustedWarning = null)
{
    int required = Mathf.Max(1, amount);
    if (Remaining >= required)
    {
        Remaining -= required;
        return true;
    }

    if (!warningIssued)
    {
        warningIssued = true;
        exhaustedWarning?.Invoke();
    }

    return false;
}

public bool TryConsume(Action exhaustedWarning = null) =>
    TryConsume(1, exhaustedWarning);
```

Add a regression test proving a budget of `1` cannot partially pay a cost of `2` and remains `1`.

- [ ] **Step 4: Connect the executor to the restricted contexts**

Construct one `EventEvaluationContext` for the invocation and one `EventExecutionContext` per Result. The execution context's scheduling delegate captures the same runtime, signal, state store, originating budget, recursion guard, and Debug scope; scheduled nested entries are evaluated only when the existing scheduler invokes the callback.

- [ ] **Step 5: Implement deterministic `EventRuleRuntime`**

Implement `TryHandle` in this exact order: validate definition/trigger; apply bonus/reentry policy; call Trigger; when the signal has an originating `DiceEventBudget`, atomically consume `Mathf.Max(1, rule.EventBudgetCost)`; for reload/draw signals without an originating activation, skip budget consumption rather than allocating a fresh budget; evaluate rule Conditions in list order; evaluate each entry's local Conditions; execute its Result; stop only on Failed or exception; always release the reentry guard in `finally`. All module calls have their own `try/catch`, call `ReportException`, and return a Failed invocation without throwing to the caller.

- [ ] **Step 6: Run Task 2 tests**

Run `EventRuleRuntimeTests`, `DiceFaceActivationTests`, `DiceShotPipelineTests`, and `CombatDebugPipelineTests`. Expected: all pass, 0 skipped.

- [ ] **Step 7: Commit Task 2**

```powershell
git add -- Assets/Scripts/Prototype/EventRuleRuntime.cs Assets/Scripts/Prototype/EventRuleRuntime.cs.meta Assets/Scripts/Prototype/DiceEventBudget.cs Assets/Tests/EditMode/EventRuleRuntimeTests.cs Assets/Tests/EditMode/EventRuleRuntimeTests.cs.meta Assets/Tests/EditMode/DiceFaceActivationTests.cs
git commit -m "实现受限事件规则执行器"
```

### Task 3: Active-slot coexistence with legacy effects

**Files:**
- Create: `Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs`
- Create: `Assets/Scripts/Prototype/BulletEventRuleServices.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- Modify: `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/EventRuleActiveIntegrationTests.cs`
- Test: `Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs`
- Test: `Assets/Tests/EditMode/DiceShotPipelineTests.cs`

**Interfaces:**
- Produces: `DiceFaceEntry.Rule`, snapshot `GetRule(DiceFaceSlotType)`, `DiceEventRuleRuntimeSet.RebuildFace`, `ExecuteActive`, and `BulletEventRuleServices`.
- Consumes: Task 2 executor; existing `BulletEventContext`, Pipeline scheduling, activation commands, Combat Debug, and Loadout slot-change notifications.

- [ ] **Step 1: Write RED tests for new/legacy exclusivity and persistent per-face runtimes**

Assert all of the following:

```csharp
Assert.That(entry.Rule, Is.SameAs(rule));
Assert.That(entry.Effect, Is.Null, "a new Rule must suppress the legacy Effect");
Assert.That(snapshot.GetRule(DiceFaceSlotType.OnFire), Is.SameAs(rule));
Assert.That(snapshot.GetEffect(DiceFaceSlotType.OnFire), Is.Null);
```

Execute two shots through `DiceShotPipeline`: a Rule-backed OnFire entry runs exactly once; a legacy-only entry still runs exactly once; a malformed entry with both serialized fields runs only the Rule. Rebuild face 1 without rebuilding face 2 and assert face 2 runtime state is unchanged.

- [ ] **Step 2: Run active integration tests and verify RED**

Expected: compile failures for `Rule`, `GetRule`, and runtime-set APIs.

- [ ] **Step 3: Add compatibility serialization without rewriting assets**

Add `[SerializeField, InspectorName("事件规则")] private EventRuleDefinition rule;` to `DiceFaceEntry`. Runtime precedence is Rule, then slot-appropriate legacy Effect/PassiveEffect. `SlotType` resolves from explicit serialized slot when Rule is present. Add Rule to `DiceFaceConfigurationSnapshot` for all five slots without renaming existing serialized fields. `DiceFaceConfiguration.Equip` rejects a Rule entry when its Trigger or Results are missing or its allowed-slot mask excludes the entry slot; rejection leaves the previously equipped entry unchanged.

- [ ] **Step 4: Implement persistent active runtimes and Bullet service adapter**

`DiceEventRuleRuntimeSet` owns a `EventRuleRuntime[FaceCount, 5]`. `RebuildFace(face, snapshot)` replaces only changed definitions and preserves a runtime when the same asset remains equipped. `ExecuteActive(face, slot, signal, services)` returns false when no Rule exists.

`BulletEventRuleServices` delegates spawn, schedule, refill/force, chain, and overlay to the same `BulletEventContext`; returns `Activation.EventBudget`; reads owned projectiles from `Activation.OwnedProjectiles`; records through `Activation.RecordDebug`; reports exceptions through the existing Pipeline logger. Draw/stats methods are no-ops in this active adapter.

- [ ] **Step 5: Route active slots through Rule-or-legacy exactly once**

Add `DiceShotPipeline.ConfigureRuleExecution(Func<int, DiceFaceSlotType, EventRuleDefinition, BulletEventContext, bool> executeRule)`. In the existing trigger method:

```csharp
EventRuleDefinition rule = entry?.Rule ?? context.Activation?.Configuration.GetRule(slotType);
if (rule != null && executeRule != null)
{
    executeRule.Invoke(context.Activation.Face, slotType, rule, context);
    return;
}

TriggerLegacyEffect(entry, slotType, context);
```

Gun owns one runtime set, rebuilds all faces in `Awake`, rebuilds only the changed face in `HandleLoadoutSlotChanged`, and configures Pipeline with the active dispatch delegate.

- [ ] **Step 6: Run active compatibility suites**

Run `EventRuleActiveIntegrationTests`, `DiceFacePassiveSlotTests`, `DiceFaceLoadoutTests`, `DiceShotPipelineTests`, `DiceRevolverGunIntegrationTests`, and `CombatDebugPipelineTests`. Expected: all pass, no duplicate effect execution.

- [ ] **Step 7: Commit Task 3**

```powershell
git add -- Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs.meta Assets/Scripts/Prototype/BulletEventRuleServices.cs Assets/Scripts/Prototype/BulletEventRuleServices.cs.meta Assets/Scripts/Prototype/DiceFaceEntry.cs Assets/Scripts/Prototype/DiceFaceConfiguration.cs Assets/Scripts/Prototype/DiceShotPipeline.cs Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Tests/EditMode/EventRuleActiveIntegrationTests.cs Assets/Tests/EditMode/EventRuleActiveIntegrationTests.cs.meta Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs Assets/Tests/EditMode/DiceShotPipelineTests.cs
git commit -m "接入活动槽事件规则兼容路径"
```

### Task 4: Built-in active modules and nested delayed results

**Files:**
- Create: `Assets/Scripts/Prototype/EventRuleModuleMenuAttribute.cs`
- Create: `Assets/Scripts/Prototype/EventRuleTriggers.cs`
- Create: `Assets/Scripts/Prototype/EventRuleConditions.cs`
- Create: `Assets/Scripts/Prototype/EventRuleResults.cs`
- Modify: `Assets/Scripts/Prototype/EventRuleDefinition.cs`
- Test: `Assets/Tests/EditMode/EventRuleBuiltInModuleTests.cs`

**Interfaces:**
- Produces reusable modules required by core and lightning active-event migrations.
- Consumes Task 2 contexts/services and Task 3 active integration.

- [ ] **Step 1: Write RED tests for the first reusable module set**

Test exact behavior for:

- `SignalTypeTriggerModule`: serialized `EventSignalMask`; matches only included signals.
- `ProjectileTypeConditionModule`: reference identity comparison.
- `ProjectileTagConditionModule`: uses `ProjectileRuntimeStats.HasTag`.
- `AttackEffectConditionModule`: compares `shot.CanTriggerHitEffects`.
- `FaceAvailableConditionModule`: passes only when the requested face is absent from remaining faces.
- `OwnedProjectileCountConditionModule`: compares nearby same-Gun projectile count using `AtLeast`.
- `SpawnProjectileResultModule`: supports explicit definition, current primary definition, hit origin, delay, attack-effect override, and primary flag.
- `ForceFaceResultModule`: calls refill-and-force once.
- `CreateLightningChainResultModule`: selects at most configured count from alive nearby projectiles.
- `QueueActiveOverlayResultModule`: copies only non-empty active slots and excludes Passive.
- `DelayResultModule`: schedules its nested ordered entries and executes them later with the same runtime state and budget.
- `IEventRuleProjectileDefinitionProvider`: implemented by projectile-spawn Results so a Base Rule exposes its primary ProjectileDefinition without executing the Rule.

- [ ] **Step 2: Run built-in module tests and verify RED**

Expected: compile failure because module classes are absent.

- [ ] **Step 3: Add discoverable module metadata**

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventRuleModuleMenuAttribute : Attribute
{
    public EventRuleModuleMenuAttribute(string path) => Path = path;
    public string Path { get; }
}
```

Every concrete module is non-abstract, has the attribute with a stable Chinese menu path, and exposes serialized fields with Chinese `InspectorName` labels. No module uses `FindObjectOfType`, `Resources.FindObjectsOfTypeAll`, singleton access, or reflection.

- [ ] **Step 4: Implement modules through contexts and services only**

`DelayResultModule.Execute` must call `context.ScheduleEntries(Mathf.Max(0f, delaySeconds), entries)`; it must not allocate a new budget or state store. `CreateLightningChainResultModule` reuses `ElectromagneticResonanceEffect.SelectTargets` until Task 9 removes the old class. `EventRuleDefinition.FindPrimaryProjectileDefinition()` walks ordered Results, including nested delayed Results, and returns the first primary `IEventRuleProjectileDefinitionProvider`; validation reports an error when a Base Rule has no primary projectile. All missing references return Skipped with a Debug description, not a null-reference exception.

- [ ] **Step 5: Run built-in and active integration suites**

Run `EventRuleBuiltInModuleTests`, `EventRuleRuntimeTests`, `EventRuleActiveIntegrationTests`, `ElectromagneticResonanceTests`, and `ChainReactionTests`. Expected: all pass.

- [ ] **Step 6: Commit Task 4**

```powershell
git add -- Assets/Scripts/Prototype/EventRuleModuleMenuAttribute.cs Assets/Scripts/Prototype/EventRuleModuleMenuAttribute.cs.meta Assets/Scripts/Prototype/EventRuleTriggers.cs Assets/Scripts/Prototype/EventRuleTriggers.cs.meta Assets/Scripts/Prototype/EventRuleConditions.cs Assets/Scripts/Prototype/EventRuleConditions.cs.meta Assets/Scripts/Prototype/EventRuleResults.cs Assets/Scripts/Prototype/EventRuleResults.cs.meta Assets/Scripts/Prototype/EventRuleDefinition.cs Assets/Tests/EditMode/EventRuleBuiltInModuleTests.cs Assets/Tests/EditMode/EventRuleBuiltInModuleTests.cs.meta
git commit -m "增加可组合活动事件规则模块"
```

### Task 5: Passive signals, draw/stats accumulators, and per-Gun state

**Files:**
- Create: `Assets/Scripts/Prototype/PassiveEventRuleServices.cs`
- Create: `Assets/Scripts/Prototype/EventRulePassiveModules.cs`
- Modify: `Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs`
- Modify: `Assets/Scripts/Prototype/DicePassiveRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs`
- Test: `Assets/Tests/EditMode/DicePassiveRuntimeTests.cs`

**Interfaces:**
- Produces: rule-set methods `FilterDrawCandidates`, `ModifyProjectileStats`, `NotifyProjectileSpawned`, `NotifyProjectileHit`, `NotifyReloadStarted`, `NotifyReloadCompleted`, and `NotifyFaceConsumed`.
- Consumes: existing legacy passive runtime and Gun notifications; both systems coexist and compose deterministically.

- [ ] **Step 1: Write RED tests for passive Rule behavior and state isolation**

Test these boundaries:

1. Rule draw priority is applied after legacy `AllowsDraw`; if either rejects a candidate it is excluded; empty combined candidates fall back to the real chamber pool once with one warning.
2. Rule projectile damage multipliers apply after the legacy passive modifier in stable face/slot order.
3. ProjectileSpawned, ProjectileHit, ReloadStarted, ReloadCompleted, and FaceConsumed signals reach only equipped passive Rule runtimes.
4. Two Guns using the same rule asset have independent counters.
5. Rebuilding one face disposes/clears only that face runtime.
6. A passive-rule exception does not stop a legacy passive or a different face rule.

- [ ] **Step 2: Run passive integration tests and verify RED**

Expected: missing passive dispatch APIs.

- [ ] **Step 3: Implement passive service accumulators**

`PassiveEventRuleServices` receives the current signal and exposes mutable outputs only through the restricted interface: highest draw priority, draw rejection, accumulated projectile damage multiplier, owned-projectile query, and bonus activation callback. It delegates Debug and exceptions to `CombatDebugTrace`/Gun loggers. It never holds static state.

- [ ] **Step 4: Add passive-oriented result modules**

Implement and test:

- `SetDrawPriorityResultModule` with serialized integer priority.
- `MultiplyProjectileDamageFromCounterResultModule` with `counterKey` and non-negative `damagePerStack`.
- `IncrementCounterResultModule`, `ResetCounterResultModule`, and `SetBooleanStateResultModule` keyed inside the current runtime state store.
- `CounterComparisonConditionModule` and `BooleanStateConditionModule`.
- `RequestBonusActivationResultModule` with maximum triggers, spread angle, separation, shared budget, and source activation.
- `SourceFaceConditionModule`, `SameProjectileTypeConditionModule`, and `SignalTypeConditionModule` for multi-signal passive rules.

- [ ] **Step 5: Compose legacy and rule passive paths in Gun**

Gun keeps `DicePassiveRuntime` during migration. For draw filtering, call legacy filtering first and pass its candidates into rule filtering. For stats, apply legacy then Rule. For notifications, invoke both with independent exception boundaries. A passive `DiceFaceEntry` with Rule returns null from `PassiveEffect`, so the same slot never creates both implementations. Resolve a face's Base projectile type from legacy `ProjectileSpawnEffect` first, otherwise from `snapshot.GetRule(DiceFaceSlotType.Base).FindPrimaryProjectileDefinition()`; this keeps Tesla and Echo type checks working for Rule-backed Base entries.

- [ ] **Step 6: Run passive, Gun, lightning, and Debug suites**

Run `EventRulePassiveIntegrationTests`, `DicePassiveRuntimeTests`, `FinisherPassiveTests`, `TeslaPassiveTests`, `EchoSynergyPassiveTests`, `DiceRevolverGunIntegrationTests`, and `CombatDebugPassiveTests`. Expected: all pass.

- [ ] **Step 7: Commit Task 5**

```powershell
git add -- Assets/Scripts/Prototype/PassiveEventRuleServices.cs Assets/Scripts/Prototype/PassiveEventRuleServices.cs.meta Assets/Scripts/Prototype/EventRulePassiveModules.cs Assets/Scripts/Prototype/EventRulePassiveModules.cs.meta Assets/Scripts/Prototype/DiceEventRuleRuntimeSet.cs Assets/Scripts/Prototype/DicePassiveRuntime.cs Assets/Scripts/Prototype/DiceRevolverGun.cs Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs Assets/Tests/EditMode/EventRulePassiveIntegrationTests.cs.meta Assets/Tests/EditMode/DicePassiveRuntimeTests.cs
git commit -m "接入被动事件规则运行时"
```

### Task 6: Validator, TypeCache catalog, and safe SubAsset operations

**Files:**
- Create: `Assets/Scripts/Editor/EventRuleModuleCatalog.cs`
- Create: `Assets/Scripts/Editor/EventRuleValidator.cs`
- Create: `Assets/Scripts/Editor/EventRuleAssetUtility.cs`
- Test: `Assets/Tests/EditMode/EventRuleEditorInfrastructureTests.cs`

**Interfaces:**
- Produces: `EventRuleModuleCatalog.GetModules<T>()`, `EventRuleValidator.Validate`, and asset utility create/add/remove/move/duplicate operations.
- Consumes: UnityEditor `TypeCache`, `AssetDatabase`, `SerializedObject`, and `Undo`; Task 1 model and Task 4/5 module metadata.

- [ ] **Step 1: Write Editor RED tests**

Use temporary assets under `Assets/Tests/TempEventRules/` with teardown deletion. Assert:

- catalog discovers a test module carrying `EventRuleModuleMenuAttribute` without a hardcoded registration;
- rule search finds a saved `EventRuleDefinition` through `AssetDatabase.FindAssets("t:EventRuleDefinition")`;
- adding a module attaches it as a SubAsset and registers Undo;
- removing a module clears the serialized reference/list item before destroying the SubAsset;
- moving ResultEntries uses serialized ordering and survives asset reload;
- duplicating a rule creates new module instances, never shared SubAssets;
- Undo/Redo restores an add/remove/reorder operation;
- validator emits Error for missing Trigger/empty Results/foreign SubAsset/slot conflict, Warning for risky recursion or missing optional service, and Info for a valid legacy-compatible rule;
- validator never mutates the rule or module fields.

- [ ] **Step 2: Run infrastructure tests and verify RED**

Expected: compile failure because Editor infrastructure is absent.

- [ ] **Step 3: Implement TypeCache catalog and deterministic ordering**

Filter abstract/generic types, require the correct base class plus `EventRuleModuleMenuAttribute`, and sort by attribute path then full type name. Return a new read-only list so window callers cannot mutate the cache.

- [ ] **Step 4: Implement non-destructive validator**

Aggregate `EventRuleDefinition.CollectValidationIssues(slot)` with Editor-only ownership/reference checks. Codes are stable strings: `RULE_TRIGGER_MISSING`, `RULE_RESULTS_EMPTY`, `RULE_SLOT_CONFLICT`, `MODULE_REFERENCE_MISSING`, `MODULE_FOREIGN_SUBASSET`, `RULE_RECURSION_RISK`, `PASSIVE_STATE_UNSUPPORTED`, and `SERVICE_UNAVAILABLE`. Concrete modules add missing-reference issues through their virtual `CollectValidationIssues` override; the Editor validator adds SubAsset ownership and AssetDatabase checks. Neither layer writes serialized data.

- [ ] **Step 5: Implement safe asset operations**

All changes use `Undo.RecordObject`, `Undo.RegisterCreatedObjectUndo`, `Undo.DestroyObjectImmediate`, `AssetDatabase.AddObjectToAsset`, `SerializedObject.ApplyModifiedProperties`, `EditorUtility.SetDirty`, and `AssetDatabase.SaveAssets`. Do not edit YAML text directly. Reject an operation when the target rule is null or not saved as an asset.

- [ ] **Step 6: Run Editor infrastructure tests twice**

Run the suite, force `AssetDatabase.Refresh`, and run it again to prove asset reload stability. Expected: both pass and temp folder is absent after teardown.

- [ ] **Step 7: Commit Task 6**

```powershell
git add -- Assets/Scripts/Editor/EventRuleModuleCatalog.cs Assets/Scripts/Editor/EventRuleModuleCatalog.cs.meta Assets/Scripts/Editor/EventRuleValidator.cs Assets/Scripts/Editor/EventRuleValidator.cs.meta Assets/Scripts/Editor/EventRuleAssetUtility.cs Assets/Scripts/Editor/EventRuleAssetUtility.cs.meta Assets/Tests/EditMode/EventRuleEditorInfrastructureTests.cs Assets/Tests/EditMode/EventRuleEditorInfrastructureTests.cs.meta
git commit -m "建立事件规则编辑器资产基础"
```

### Task 7: Three-column Event Rule EditorWindow and Play Mode Debug panel

**Files:**
- Create: `Assets/Scripts/Editor/EventRuleEditorWindow.cs`
- Create: `Assets/Scripts/Editor/EventRuleEditorSelection.cs`
- Create: `Assets/Scripts/Editor/EventRuleDebugSource.cs`
- Create: `Assets/Scripts/Editor/AssemblyInfo.cs`
- Modify: `Assets/Scripts/Prototype/CombatDebugTrace.cs`
- Test: `Assets/Tests/EditMode/EventRuleEditorWindowTests.cs`

**Interfaces:**
- Produces menu item `Window/Dice Revolver/事件规则编辑器` and testable non-visual selection/filter state.
- Consumes Task 6 catalog/validator/assets, `SerializedObject`, and read-only Combat Debug records.

- [ ] **Step 1: Write RED tests for selection, filters, commands, and Debug projection**

Instantiate the window with `EditorWindow.CreateInstance<EventRuleEditorWindow>()`. Assert category filter, tag filter, error-only filter, case-insensitive search, asset selection persistence by GUID, create/duplicate/rename/ping commands, module add/remove, ResultEntry reorder, validation refresh, and Play Mode record projection. `AssemblyInfo.cs` contains `[assembly: InternalsVisibleTo("DiceRevolver.EditMode.Tests")]`; tests call internal methods directly and do not reflect against window internals.

- [ ] **Step 2: Run window tests and verify RED**

Expected: missing window and selection types.

- [ ] **Step 3: Implement three focused units**

- `EventRuleEditorSelection` owns current slot filter, tag, error-only flag, search text, and selected asset GUID; it contains no GUI calls.
- `EventRuleDebugSource` reads the selected live Gun's `CombatDebugTrace` only while `EditorApplication.isPlaying`; it never writes scene state. If no Gun is selected, it returns an empty list and a visible message.
- `EventRuleEditorWindow` renders and delegates state/assets; it does not implement validation or asset mutation logic itself.

- [ ] **Step 4: Implement exact three-column UI behavior**

Use one horizontal root and fixed minimum widths: left `180`, middle `260`, right remaining width with minimum `420`. Left shows Base/OnFire/OnHit/OnFireEnd/Passive, tag popup, error-only toggle. Middle shows search field and rule rows plus New/Duplicate/Rename/Ping buttons. Right draws serialized metadata, allowed slots, Trigger, rule Conditions, ordered ResultEntries with local Conditions and Result, validation issues, references, and Play Mode Debug records.

Module menus are populated only from `EventRuleModuleCatalog`. Result ordering buttons and drag handling call `EventRuleAssetUtility.MoveResult`; public fields render through `EditorGUILayout.PropertyField` so newly discovered module fields require no window code changes.

- [ ] **Step 5: Add granular Debug records without flooding the default HUD**

Extend `CombatDebugEventType` with `RuleTrigger`, `RuleCondition`, and `RuleResult`. Add a `Verbose` flag/property to records generated by condition-level events. Existing `CombatDebugOverlay` ignores verbose records; `EventRuleDebugSource` includes them. Preserve sequence numbers, ChainId, parent activation, and actual delayed execution time.

- [ ] **Step 6: Run window, Debug, and overlay tests**

Run `EventRuleEditorWindowTests`, `EventRuleEditorInfrastructureTests`, `CombatDebugTraceTests`, `CombatDebugOverlayTests`, `CombatDebugPipelineTests`, and `CombatDebugPassiveTests`. Expected: all pass; no scene or Prefab diffs.

- [ ] **Step 7: Commit Task 7**

```powershell
git add -- Assets/Scripts/Editor/EventRuleEditorWindow.cs Assets/Scripts/Editor/EventRuleEditorWindow.cs.meta Assets/Scripts/Editor/EventRuleEditorSelection.cs Assets/Scripts/Editor/EventRuleEditorSelection.cs.meta Assets/Scripts/Editor/EventRuleDebugSource.cs Assets/Scripts/Editor/EventRuleDebugSource.cs.meta Assets/Scripts/Editor/AssemblyInfo.cs Assets/Scripts/Editor/AssemblyInfo.cs.meta Assets/Scripts/Prototype/CombatDebugTrace.cs Assets/Tests/EditMode/EventRuleEditorWindowTests.cs Assets/Tests/EditMode/EventRuleEditorWindowTests.cs.meta
git commit -m "实现三栏事件规则编辑器"
```

### Task 8: Migrate Basic Shot, DoubleTap, BlastRound, and LoadedFour

**Files:**
- Create: `Assets/Scripts/Editor/EventRuleMigrationUtility.cs`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Core/BasicShotRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Core/DoubleTapRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Core/BlastRoundRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Core/LoadedFourRule.asset`
- Modify: matching `Assets/Resources/DiceFacePrototype/DiceFaces/*.asset`
- Modify: `Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs`
- Test: `Assets/Tests/EditMode/EventRuleCoreMigrationTests.cs`
- Test: `Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs`

**Interfaces:**
- Produces four real Rule assets and an idempotent targeted migration utility.
- Consumes Task 4 modules and Task 3 Rule precedence.

- [ ] **Step 1: Record protected asset hashes and write behavioral parity RED tests**

Record SHA256 for Player, TestRobot, TargetDummy, scene, and existing projectile Prefabs before migration. The Player/TestRobot `baseEffects` arrays remain on the legacy `ProjectileSpawnEffect` compatibility path in this plan so protected Prefabs are not rewritten; this task migrates the public DiceFaceLibrary entries. For each old/new pair, execute the same input and assert the same observable request:

- BasicShot: one primary BasicRevolverBullet spawn.
- DoubleTap: one scheduled callback at `0.25` seconds, then one non-primary current-primary spawn with `ForceDisabled` attack effects.
- BlastRound: on hit, one BlastExplosion spawn at hit position; no spawn when attack-effect condition fails.
- LoadedFour: one refill-and-force request for face `4` after OnFireEnd.

Before assets exist, asset tests must fail because Rule references are null.

- [ ] **Step 2: Run migration tests and verify RED**

Expected: behavior module tests pass for legacy controls; new Rule asset/reference assertions fail.

- [ ] **Step 3: Build Rule assets as main assets with module SubAssets**

Use `EventRuleMigrationUtility` and serialized properties, never hand-authored YAML. Preserve display name, description, color, slot type, library membership, and all referenced ProjectileDefinitions. Each migration:

1. creates or loads the named Rule asset;
2. creates missing Trigger/Condition/Result SubAssets by type;
3. copies exact parameter values from the legacy Effect;
4. assigns `DiceFaceEntry.rule`;
5. clears `effect` only after parity tests can load the Rule;
6. is idempotent and leaves designer-edited existing values unchanged.

- [ ] **Step 4: Keep builders compatible and non-destructive**

`DiceFacePrototypeAssetBuilder` creates missing Rule assets and links empty Rule fields only. It must not load or save Player/TestRobot/TargetDummy Prefabs or the scene. Existing legacy asset files remain until Task 10 reference scans prove they are unused.

- [ ] **Step 5: Run core migration and protection suites**

Run `EventRuleCoreMigrationTests`, `EventRuleActiveIntegrationTests`, `BulletEventEffectTests`, `DiceShotPipelineTests`, `DiceRevolverGunIntegrationTests`, `DiceBuildUITests`, and `DiceRevolverCoreAssetMigrationTests`. Recompute protected hashes and assert byte equality.

- [ ] **Step 6: Commit Task 8**

```powershell
git add -- Assets/Scripts/Editor/EventRuleMigrationUtility.cs Assets/Scripts/Editor/EventRuleMigrationUtility.cs.meta Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs Assets/Resources/DiceFacePrototype/EventRules Assets/Resources/DiceFacePrototype/DiceFaces/BasicShot.asset Assets/Resources/DiceFacePrototype/DiceFaces/DoubleTap.asset Assets/Resources/DiceFacePrototype/DiceFaces/BlastRound.asset Assets/Resources/DiceFacePrototype/DiceFaces/LoadedFour.asset Assets/Tests/EditMode/EventRuleCoreMigrationTests.cs Assets/Tests/EditMode/EventRuleCoreMigrationTests.cs.meta Assets/Tests/EditMode/DiceRevolverCoreAssetMigrationTests.cs
git commit -m "迁移核心骰面事件规则"
```

### Task 9: Migrate lightning active and passive builds

**Files:**
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/LightningOrbRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/ElectromagneticResonanceRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/TeslaRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/EchoSynergyRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/ChainReactionRule.asset`
- Create: `Assets/Resources/DiceFacePrototype/EventRules/Lightning/FinisherRule.asset`
- Modify: matching `Assets/Resources/DiceFacePrototype/DiceFaces/*.asset`
- Modify: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- Test: `Assets/Tests/EditMode/EventRuleLightningMigrationTests.cs`
- Test: existing lightning behavior tests.

**Interfaces:**
- Consumes Task 4/5 active/passive modules and Task 8 migration utility.
- Produces all six lightning entries as Rule-backed resources with legacy-observable parity.

- [ ] **Step 1: Write parity RED tests for each lightning Rule**

Use the existing legacy test inputs as controls and assert new Rules produce the same observations:

- LightningOrb: primary LightningOrb ProjectileDefinition spawn.
- ElectromagneticResonance: Lightning-tag primary plus nearby same-Gun orbs selects at most `3` within radius `6` and requests chain definition once.
- Tesla: each Lightning projectile spawn increments the per-runtime counter; bound-face base projectile damage is `base * (1 + stacks * 0.05)`; ReloadStarted resets stacks.
- EchoSynergy: same ProjectileType hit requests bound-face bonus activation at most `4` times per chamber, preserves shared budget/source activation, uses max spread `8` and minimum separation `2`, and deactivates after bound face consumption.
- ChainReaction: queues non-empty active overlay, excludes Passive, consumes itself, and bonus shots do not consume pending overlay.
- Finisher: bound face draw priority is `1`, other faces remain `0`, forced face waits until eligible.

- [ ] **Step 2: Run lightning migration tests and verify RED**

Expected: new Rule asset/reference assertions fail; legacy control tests pass.

- [ ] **Step 3: Create the six Rule assets and copy exact parameters**

Build multi-signal passive Rules using `SignalTypeTriggerModule` masks and local `SignalTypeConditionModule` entries. Store counters/active flags only in `EventRuleStateStore`; never on module assets. Reuse existing Lightning Tag, Lightning Orb type/definition, and LightningChainDefinition references by identity.

- [ ] **Step 4: Update the targeted lightning builder**

The builder may create missing Rule assets/SubAssets and fill empty DiceFaceEntry Rule references. It must retain existing non-empty module parameters, never equip entries on Player/TestRobot, and never load/save protected Prefabs or the scene.

- [ ] **Step 5: Run lightning, passive, Debug, and asset suites**

Run `EventRuleLightningMigrationTests`, `LightningBuildAssetTests`, `LightningProjectileAssetTests`, `ElectromagneticResonanceTests`, `TeslaPassiveTests`, `EchoSynergyPassiveTests`, `ChainReactionTests`, `FinisherPassiveTests`, `CombatDebugPassiveTests`, and `CombatDebugPipelineTests`. Expected: all pass and protected hashes/weapon values remain unchanged.

- [ ] **Step 6: Commit Task 9**

```powershell
git add -- Assets/Resources/DiceFacePrototype/EventRules/Lightning Assets/Resources/DiceFacePrototype/DiceFaces/LightningOrb.asset Assets/Resources/DiceFacePrototype/DiceFaces/ElectromagneticResonance.asset Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset Assets/Resources/DiceFacePrototype/DiceFaces/EchoSynergy.asset Assets/Resources/DiceFacePrototype/DiceFaces/ChainReaction.asset Assets/Resources/DiceFacePrototype/DiceFaces/Finisher.asset Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs Assets/Tests/EditMode/EventRuleLightningMigrationTests.cs Assets/Tests/EditMode/EventRuleLightningMigrationTests.cs.meta
git commit -m "迁移雷电构筑事件规则"
```

### Task 10: Remove unused concrete effects, complete regression, and synchronize context

**Files:**
- Delete only after zero-reference proof: `ExtraShotOnFireEffect`, `ExplosionOnHitEffect`, `ForceFaceFourOnFireEndEffect`, `ElectromagneticResonanceEffect`, `TeslaPassiveEffect`, `EchoSynergyPassiveEffect`, `ChainReactionOnFireEndEffect`, and `FinisherPassiveEffect` scripts/assets with their `.meta` files.
- Modify: `Assets/Scripts/Prototype/BulletEventLibrary.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLibrary.cs`
- Modify: affected Editor builders and tests.
- Modify: `.project-context/project/PROJECT.md`
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/ENVIRONMENT.md`
- Modify: `.project-context/project/workstreams/2026-08-21-event-rule-editor/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-21-event-rule-editor/HANDOFF.md`

**Interfaces:**
- Consumes all completed Rule assets/runtime/editor behavior.
- Produces a recoverable completed workstream, no stale references to the eight removed concrete Effects, an explicitly preserved spawn-effect compatibility boundary, and final regression evidence.

- [ ] **Step 1: Scan references before deletion**

Run `rg` for every migrated concrete type name and GUID across `Assets`. A listed script/asset may be deleted only when all production resources use Rule assets and remaining matches are migration compatibility tests or the file being removed. Long-term compatibility keeps the abstract `BulletEventEffect`, `PassiveEventEffect`, hidden legacy serialized fields, and safe read fallback. It also keeps `ProjectileSpawnEffect` and the three existing spawn-effect assets because protected Player/TestRobot `baseEffects` still reference that concrete compatibility type; record those references as intentional, not stale.

- [ ] **Step 2: Add deletion and library contract tests**

Use `AssetDatabase` and real ScriptableObject references, not YAML/source-text assertions. Assert all DiceFaceLibrary entries have exactly one of Rule/legacy; all migrated library entries use Rule; every Rule module is a SubAsset of its owning Rule; no missing script; libraries contain the same public entries; protected Player/TestRobot legacy Base effects, equipment, and parameters are unchanged.

- [ ] **Step 3: Delete only proven-unused concrete files and update builders/tests**

Remove concrete legacy scripts/assets in one compilable change. Do not delete the abstract compatibility base classes or serialized fallback fields. Replace tests of removed implementations with Rule parity/asset tests; retain behavioral coverage rather than source-shape tests.

- [ ] **Step 4: Run layered focused regression**

Run one filter containing all EventRule model/runtime/active/passive/editor/migration suites plus DiceFace, Pipeline, Gun, lightning, Debug, UI, and protected-asset suites. Expected: every focused test passes, 0 skipped.

- [ ] **Step 5: Run full EditMode regression**

```powershell
& $UnityEditor -batchmode -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults .\Logs\event-rule-editor-full.xml -logFile .\Logs\event-rule-editor-full.log
```

Expected: no new failures; the only allowed failure is the named Ground Y contract. Record the full result as `[failed]`, never `[passed]`, while that exception remains.

- [ ] **Step 6: Verify protected assets and Editor safety**

Recompute Player/TestRobot/TargetDummy/scene/projectile Prefab hashes and weapon values. Verify no protected asset diff, no `TopDownPrototypeSceneBuilder` execution, no module scene search/singleton/reflection usage, and no missing SubAssets. Open the EditorWindow in EditMode through a test and ensure it does not mutate assets merely by drawing.

- [ ] **Step 7: Synchronize portable context truthfully**

Update PROJECT with Rule runtime/editor/data flow; STATUS with the active/completed index and real counts; ENVIRONMENT with the latest full result; STATE/HANDOFF with changed files, exact commands, protected hashes, remaining PlayMode work, and the next action. Mark visible Editor layout and combat feel `[not-run]` unless performed by a human.

- [ ] **Step 8: Validate context and diff**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
git diff --check
git status --short
```

Expected: `[context:ok]`; no whitespace errors; status contains only planned runtime/editor/tests/assets/context changes.

- [ ] **Step 9: Commit Task 10**

```powershell
git add -A -- Assets/Scripts/Prototype Assets/Scripts/Editor Assets/Tests/EditMode Assets/Resources/DiceFacePrototype .project-context/project
git commit -m "完成事件规则编辑器与事件迁移"
```

- [ ] **Step 10: Record manual acceptance honestly**

In visible Unity, open `Window > Dice Revolver > 事件规则编辑器`, verify three-column resizing, filters, create/duplicate/rename, module menus, reorder, Undo/Redo, validation messages, and Play Mode detailed Debug. Equip representative core/lightning Rules temporarily without saving protected Prefabs and verify combat parity. If this session cannot perform the visual review, record every item `[not-run]`; automated completion must not be described as visual acceptance.
