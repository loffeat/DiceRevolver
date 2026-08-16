# Dice Revolver Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repackage the revolver as a low-coupling dice revolver module with a six-face random pool and fire/hit/end event seams.

**Architecture:** Keep 3C as a caller that supplies aim and input only. Move dice pool rules into `DiceChamber`, fire state into `DiceRevolverGun`, event payloads into small context types, and projectile hit reporting into a tiny `ProjectileHitReporter` seam.

**Tech Stack:** Unity 6000.3.10f1, C#, UnityEngine, UnityEngine.InputSystem.

## Global Constraints

- Dice faces are 1-6 by default.
- Each shot randomly draws one remaining face, removes it from the pool, and broadcasts fire events.
- Reload resets the random pool to 1-6.
- 3C code must not know dice faces, ammo rules, or bullet effects.
- Bullet effect logic remains empty and will be attached later through event subscriptions.

---

### Task 1: Dice Pool Core

**Files:**
- Create: `Assets/Scripts/Prototype/DiceChamber.cs`

**Interfaces:**
- Produces: `bool TryDrawFace(out int face)`, `void Reset()`, `int RemainingCount`, `IReadOnlyList<int> RemainingFaces`

- [ ] Implement an isolated non-MonoBehaviour dice pool.
- [ ] Draw one random remaining face and remove it.
- [ ] Reset back to 1-6.

### Task 2: Event Payloads

**Files:**
- Create: `Assets/Scripts/Prototype/DiceRevolverShotContext.cs`
- Create: `Assets/Scripts/Prototype/ProjectileHitReporter.cs`

**Interfaces:**
- Produces: `DiceRevolverShotContext` with face, origin, direction, projectile.
- Produces: `ProjectileHitReporter.Hit` event.

- [ ] Store shot data in a small context object.
- [ ] Let projectile hit reporter publish hit collider data without knowing dice logic.

### Task 3: Dice Revolver Controller

**Files:**
- Create: `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- Keep: `Assets/Scripts/Prototype/RevolverGun.cs` as old prototype script for reference.

**Interfaces:**
- Produces: C# events `FireStarted`, `ProjectileHit`, `FireEnded`, `ReloadStarted`, `ReloadCompleted`.
- Consumes: `TopDownPlayerController.AimDirection`, `Projectile`, `ProjectileHitReporter`.

- [ ] Move hold pose, shooting cooldown, ammo, reload, dice draw, and event broadcast into `DiceRevolverGun`.
- [ ] Instantiate projectile, attach hit reporter, and bridge hit events back to the shot context.
- [ ] Block firing during reload.
- [ ] Reload when pool is empty or manual reload input is pressed.

### Task 4: Scene Builder And Verification

**Files:**
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Regenerate: `Assets/Scenes/TopDownShooterPrototype.unity`

**Interfaces:**
- Scene uses `DiceRevolverGun` on `Player/GunPivot`.

- [ ] Update scene builder to attach `DiceRevolverGun`.
- [ ] Run Unity batchmode compile and scene generation.
- [ ] Confirm no C# compiler errors and no old `GunController` reference in the generated scene.
