# Target Dummy Damage Numbers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an immobile, non-attacking, infinite-health target dummy that displays one independent world-space floating damage number for every hit.

**Architecture:** `Projectile` talks only to the reusable `IDamageReceiver` protocol using immutable `DamageInfo`. `TargetDummy` publishes received damage without owning health, while `WorldDamageNumberSpawner` translates that event into short-lived UGUI world-space views. A dedicated idempotent editor builder creates only the target prefab and its single scene instance.

**Tech Stack:** Unity `6000.3.10f1`, C#, Unity Physics, UGUI `2.0.0`, Unity Test Framework EditMode tests, NUnit.

## Global Constraints

- Do not modify Player, AimRoot, camera, sorting layers, dice-face assets, gun tuning, or any existing Inspector value.
- Do not call or extend the full `TopDownPrototypeSceneBuilder` scene rebuild path.
- Preserve all unrelated dirty-worktree changes.
- The target never moves, attacks, loses health, dies, or blocks future hits.
- Every accepted hit creates its own world-space number that rises and fades.

---

### Task 1: Reusable Damage Contract and Infinite Target

**Files:**
- Create: `Assets/Scripts/Prototype/DamageInfo.cs`
- Create: `Assets/Scripts/Prototype/IDamageReceiver.cs`
- Create: `Assets/Scripts/Prototype/TargetDummy.cs`
- Create: `Assets/Tests/EditMode/TargetDummyTests.cs`

**Interfaces:**
- Produces: `DamageInfo(float amount, Vector3 hitPosition, GameObject source)`.
- Produces: `void IDamageReceiver.ReceiveDamage(DamageInfo damage)`.
- Produces: `TargetDummy.DamageReceived`, `LastDamage`, and `HitCount`.

- [x] **Step 1: Write failing tests** for event data, repeated hits, and the object remaining alive.
- [x] **Step 2: Run `TargetDummyTests` and verify RED** because the damage types do not exist.
- [x] **Step 3: Implement the minimal immutable data, interface, and event-only target.**
- [x] **Step 4: Run `TargetDummyTests` and verify GREEN.**

### Task 2: Projectile Damage Delivery

**Files:**
- Modify: `Assets/Scripts/Prototype/Projectile.cs`
- Modify: `Assets/Tests/EditMode/TargetDummyTests.cs`

**Interfaces:**
- Consumes: `IDamageReceiver.ReceiveDamage(DamageInfo)`.
- Produces: one damage submission using the projectile's configured `Damage` and collision position.

- [x] **Step 1: Add a failing real-collision-boundary test** invoking the projectile trigger against a child Collider of a target.
- [x] **Step 2: Run the fixture and verify RED** because no damage is delivered.
- [x] **Step 3: Deliver damage before the existing projectile destruction path.**
- [x] **Step 4: Run projectile and target fixtures and verify GREEN.**

### Task 3: World-Space Floating Numbers

**Files:**
- Create: `Assets/Scripts/Prototype/WorldDamageNumber.cs`
- Create: `Assets/Scripts/Prototype/WorldDamageNumberSpawner.cs`
- Modify: `Assets/Tests/EditMode/TargetDummyTests.cs`

**Interfaces:**
- Produces: `WorldDamageNumber.SetDamage(float)` and `DisplayText`.
- Produces: `WorldDamageNumberSpawner.SpawnedCount` and per-hit view creation.

- [x] **Step 1: Add failing tests** proving per-hit creation and damage formatting.
- [x] **Step 2: Run the fixture and verify RED** because the view components do not exist.
- [x] **Step 3: Implement event subscription, view creation, upward motion, fade, and timed destruction.**
- [x] **Step 4: Run the fixture and verify GREEN.**

### Task 4: Target Prefab and Scene Instance

**Files:**
- Create: `Assets/Scripts/Editor/TargetDummyPrototypeBuilder.cs`
- Create: `Assets/Prefab/TargetDummy.prefab`
- Modify: `Assets/Scenes/TopDownShooterPrototype.unity`
- Create: `Assets/Tests/EditMode/TargetDummyAssetTests.cs`

**Interfaces:**
- Produces: menu/batch entry `TargetDummyPrototypeBuilder.BuildTargetDummy()`.
- Produces: one prefab with target, trigger Collider, rigidbody, visual children, and world-space number template.

- [x] **Step 1: Add failing asset tests** for the prefab contract and exactly one scene instance.
- [x] **Step 2: Run asset tests and verify RED** because the prefab is absent.
- [x] **Step 3: Implement and run the dedicated idempotent builder.**
- [x] **Step 4: Run asset tests and verify GREEN.**

### Task 5: Verification and Context Handoff

**Files:**
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-18-target-dummy-damage-numbers/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-18-target-dummy-damage-numbers/HANDOFF.md`

- [x] **Step 1: Run the complete EditMode suite** and inspect XML totals.
- [x] **Step 2: Verify protected Player, aim, sorting, gun, package, and project settings data remain unchanged.**
- [x] **Step 3: Update context with exact verification results and remaining manual PlayMode check.**
- [x] **Step 4: Run `git diff --check` and the project-context checker.**
