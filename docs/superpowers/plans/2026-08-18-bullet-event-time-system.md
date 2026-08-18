# Bullet Event Time System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable scaled-game-time scheduler for bullet events, make Double Tap fire its second projectile after an adjustable default delay of `0.25` seconds, and localize core combat Inspector ports without changing existing serialized values.

**Architecture:** `BulletEventTimeScheduler` is a deterministic plain C# queue that receives explicit time values. `DiceRevolverGun` owns and ticks it with `Time.time`; `BulletEventContext` exposes scheduling so event ScriptableObjects never depend on the queue or coroutines. Inspector localization uses metadata attributes only, preserving field names and serialized data.

**Tech Stack:** Unity `6000.3.10f1`, C#, Unity Test Framework EditMode tests, NUnit, UGUI/Unity serialization attributes.

## Global Constraints

- Use scaled game time; `Time.timeScale = 0` pauses delayed events.
- Do not add a global singleton, coroutine dependency, repeating timer, cancellation API, or persistence.
- Do not modify player transforms, aiming, camera, sorting, Prefab layers, or existing gun tuning values.
- Preserve all serialized field names; Chinese Inspector labels are metadata only.
- Delayed additional shots reuse the original shot context and cannot recursively trigger on-fire effects.
- Do not overwrite existing user changes in the dirty worktree.

---

### Task 1: Deterministic Bullet Event Scheduler

**Files:**
- Create: `Assets/Scripts/Prototype/BulletEventTimeScheduler.cs`
- Create: `Assets/Tests/EditMode/BulletEventTimeSchedulerTests.cs`

**Interfaces:**
- Produces: `bool Schedule(float now, float delaySeconds, Action callback)`
- Produces: `void Tick(float now, Action<Exception> exceptionHandler = null)`
- Produces: `void Clear()` and `int PendingCount`

- [x] **Step 1: Write failing scheduler tests**

Add tests with hand-authored times proving: no execution at `10.24` for a `10.00 + 0.25` task, one execution at `10.25`, FIFO order for equal due times, exception isolation, and tasks scheduled during `Tick` waiting for the next `Tick`.

- [x] **Step 2: Run the scheduler fixture and verify RED**

Run Unity EditMode with `-testFilter "DiceRevolver.Tests.BulletEventTimeSchedulerTests"`.

Expected: compilation fails because `BulletEventTimeScheduler` does not exist.

- [x] **Step 3: Implement the minimal scheduler**

Use a private scheduled-item record containing due time, monotonically increasing sequence, and callback. Keep the queue sorted by due time then sequence. Remove the due snapshot before invoking callbacks, clamp negative delay to zero, reject null callbacks, and isolate callback exceptions through the supplied handler.

- [x] **Step 4: Run the scheduler fixture and verify GREEN**

Expected: all scheduler tests pass with no unexpected logs.

### Task 2: Context Scheduling and Delayed Double Tap

**Files:**
- Modify: `Assets/Scripts/Prototype/BulletEventContext.cs`
- Modify: `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`
- Modify: `Assets/Tests/EditMode/BulletEventEffectTests.cs`
- Modify: `Assets/Resources/DiceFacePrototype/BulletEvents/ExtraShotOnFireEffect.asset`

**Interfaces:**
- Consumes: scheduler callback supplied as `Action<float, Action>`.
- Produces: `bool BulletEventContext.Schedule(float delaySeconds, Action<BulletEventContext> callback)`.
- Produces: `float ExtraShotOnFireEffect.DelaySeconds` and serialized `delaySeconds = 0.25f`.

- [x] **Step 1: Replace the immediate extra-shot expectation with failing delayed behavior tests**

Verify that triggering Double Tap submits one task with literal delay `0.25`, does not request a shot immediately, and requests exactly one shot when the captured callback is invoked. Keep the recursion-blocked test and add a missing-scheduler test returning safely.

- [x] **Step 2: Run `BulletEventEffectTests` and verify RED**

Expected: compilation fails because the Context scheduling interface and effect delay property are absent.

- [x] **Step 3: Implement Context scheduling and update Extra Shot**

Append an optional scheduling delegate to the Context constructor. Copy the readonly Context before capturing it. Add `[SerializeField]`, `[Min(0f)]`, Chinese Inspector metadata, and the `0.25f` default to `ExtraShotOnFireEffect`; trigger `Schedule` rather than immediate `RequestAdditionalShot`.

- [x] **Step 4: Persist the existing SO value**

Add `delaySeconds: 0.25` to `ExtraShotOnFireEffect.asset` without changing its GUID or other resource fields.

- [x] **Step 5: Run `BulletEventEffectTests` and verify GREEN**

Expected: all effect tests pass.

### Task 3: Dice Revolver Time Driver

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Modify: `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`

**Interfaces:**
- Consumes: `BulletEventTimeScheduler.Schedule(Time.time, delay, callback)` and `Tick(Time.time, Debug.LogException)`.
- Produces: event Contexts capable of scheduling delayed callbacks.

- [x] **Step 1: Add a failing gun scheduling integration test**

Construct a gun and event Context through the real gun boundary or a narrowly reflected private factory, schedule a callback, tick before and at the due time, and assert one execution. The test must fail because the gun does not yet wire a scheduler delegate.

- [x] **Step 2: Run `DiceRevolverGunIntegrationTests` and verify RED**

Expected: the new delayed callback is never accepted or executed.

- [x] **Step 3: Wire scheduler ownership into the gun**

Create one scheduler per gun, pass its schedule delegate from `CreateEventContext`, tick after `TryFire` in `LateUpdate`, log isolated exceptions, and clear pending callbacks in `OnDestroy`. Do not gate scheduled callbacks on reload state.

- [x] **Step 4: Run gun integration tests and verify GREEN**

Expected: all gun integration tests pass, including projectile-to-projectile collision regressions.

### Task 4: Chinese Inspector Ports for Core Combat

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLibrary.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventLibrary.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Test: `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`

**Interfaces:**
- Produces: Chinese `SerializedProperty.displayName` values for every core combat field listed in the design.

- [x] **Step 1: Add failing Inspector display-name tests**

Use `SerializedObject.FindProperty(fieldName).displayName` and literal Chinese expectations for representative fields from every covered type, including `shotsPerSecond`, `reloadDuration`, `damage`, `flightSpeed`, `explosionProjectilePrefab`, and library/loadout arrays.

- [x] **Step 2: Run localization tests and verify RED**

Expected: current generated English display names do not match Chinese literals.

- [x] **Step 3: Add Chinese metadata only**

Add `[InspectorName]`, Chinese `[Header]`, and concise `[Tooltip]` attributes. Do not rename fields, change initializers, run asset builders, or edit Prefab/scene YAML.

- [x] **Step 4: Run localization tests and verify GREEN**

Expected: all covered fields report the required Chinese display names.

### Task 5: Full Verification and Context Handoff

**Files:**
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-18-bullet-event-time-system/STATE.md`
- Rewrite: `.project-context/project/workstreams/2026-08-18-bullet-event-time-system/HANDOFF.md`

- [x] **Step 1: Run complete Unity EditMode tests**

Run the full EditMode suite and inspect the XML `<test-run>` totals. Record `passed` only when failed count is zero.

- [x] **Step 2: Verify protected data**

Confirm `Player.prefab`, `PrototypeProjectile.prefab`, `Packages/`, and `ProjectSettings/` are unchanged. Check that existing serialized gun values remain intact and only the new `delaySeconds` field changed the event asset.

- [x] **Step 3: Update project context**

Mark the workstream `completed`, record RED/GREEN/full-suite results, list modified files, and leave PlayMode visual verification as `not-run` unless actually executed.

- [x] **Step 4: Run final validation**

Run `git diff --check` and `.project-context/framework/scripts/check.ps1`. Ensure no Unity test process remains.
