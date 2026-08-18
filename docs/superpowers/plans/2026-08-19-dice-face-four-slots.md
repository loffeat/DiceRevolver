# Dice Face Four Slots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every dice face one independent Base, On Fire, On Hit, and On Fire End event slot without overwriting the other slots.

**Architecture:** A typed `DiceFaceConfiguration` owns four single-entry slots and produces an immutable snapshot for each activation. `DiceFaceLoadout` keeps six configurations while reading the existing serialized arrays as compatibility fallbacks, and gun/UI consumers switch from one composite entry to typed slots.

**Tech Stack:** Unity 6000.3.10f1, C#, ScriptableObject, UGUI, Unity Test Framework EditMode tests.

**Spec:** `docs/superpowers/specs/2026-08-19-dice-face-four-slots-design.md`

## Global Constraints

- Do not modify Player Prefab AimRoot, ArmVisual, GunBody, Muzzle transforms or sorting.
- Do not modify existing `DiceRevolverGun` tuning values.
- Do not run the full scene or Player Prefab builder.
- Preserve existing serialized `entries` and `baseEffects` as compatibility data.
- Every production behavior change follows a failing EditMode test.

---

### Task 1: Typed Four-Slot Data Model

**Files:**
- Create: `Assets/Scripts/Prototype/DiceFaceSlotType.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceConfiguration.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Test: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`

**Interfaces:**
- Produces: `DiceFaceSlotType`, `DiceFaceConfiguration.Equip(DiceFaceEntry)`, `DiceFaceConfiguration.GetEntry(DiceFaceSlotType)`, `DiceFaceConfigurationSnapshot.GetEffect(DiceFaceSlotType)`, `DiceFaceLoadout.GetSnapshot(int)`.

- [x] Add failing tests proving four different slot entries coexist, replacing one slot preserves the other three, invalid faces are ignored, and a captured snapshot is stable.
- [x] Run the focused `DiceFaceLoadoutTests` and confirm the new API is missing.
- [x] Add the enum, single-slot entry metadata, configuration, snapshot, and six-face loadout APIs.
- [x] Keep hidden legacy arrays and resolve existing one-stage entries/base effects as fallbacks without writing the Player Prefab.
- [x] Run the focused tests and confirm they pass.

### Task 2: Activation and Gun Trigger Pipeline

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Test: `Assets/Tests/EditMode/DiceFaceActivationTests.cs`
- Test: `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`

**Interfaces:**
- Consumes: `DiceFaceConfigurationSnapshot` from Task 1.
- Produces: `DiceFaceActivation.Configuration`, shot contexts that retain the activation snapshot, and phase-specific effect dispatch.

- [x] Add failing tests proving Base and On Fire both trigger during firing, On Fire End triggers afterward, and On Hit uses the captured snapshot.
- [x] Run the focused activation/integration tests and confirm failure is caused by the single-entry pipeline.
- [x] Replace activation/shot-context entry storage with the configuration snapshot and migrate existing constructor callers.
- [x] Trigger exactly one effect from each relevant slot through the existing event budget, scheduler, and exception boundary.
- [x] Run the focused tests and confirm they pass.

### Task 3: Four-Slot Build UI

**Files:**
- Modify: `Assets/Scripts/Prototype/DiceBuildPageUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildFaceSlotUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildEntryButtonUI.cs`
- Modify: `Assets/Scripts/Prototype/DiceBuildRuntimeView.cs`
- Test: `Assets/Tests/EditMode/DiceBuildUITests.cs`

**Interfaces:**
- Consumes: typed entries and `DiceFaceLoadout.Equip(int, DiceFaceEntry)`.
- Produces: a face tile showing four labels and entry buttons showing their slot type.

- [x] Add failing UI tests that equip Base, On Fire, On Hit, and On Fire End entries to one face and assert all four remain visible and stored.
- [x] Run `DiceBuildUITests` and confirm failure reflects the old single-label tile.
- [x] Update UI binding and runtime-created controls to render four stable rows and route selection to the entry's typed slot.
- [x] Update change subscriptions to include the changed slot while refreshing only the affected face.
- [x] Run focused UI tests and confirm they pass.

### Task 4: Prototype Asset Migration

**Files:**
- Modify: `Assets/Scripts/Editor/DiceFacePrototypeAssetBuilder.cs`
- Modify: `Assets/Resources/DiceFacePrototype/DiceFaceLibrary.asset`
- Create: `Assets/Resources/DiceFacePrototype/DiceFaces/BasicShot.asset`
- Test: `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`
- Test: `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`

**Interfaces:**
- Consumes: `DiceFaceEntry` typed slot fields.
- Produces: four library entries mapped to the four prototype effects.

- [x] Add failing resource tests for BasicShot and the slot/effect mapping of all four entries.
- [x] Run focused asset tests and confirm BasicShot or typed mappings are absent.
- [x] Make the asset builder append/create missing prototype assets without overwriting already configured user fields.
- [x] Create/migrate only the four controlled prototype entry assets and append BasicShot to the library.
- [x] Run focused asset and localization tests and confirm they pass.

### Task 5: Regression and Context Closure

**Files:**
- Modify: `.project-context/project/PROJECT.md`
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-19-dice-face-four-slots/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-19-dice-face-four-slots/HANDOFF.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: verified project state and reproducible handoff.

- [x] Run the focused four-slot EditMode tests and record exact counts.
- [x] Run the complete EditMode suite and require zero failures/errors.
- [x] Verify protected Player Prefab local transforms, sorting, and gun tuning were not rewritten by this task.
- [x] Update project context with the four-slot terminology, data flow, files, and actual verification results.
- [x] Run `.project-context/framework/scripts/check.ps1` and require `[context:ok]`.
