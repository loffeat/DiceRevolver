# Lightning Build System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add five-slot dice faces, type-safe projectile identities, generic piercing, independent passive runtimes, and the approved lightning build content without changing existing character or gun tuning.

**Architecture:** `DiceRevolverRuntime` remains the chamber mechanic, `DiceShotPipeline` remains the active-event lifecycle, and a new `DicePassiveRuntime` owns per-face passive state and constrained chamber/activation commands. Each Gun owns an `OwnedProjectileRegistry`; electromagnetic resonance delegates line damage and visuals to `LightningChainExecutor`.

**Tech Stack:** Unity 6000.3.10f1, C#, ScriptableObject, Physics overlap queries, LineRenderer, Unity Test Framework EditMode tests.

**Spec:** `docs/superpowers/specs/2026-08-20-lightning-build-system-design.md`

## Global Constraints

- Do not modify Player/TestRobot gun tuning, AimRoot transforms, sorting layers, Ground `Y=-0.01`, character art, or existing six-face equipment.
- Do not run the full scene or Player reconstruction Builder.
- New assets enter libraries but are not automatically equipped.
- Existing serialized fields remain in place; migrations only fill new empty references.
- Lightning chain is not an attack effect and does not publish projectile hits.
- Chain Reaction never copies the passive slot.
- The known Ground contract test may remain the sole full-suite failure; no new failures are allowed.
- Every behavior change follows a verified RED, minimal GREEN, focused regression, and task commit.

---

### Task 1: Five-Slot Faces And Type-Safe Projectile Identity

**Files:**
- Create: `Assets/Scripts/Prototype/PassiveEventEffect.cs`
- Create: `Assets/Scripts/Prototype/PassiveBindingContext.cs`
- Create: `Assets/Scripts/Prototype/IDicePassiveEffectRuntime.cs`
- Create: `Assets/Scripts/Prototype/ProjectileTypeDefinition.cs`
- Create: `Assets/Scripts/Prototype/ProjectileTagDefinition.cs`
- Create: `Assets/Scripts/Prototype/ProjectileTypeLibrary.cs`
- Create: `Assets/Scripts/Prototype/ProjectileTagLibrary.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceSlotType.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildRuntimeView.cs`
- Modify: `Assets/Scripts/Prototype/ProjectileDefinition.cs`
- Modify: `Assets/Scripts/Prototype/ProjectileRuntimeStats.cs`
- Test: `Assets/Tests/EditMode/DiceFacePassiveSlotTests.cs`
- Test: `Assets/Tests/EditMode/ProjectileIdentityTests.cs`
- Test: `Assets/Tests/EditMode/DiceBuildUITests.cs`

**Interfaces:**
- Produces: `DiceFaceSlotType.Passive`.
- Produces: `PassiveEventEffect.CreateRuntime(PassiveBindingContext context): IDicePassiveEffectRuntime` as the factory seam used in Task 3.
- Produces: `ProjectileTypeDefinition`, `ProjectileTagDefinition`, and `ProjectileRuntimeStats.HasTag(ProjectileTagDefinition)`.
- Preserves: legacy `ProjectileRuntimeStats.ProjectileType` and `ProjectileTag` string accessors until all existing callers migrate.

- [x] **Step 1: Write failing five-slot and identity tests**

```csharp
[Test]
public void PassiveEntryDoesNotReplaceFourActiveSlots()
{
    DiceFaceConfiguration face = new();
    face.Equip(Entry(DiceFaceSlotType.Base, activeEffect));
    face.Equip(PassiveEntry(passiveEffect));

    DiceFaceConfigurationSnapshot snapshot = face.CreateSnapshot();

    Assert.That(snapshot.GetEffect(DiceFaceSlotType.Base), Is.SameAs(activeEffect));
    Assert.That(snapshot.GetPassiveEffect(), Is.SameAs(passiveEffect));
}

[Test]
public void RuntimeStatsCompareTypeIdentityAndMultipleTagIdentity()
{
    ProjectileRuntimeStats stats = Definition(type, lightningTag, elementalTag).BuildRuntimeStats();
    Assert.That(stats.ProjectileTypeDefinition, Is.SameAs(type));
    Assert.That(stats.HasTag(lightningTag), Is.True);
    Assert.That(stats.HasTag(elementalTag), Is.True);
}
```

- [x] **Step 2: Run the focused tests and verify RED**

Run Unity EditMode filters `DiceFacePassiveSlotTests;ProjectileIdentityTests;DiceBuildUITests`.

Expected: compile/test failure because Passive slot, passive factory, type identity, and tag collection do not exist.

- [x] **Step 3: Implement the data interfaces and migration-safe fields**

```csharp
public abstract class PassiveEventEffect : ScriptableObject
{
    public abstract IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context);
}

public sealed class ProjectileTypeDefinition : ScriptableObject
{
    [SerializeField, InspectorName("显示名称")] private string displayName;
    public string DisplayName => displayName;
}

public sealed class ProjectileTagDefinition : ScriptableObject
{
    [SerializeField, InspectorName("显示名称")] private string displayName;
    public string DisplayName => displayName;
}
```

Add `passiveEntry` after the existing four serialized entries. Add `projectileTypeDefinition` and `projectileTags` after the existing string fields. `BuildRuntimeStats()` passes both legacy and identity data; no existing numeric field is rewritten.

- [x] **Step 4: Add the fifth UI label and update snapshot helpers**

`DiceBuildFaceSlotUI.SetConfiguration` must call:

```csharp
SetSlotLabel(passiveLabel, DiceFaceSlotType.Passive, configuration);
```

`DiceFaceConfigurationSnapshot.FirstEntry`, `HasAnyEntry`, `FromEntry`, `GetEntry`, and label conversion must include Passive without changing active execution order.

- [x] **Step 5: Run focused tests and verify GREEN**

Expected: all Task 1 filters pass; legacy four-slot tests remain green.

- [x] **Step 6: Commit Task 1**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add passive dice slot and projectile identities"
```

---

### Task 2: Generic Projectile Piercing

**Files:**
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Test: `Assets/Tests/EditMode/ProjectilePiercingTests.cs`
- Modify: `Assets/Tests/EditMode/ProjectileCollisionTests.cs`

**Interfaces:**
- Consumes: `ProjectileRuntimeStats.EnemyPierceCount`.
- Produces: one hit/damage per distinct `IDamageReceiver`, total receiver limit `EnemyPierceCount + 1`.

- [ ] **Step 1: Write failing piercing tests**

```csharp
[Test]
public void PierceTwoDamagesThreeDistinctReceiversBeforeDestroying()
{
    Projectile projectile = CreateProjectile(enemyPierceCount: 2);
    Trigger(projectile, first.Collider);
    Trigger(projectile, second.Collider);
    Trigger(projectile, third.Collider);

    Assert.That(first.HitCount, Is.EqualTo(1));
    Assert.That(second.HitCount, Is.EqualTo(1));
    Assert.That(third.HitCount, Is.EqualTo(1));
    Assert.That(projectile == null, Is.True);
}

[Test]
public void MultipleCollidersOnSameReceiverDoNotConsumePierceTwice()
{
    Trigger(projectile, target.RootCollider);
    Trigger(projectile, target.ChildCollider);
    Assert.That(target.HitCount, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run `ProjectilePiercingTests;ProjectileCollisionTests` and verify RED**

Expected: projectile destroys on first receiver and duplicate receiver protection is absent.

- [ ] **Step 3: Implement receiver identity tracking and pierce consumption**

Use a `HashSet<IDamageReceiver>` and `remainingEnemyPierces`. Ignore already-hit receivers. A new receiver receives Hit broadcast and direct damage; destroy only when `remainingEnemyPierces == 0`, otherwise decrement and continue. A collider with no receiver destroys immediately.

- [ ] **Step 4: Run focused tests and verify GREEN**

Expected: new piercing tests and existing collision-order tests pass.

- [ ] **Step 5: Commit Task 2**

```powershell
git add Assets/Scripts/Prototype/Projectile.cs Assets/Tests/EditMode
git commit -m "feat: implement projectile enemy piercing"
```

---

### Task 3: Passive Runtime And Finisher Draw Rule

**Files:**
- Create: `Assets/Scripts/Prototype/DicePassiveRuntime.cs`
- Modify: `Assets/Scripts/Prototype/PassiveBindingContext.cs`
- Modify: `Assets/Scripts/Prototype/IDicePassiveEffectRuntime.cs`
- Create: `Assets/Scripts/Prototype/FinisherPassiveEffect.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/DicePassiveRuntimeTests.cs`
- Test: `Assets/Tests/EditMode/FinisherPassiveTests.cs`
- Modify: `Assets/Tests/EditMode/DiceRevolverRuntimeTests.cs`

**Interfaces:**
- Produces: `DicePassiveRuntime.RebuildFace(int face, PassiveEventEffect effect)`.
- Produces: `DicePassiveRuntime.FilterDrawCandidates(IReadOnlyList<int> remaining, int? forcedFace): DiceDrawConstraintResult`.
- Produces: `DicePassiveRuntime.NotifyFaceConsumed(int face)` and reload notifications.
- Changes: `DiceRevolverRuntime.TryBeginShot` accepts a candidate-filter callback while retaining fallback to true remaining faces.

- [ ] **Step 1: Write failing independent-instance and Finisher tests**

```csharp
[Test]
public void DuplicatePassiveAssetsCreateIndependentFaceInstances()
{
    runtime.RebuildFace(2, passiveAsset);
    runtime.RebuildFace(5, passiveAsset);
    Assert.That(factory.Contexts.Select(x => x.Face), Is.EqualTo(new[] { 2, 5 }));
    Assert.That(factory.Created[0], Is.Not.SameAs(factory.Created[1]));
}

[Test]
public void ForcedFinisherWaitsUntilOrdinaryFacesAreConsumed()
{
    MarkFinisher(4);
    chamber.ForceNextFace(4);
    Assert.That(DrawFiveShots(), Does.Not.Contain(4));
    Assert.That(chamber.TryBeginShot(time).Face, Is.EqualTo(4));
}
```

- [ ] **Step 2: Run passive/runtime filters and verify RED**

Expected: passive interfaces and candidate filtering are missing.

- [ ] **Step 3: Implement `DicePassiveRuntime` as a pure C# module**

```csharp
public interface IDicePassiveEffectRuntime : IDisposable
{
    bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces);
    void OnReloadStarted();
    void OnReloadCompleted();
    void OnFaceConsumed(int face);
}
```

The runtime catches exceptions per instance, logs through an injected adapter, and leaves candidates unchanged on failure.

- [ ] **Step 4: Implement Finisher filtering and forced-face waiting**

Ordinary faces are candidates while any remain. If only Finishers remain, all remaining Finishers are candidates. A forced Finisher stays pending while ineligible. Empty-filter fallback returns the real remaining list and one warning.

- [ ] **Step 5: Wire Loadout `SlotChanged` and reload lifecycle in Gun**

Only Passive slot changes rebuild an instance. Existing active slot edits do not reset passive state. Reload start/reset calls the passive runtime without adding concrete passive checks to Gun.

- [ ] **Step 6: Run focused tests and verify GREEN**

Expected: passive isolation, immediate replacement, Finisher ordering, multiple Finishers, forced waiting, fallback, and existing six-face tests pass.

- [ ] **Step 7: Commit Task 3**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add passive runtime and finisher draw rule"
```

---

### Task 4: Owned Projectile Registry And Lightning Orb

**Files:**
- Create: `Assets/Scripts/Prototype/OwnedProjectileRegistry.cs`
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Create: `Assets/Tests/EditMode/OwnedProjectileRegistryTests.cs`
- Modify: `Assets/Tests/EditMode/DiceShotPipelineTests.cs`
- Create: `Assets/Tests/EditMode/LightningProjectileAssetTests.cs`

**Interfaces:**
- Produces: `OwnedProjectileRegistry.Register(Projectile projectile, ProjectileRuntimeStats stats)`.
- Produces: `FindNearby(Vector3 center, float radius, ProjectileTagDefinition tag, Projectile exclude, List<ProjectileHandle> results)`.
- Changes: spawn adapter returns `ProjectileHandle`; primary handle is available through `BulletEventContext.PrimaryProjectile`.

- [ ] **Step 1: Write failing registry and primary-handle tests**

Test owner isolation with two Registry instances, radius filtering, tag filtering, exclude-current behavior, and destroyed-reference cleanup. Test that Base spawn completes before OnFire and exposes the actual primary projectile handle.

- [ ] **Step 2: Run registry/pipeline filters and verify RED**

Expected: registry and spawn-result interface do not exist.

- [ ] **Step 3: Implement Registry and synchronous spawn result**

```csharp
public readonly struct ProjectileHandle
{
    public ProjectileHandle(Projectile projectile, ProjectileRuntimeStats stats)
    {
        Projectile = projectile;
        Stats = stats;
    }

    public Projectile Projectile { get; }
    public ProjectileRuntimeStats Stats { get; }
    public bool IsAlive => Projectile != null;
}
```

Each Gun owns one Registry and registers every successful spawn. Do not use a static singleton.

- [ ] **Step 4: Add lightning asset contract tests**

Assert type LightningOrb, tags Lightning and Elemental, damage `1`, speed `5`, distance `15`, pierce `4`, attack effect false, and collider radius `0.35`.

- [ ] **Step 5: Commit Task 4 runtime changes**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: track owned projectiles for lightning builds"
```

---

### Task 5: Tesla Passive Damage Growth

**Files:**
- Create: `Assets/Scripts/Prototype/TeslaPassiveEffect.cs`
- Extend: `Assets/Scripts/Prototype/IDicePassiveEffectRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DicePassiveRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- Test: `Assets/Tests/EditMode/TeslaPassiveTests.cs`

**Interfaces:**
- Produces: `DicePassiveRuntime.ModifyProjectileStats(int sourceFace, ProjectileRuntimeStats stats): ProjectileRuntimeStats`.
- Produces: `DicePassiveRuntime.NotifyProjectileSpawned(int sourceFace, ProjectileHandle projectile)`.

- [ ] **Step 1: Write failing Tesla tests**

```csharp
[Test]
public void LightningProjectileUsesOldStacksThenAddsOneStack()
{
    Assert.That(SpawnFromTeslaFace().Damage, Is.EqualTo(1f));
    SpawnLightningFromOtherFace();
    Assert.That(SpawnFromTeslaFace().Damage, Is.EqualTo(1.05f));
}
```

Also assert two Tesla instances grow independently, non-lightning projectiles do not add stacks, reload clears stacks, and the ScriptableObject definition damage remains unchanged.

- [ ] **Step 2: Run `TeslaPassiveTests` and verify RED**

- [ ] **Step 3: Implement additive per-face modifiers**

Tesla runtime stores `stackCount`. `ModifyProjectileStats` applies `1f + stackCount * damagePerStack` only when source face equals owner face and projectile type equals that face's Base projectile type. `NotifyProjectileSpawned` increments after final stats are captured when the projectile has Lightning tag.

- [ ] **Step 4: Run Tesla and pipeline regressions and verify GREEN**

- [ ] **Step 5: Commit Task 5**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add tesla passive damage growth"
```

---

### Task 6: Electromagnetic Resonance And Lightning Chain

**Files:**
- Create: `Assets/Scripts/Prototype/ElectromagneticResonanceEffect.cs`
- Create: `Assets/Scripts/Prototype/LightningChainDefinition.cs`
- Create: `Assets/Scripts/Prototype/LightningChainExecutor.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventContext.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/ElectromagneticResonanceTests.cs`
- Test: `Assets/Tests/EditMode/LightningChainExecutorTests.cs`

**Interfaces:**
- Produces: `BulletEventContext.RequestLightningChain(ProjectileHandle origin, IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition): bool`.
- `ElectromagneticResonanceEffect` consumes only the primary handle and Registry query interface.
- `LightningChainExecutor` consumes ordered node positions, owner transform, definition, and target layer mask.

- [ ] **Step 1: Write failing selection and damage tests**

Test no-op without Lightning primary, same-Gun selection only, radius `6`, no duplicate node, maximum `3`, deterministic injected random, dead-node skip, whole-chain receiver dedupe, owner immunity, and direct damage `1`.

- [ ] **Step 2: Write a test proving chains do not publish hit effects**

Subscribe to Pipeline OnHit observation, execute a chain through a dummy, and assert direct damage occurs while OnHit count remains zero.

- [ ] **Step 3: Run resonance/chain filters and verify RED**

- [ ] **Step 4: Implement selection and executor modules**

Use ordered random sampling without replacement. Build segment damage with capsule-equivalent distance-to-segment checks over `Physics.OverlapSphere` broad-phase results, dedupe by `IDamageReceiver`, and render all valid segments for `0.2` seconds.

- [ ] **Step 5: Run focused tests and verify GREEN**

- [ ] **Step 6: Commit Task 6**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add electromagnetic lightning chains"
```

---

### Task 7: Echo Synergy Passive And Same-Frame Spread

**Files:**
- Create: `Assets/Scripts/Prototype/EchoSynergyPassiveEffect.cs`
- Create: `Assets/Scripts/Prototype/BonusShotSpreadAllocator.cs`
- Modify: `Assets/Scripts/Prototype/DicePassiveRuntime.cs`
- Modify: `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/EchoSynergyPassiveTests.cs`
- Test: `Assets/Tests/EditMode/BonusShotSpreadAllocatorTests.cs`

**Interfaces:**
- Produces: constrained bonus activation request with face, causal shared budget, suppressed passive instance id, current muzzle pose, and direction.
- Produces: `BonusShotSpreadAllocator.Next(float maxAngle, float minimumSeparation): float` reset once per Unity frame.

- [ ] **Step 1: Write failing Echo behavior tests**

Test matching type triggers immediately, tag-only match does not, maximum four, same passive cannot self-recurse, another face's Echo can respond, normal owner-face shot stops Echo, reload restores count, and child activation shares the causal budget.

- [ ] **Step 2: Write failing spread tests**

With an injected deterministic random sequence, assert all offsets stay within `[-8, 8]` and same-frame accepted offsets differ by at least `2` degrees.

- [ ] **Step 3: Run Echo/spread filters and verify RED**

- [ ] **Step 4: Implement immediate bonus activation and suppression token**

Do not queue through time. Handle the bonus request in the same frame through Pipeline, using the current Gun muzzle pose. Carry the same shared budget object and a suppression token identifying only the originating Echo instance.

- [ ] **Step 5: Run focused tests and verify GREEN**

- [ ] **Step 6: Commit Task 7**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add echo synergy passive shots"
```

---

### Task 8: Chain Reaction Non-Empty Overlay

**Files:**
- Create: `Assets/Scripts/Prototype/ChainReactionOnFireEndEffect.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceActiveOverlay.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- Modify: `Assets/Scripts/Prototype/DiceShotPipeline.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventContext.cs`
- Test: `Assets/Tests/EditMode/ChainReactionTests.cs`
- Modify: `Assets/Tests/EditMode/DiceShotPipelineTests.cs`

**Interfaces:**
- Produces: `DiceShotPipeline.QueueNextShotOverlay(DiceFaceActiveOverlay overlay)`.
- Produces: `DiceFaceConfigurationSnapshot.MergeActiveOverlay(DiceFaceActiveOverlay overlay)`.
- Passive entry is not a member of `DiceFaceActiveOverlay`.

- [ ] **Step 1: Write failing overlay tests**

Use source `[Base=A, OnFire=B, OnHit=null, OnFireEnd=Chain]` and target `[Base=X, OnFire=null, OnHit=Y, OnFireEnd=Z]`; assert final `[A, B, Y, Z]`, target Passive unchanged, actual target face consumed, and Loadout unchanged.

Also assert later non-empty queued overlays win, empty slots never erase, one-shot consumption, and reload clear.

- [ ] **Step 2: Run ChainReaction/Pipeline filters and verify RED**

- [ ] **Step 3: Implement active-only immutable overlay**

The effect builds the overlay after excluding its own OnFireEnd entry. Pipeline merges queued overlays in order immediately before executing the next normal shot, then clears the queue. Bonus activations do not consume the pending normal-shot overlay.

- [ ] **Step 4: Run focused tests and verify GREEN**

- [ ] **Step 5: Commit Task 8**

```powershell
git add Assets/Scripts/Prototype Assets/Tests/EditMode
git commit -m "feat: add chain reaction shot overlays"
```

---

### Task 9: Targeted Assets, Libraries, UI Integration, And Final Verification

**Files:**
- Create: `Assets/Scripts/Editor/LightningBuildPrototypeBuilder.cs`
- Create: `Assets/Prefab/Projectiles/LightningOrb.prefab`
- Create: `Assets/Prefab/Effects/LightningChain.prefab`
- Create: `Assets/Resources/DiceFacePrototype/ProjectileTypes/*.asset`
- Create: `Assets/Resources/DiceFacePrototype/ProjectileTags/*.asset`
- Create: `Assets/Resources/DiceFacePrototype/Projectiles/LightningOrb.asset`
- Create: `Assets/Resources/DiceFacePrototype/ProjectileTypes/ProjectileTypeLibrary.asset`
- Create: `Assets/Resources/DiceFacePrototype/ProjectileTags/ProjectileTagLibrary.asset`
- Create: `Assets/Resources/DiceFacePrototype/Lightning/LightningChainDefinition.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/FireLightningOrbProjectile.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/FinisherPassiveEffect.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/ElectromagneticResonanceEffect.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/TeslaPassiveEffect.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/EchoSynergyPassiveEffect.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/ChainReactionOnFireEndEffect.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/LightningOrb.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/Finisher.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/ElectromagneticResonance.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/Tesla.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/EchoSynergy.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/ChainReaction.asset`
- Modify: `Assets/Resources/DiceFacePrototype/DiceFaceLibrary.asset`
- Modify: `Assets/Resources/DiceFacePrototype/BulletEventLibrary.asset`
- Modify: `Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset`
- Test: `Assets/Tests/EditMode/LightningBuildAssetTests.cs`
- Modify: `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`

**Interfaces:**
- Builder entry: `DiceRevolver.Editor.LightningBuildPrototypeBuilder.Build`.
- Builder is idempotent: existing non-null fields and tuned values are never overwritten.

- [ ] **Step 1: Record protected hashes and gun values**

Record SHA256 for Player, TargetDummy, TestRobot and serialize current Player `shotsPerSecond`, `reloadDuration`, `holdDistance`, `holdHeight` before any Builder execution.

- [ ] **Step 2: Write failing asset and localization tests**

Assert all new assets, default values, type/tag references, prefab components, projectile sorting, fifth UI row, six Library entries, and Chinese Inspector names. Assert Player/TestRobot do not reference new entries.

- [ ] **Step 3: Run asset filters and verify RED**

- [ ] **Step 4: Implement and run only the targeted Builder**

Existing assets return unchanged. New assets receive approved defaults. Library arrays append only missing references. Do not invoke `TopDownPrototypeSceneBuilder`.

- [ ] **Step 5: Run all affected focused tests**

Run all new test classes plus existing Runtime, Pipeline, Gun integration, loadout, UI, projectile collision, explosion, robot asset, and rendering-layer tests. Expected: all focused feature tests pass; the separately identified Ground contract remains the only allowed failure when included.

- [ ] **Step 6: Run the full EditMode suite**

Expected: no new failures. If Ground `Y=-0.01` remains the sole failure, record exact total/pass/fail counts and test identity without calling the suite green.

- [ ] **Step 7: Recheck protected assets and project context**

Hashes and gun values must match Step 1. Run context checker and `git diff --check`.

- [ ] **Step 8: Update context and commit final integration**

Update workstream STATE/HANDOFF, PROJECT, STATUS, ENVIRONMENT when facts changed, and record PlayMode as `not-run` unless visibly tested.

```powershell
git add Assets .project-context docs/superpowers/plans/2026-08-21-lightning-build-system.md
git commit -m "feat: add modular lightning dice builds"
```
