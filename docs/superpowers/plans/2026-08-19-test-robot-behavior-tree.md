# Test Robot Behavior Tree Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a test robot enemy that uses a behavior tree to maintain combat distance, strafe, continuously aim, and fire zero-damage basic revolver bullets.

**Architecture:** A shared top-down character controller consumes control intent from either player input or robot AI. A pure C# combat brain hides a small behavior tree behind one tick interface, while a Unity adapter binds its decisions to the existing aim rig, animation bridge, dice-face loadout, and revolver gun.

**Tech Stack:** Unity 6000.3.10f1, C#, Input System, ScriptableObject, Unity Test Framework EditMode tests.

**Spec:** `docs/superpowers/specs/2026-08-19-test-robot-behavior-tree-design.md`

## Global Constraints

- Do not resave or modify `Assets/Prefab/Player.prefab` or `Assets/Prefab/TargetDummy.prefab`.
- Do not change player AimRoot transforms, sorting, body scale/height, or DiceRevolverGun tuning.
- Do not run `TopDownPrototypeSceneBuilder`.
- Preserve existing shared-library entries and append only missing robot assets.
- Every production behavior change follows a failing EditMode test.

---

### Task 1: Pure Behavior Tree And Combat Brain

**Files:**
- Create: `Assets/Scripts/Prototype/BehaviorTree.cs`
- Create: `Assets/Scripts/Prototype/TestRobotCombatBrain.cs`
- Test: `Assets/Tests/EditMode/TestRobotBehaviorTreeTests.cs`

**Interfaces:**
- Produces: `IBehaviorNode<TContext>.Tick(TContext)`, standard composite/leaf nodes, `TestRobotCombatBrain.Tick(Vector3, Vector3, float)`, and `TestRobotDecision`.

- [ ] Write tests for sequence/selector/parallel semantics and robot approach, retreat, strafe, strafe switching, aim, and fire.
- [ ] Run focused tests and verify failure is caused by missing production types.
- [ ] Implement the minimal behavior-tree nodes and combat brain.
- [ ] Run focused tests and require zero failures.

### Task 2: Shared Character Intent Seam

**Files:**
- Create: `Assets/Scripts/Prototype/TopDownCharacterController.cs`
- Modify: `Assets/Scripts/Prototype/TopDownPlayerController.cs`
- Modify: `Assets/Scripts/Prototype/TopDownAimHandRig.cs`
- Modify: `Assets/Scripts/Prototype/PlayerMovementAnimatorBridge.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Create: `Assets/Scripts/Prototype/TestRobotController.cs`
- Test: `Assets/Tests/EditMode/TopDownCharacterControllerTests.cs`

**Interfaces:**
- Produces: shared `AimWorldPoint`, `AimDirection`, `MoveInput`, `IsMoving`, `FireHeld`, and `ReloadPressedThisFrame` interface.
- Consumes: `TestRobotCombatBrain` decisions from Task 1.

- [ ] Write tests proving player/controller references work through the shared type and robot intent drives the common motor state.
- [ ] Run focused tests and verify the shared types/ports are absent.
- [ ] Extract existing motor behavior into the shared base while retaining serialized field names.
- [ ] Make player and robot adapters supply their respective control intent.
- [ ] Change aim, animator, and gun consumers to the shared type without resaving Player Prefab.
- [ ] Run focused and existing aim/gun tests.

### Task 3: Robot Projectile And Prefab Packaging

**Files:**
- Create: `Assets/Scripts/Editor/TestRobotPrototypeBuilder.cs`
- Create: `Assets/Prefab/TestRobot.prefab`
- Create: `Assets/Resources/DiceFacePrototype/Projectiles/TestRobotRevolverBullet.asset`
- Create: `Assets/Resources/DiceFacePrototype/BulletEvents/FireTestRobotRevolverProjectile.asset`
- Modify: `Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset`
- Modify: `Assets/Resources/DiceFacePrototype/BulletEventLibrary.asset`
- Modify: `Assets/Scenes/TopDownShooterPrototype.unity`
- Test: `Assets/Tests/EditMode/TestRobotAssetTests.cs`

**Interfaces:**
- Produces: a packaged `TestRobot.prefab`, zero-damage projectile definition/spawn effect, and one prototype-scene instance.

- [ ] Write asset tests for prefab wiring, infinite-health target behavior, six base effects, zero damage, shared projectile prefab, and scene instance.
- [ ] Run focused tests and verify the missing assets fail.
- [ ] Implement a targeted append-only builder that clones TargetDummy and wires robot-only modules.
- [ ] Run the builder in an isolated Unity project and copy only generated/targeted assets back.
- [ ] Run focused asset tests and require zero failures.

### Task 4: Regression And Context Closure

**Files:**
- Modify: `.project-context/project/PROJECT.md`
- Modify: `.project-context/project/STATUS.md`
- Modify: `.project-context/project/workstreams/2026-08-19-test-robot-behavior-tree/STATE.md`
- Modify: `.project-context/project/workstreams/2026-08-19-test-robot-behavior-tree/HANDOFF.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: verified repository state and reproducible handoff.

- [ ] Run focused AI, shared-controller, and robot-asset tests.
- [ ] Run the complete EditMode suite and require zero failures/errors.
- [ ] Verify protected Player/TargetDummy Prefab hashes and player gun tuning match pre-task values.
- [ ] Update project context with behavior-tree terminology, ports, generated assets, and exact verification counts.
- [ ] Run `.project-context/framework/scripts/check.ps1` and require `[context:ok]`.

