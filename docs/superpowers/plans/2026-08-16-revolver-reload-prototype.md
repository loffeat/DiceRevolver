# Revolver Reload Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the prototype gun with a six-shot revolver that reloads after empty, and fix projectiles spawning or moving in ways that can appear stuck.

**Architecture:** Keep aiming/holding on the weapon script, but move revolver ammo and reload state into a dedicated `RevolverGun` MonoBehaviour. Keep `Projectile` responsible only for launch direction, owner collision ignores, travel, and lifetime.

**Tech Stack:** Unity 6000.3.10f1, C#, UnityEngine, UnityEngine.InputSystem.

## Global Constraints

- Prototype remains graybox and inspector-tunable.
- Revolver capacity is six by default.
- Reload duration is exposed in the Inspector.
- Reloading blocks shooting until the duration completes.
- Bullet movement must not snap to a hardcoded height after spawn.

---

### Task 1: Projectile Reliability

**Files:**
- Modify: `Assets/Scripts/Prototype/Projectile.cs`

**Interfaces:**
- Produces: `Launch(Vector3 launchDirection, Collider ownerCollider = null)`

- [ ] Make projectiles keep their spawn height instead of forcing `y = 0.55`.
- [ ] Ignore collision with the provided owner collider.
- [ ] Keep lifetime despawn behavior.

### Task 2: Revolver Gun

**Files:**
- Create: `Assets/Scripts/Prototype/RevolverGun.cs`

**Interfaces:**
- Consumes: `TopDownPlayerController.AimDirection`
- Produces: inspector fields `capacity`, `reloadDuration`, `shotsPerSecond`, `remainingRounds`

- [ ] Hold and rotate the gun toward the player's aim direction.
- [ ] Fire only when not reloading and rounds remain.
- [ ] Consume one round per shot.
- [ ] Start reload automatically after firing the sixth round.
- [ ] Block firing while reloading and refill rounds after `reloadDuration`.
- [ ] Animate the gun locally during reload as a graybox placeholder.

### Task 3: Scene Builder

**Files:**
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Regenerate: `Assets/Scenes/TopDownShooterPrototype.unity`
- Regenerate: `Assets/PrototypeProjectile.prefab`

**Interfaces:**
- Consumes: `RevolverGun`

- [ ] Attach `RevolverGun` instead of `GunController`.
- [ ] Remove the gun body's collider so bullets cannot hit their own gun at spawn.
- [ ] Regenerate the scene and prefab.

### Task 4: Verification

**Files:**
- Read: Unity batchmode log

- [ ] Run Unity batchmode scene builder.
- [ ] Confirm no `error CS` or script compiler errors appear in the log.
