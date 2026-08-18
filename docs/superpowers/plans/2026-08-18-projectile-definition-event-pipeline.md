# Projectile Definition And Event Pipeline Implementation Plan

> **For Codex:** Execute this plan in the current session. Preserve user-authored prefab transforms, sorting data, and `DiceRevolverGun` tuning values.

**Goal:** Move projectile gameplay data into reusable ScriptableObjects and route every dice-face activation through independent base/on-fire/on-hit/on-fire-end modules.

**Architecture:** A `DiceFaceActivation` owns one trigger's immutable face snapshot, projectile spawn requests, hit-event relationship, scheduler access, and event budget. `ProjectileSpawnEffect` is the generic base-event module. Projectile instances receive all runtime attributes from `ProjectileDefinition`, while attack-effect policy decides whether that specific projectile may dispatch the activation's on-hit modules.

**Tech Stack:** Unity 6, C#, ScriptableObject, NUnit EditMode tests, Unity Prefab/AssetDatabase editor APIs.

---

### Task 1: Lock The New Data Contracts With Tests

**Files:**
- Modify: `Assets/Tests/EditMode/DiceFaceLoadoutTests.cs`
- Create: `Assets/Tests/EditMode/ProjectileDefinitionTests.cs`
- Create: `Assets/Tests/EditMode/DiceFaceActivationTests.cs`

1. Add failing tests proving projectile runtime stats come exclusively from `ProjectileDefinition`.
2. Add failing tests proving each loadout face owns an independent base-event slot.
3. Add failing tests for primary, default, forced-enabled, and forced-disabled hit-event permission.
4. Add a failing test proving an activation stops accepting effects and spawns after its budget is exhausted.
5. Run the focused EditMode suite and record the expected compile/test failures.

### Task 2: Implement The Runtime Event Pipeline

**Files:**
- Create: `Assets/Scripts/Prototype/ProjectileDefinition.cs`
- Create: `Assets/Scripts/Prototype/ProjectileDefinitionLibrary.cs`
- Create: `Assets/Scripts/Prototype/AttackEffectOverride.cs`
- Create: `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- Create: `Assets/Scripts/Prototype/ProjectileSpawnEffect.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceEntry.cs`
- Modify: `Assets/Scripts/Prototype/DiceFaceLoadout.cs`
- Modify: `Assets/Scripts/Prototype/BulletEventContext.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- Modify: `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`
- Modify: `Assets/Scripts/Prototype/ExplosionOnHitEffect.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`

1. Implement projectile definitions and the attack-effect override rule.
2. Implement a per-trigger activation context with a default event budget of 32.
3. Make base, on-fire, on-hit, and on-fire-end effects dispatch independently.
4. Route spawned projectile configuration through its definition.
5. Keep legacy public helpers only where existing callers still require them; do not retain the old face-owned projectile-data path.
6. Run focused tests until green.

### Task 3: Generate The Basic Revolver Projectile Assets

**Files:**
- Create: `Assets/Editor/ProjectileDefinitionPrototypeBuilder.cs`
- Create: `Assets/Prefab/Projectiles/BasicRevolverBullet.prefab`
- Create: `Assets/Resources/DiceFacePrototype/Projectiles/BasicRevolverBullet.asset`
- Create: `Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset`
- Modify: `Assets/Prefab/Player.prefab`

1. Create a runtime projectile wrapper with collision and projectile components.
2. Nest `Assets/Art/Effect/perfab/fire_1.prefab` as the visual child without modifying the source prefab.
3. Create the basic revolver definition and one-item definition library.
4. Create the base spawn event and bind it to all six loadout base slots.
5. Verify the builder only touches these approved assets and the new loadout field.

### Task 4: Verify Integration And Regression Safety

**Files:**
- Modify: `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`
- Modify: `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`
- Create: `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`

1. Prove every dice face resolves the same base projectile event.
2. Prove DoubleTap schedules a second basic projectile at 0.25 seconds and defaults to no hit-event dispatch.
3. Prove the primary projectile can independently trigger BlastRound and LoadedFour remains fire-end only.
4. Run the full EditMode suite in an isolated temporary Unity project.
5. Run PlayMode or a deterministic scene smoke check for visual/collision integration.
6. Confirm protected prefab transforms, sorting data, and revolver tuning values were not changed.

### Task 5: Synchronize Project Context

**Files:**
- Modify: `.project-context/project/workstreams/2026-08-18-projectile-definition-event-pipeline/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-18-projectile-definition-event-pipeline/HANDOFF.md`
- Modify: `.project-context/project/STATUS.md`

1. Record exact implementation and verification outcomes.
2. Run `.project-context/framework/scripts/check.ps1`.
3. Leave any visual-only manual checks explicitly listed instead of claiming they ran.
