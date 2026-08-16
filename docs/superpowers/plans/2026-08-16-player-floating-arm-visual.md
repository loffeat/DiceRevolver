# Player Floating Arm Visual Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repackage the prototype player with `Player_left` body animation and a separate floating arm that orbits toward the mouse like Enter the Gungeon.

**Architecture:** Keep 3C and dice revolver gameplay logic intact. Add visual-only bridge modules that read player movement/aim data and update Animator, SpriteRenderer flipping, arm position, and render order without changing weapon rules.

**Tech Stack:** Unity 6000.3.10f1, C#, SpriteRenderer, Animator, existing `Player_Left.controller`.

## Global Constraints

- Use `Assets/Art/Player/Player_left` and `Assets/Art/PlayerAnimation/Player_Left.controller`.
- Use `Assets/Art/Player/Player_left/蛙手.png` as the floating arm sprite.
- Body visual must not rotate with mouse aim.
- Arm visual must orbit around the body and mirror when aiming across left/right sides.
- Existing WASD, left-click shooting, dice revolver, and ammo UI must keep working.

---

### Task 1: Visual Bridge Scripts

**Files:**
- Modify: `Assets/Scripts/Prototype/TopDownPlayerController.cs`
- Create: `Assets/Scripts/Prototype/PlayerMovementAnimatorBridge.cs`
- Create: `Assets/Scripts/Prototype/FloatingArmAimVisual.cs`

**Interfaces:**
- `TopDownPlayerController` exposes `MoveInput`, `IsMoving`, and configurable body rotation.
- `PlayerMovementAnimatorBridge` writes Animator bool `isWalking`.
- `FloatingArmAimVisual` mirrors body/arm sprites and render order from `AimDirection`.

### Task 2: Scene Builder Visual Packaging

**Files:**
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Regenerate: `Assets/Scenes/TopDownShooterPrototype.unity`

**Interfaces:**
- Scene creates `Player/PlayerVisual` and `Player/GunPivot/ArmVisual`.
- `GunPivot` still hosts `DiceRevolverGun`.

### Task 3: Verification

**Files:**
- Read Unity batchmode log.

- [ ] Run Unity batchmode scene builder.
- [ ] Confirm no C# compile errors.
- [ ] Confirm generated scene contains `PlayerVisual`, `ArmVisual`, `FloatingArmAimVisual`, and `Player_Left.controller`.
