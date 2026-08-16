# Top-Down Shooter Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Unity graybox prototype where the player moves with WASD, aims at the mouse, holds a gun, and fires projectiles with left click.

**Architecture:** Use small MonoBehaviours with one responsibility each: player movement/aiming, gun handling/firing, projectile travel, and camera follow. A Unity editor builder creates a runnable prototype scene from primitives so the scene can be regenerated.

**Tech Stack:** Unity 6000.3.10f1, C#, UnityEngine, UnityEngine.InputSystem, Universal Render Pipeline.

## Global Constraints

- Keep this prototype graybox and throwaway.
- Use WASD for movement and left mouse button for firing.
- Aim and gun direction follow the mouse cursor on the ground plane.
- Avoid new package dependencies.

---

### Task 1: Runtime Prototype Behaviours

**Files:**
- Create: `Assets/Scripts/Prototype/TopDownPlayerController.cs`
- Create: `Assets/Scripts/Prototype/GunController.cs`
- Create: `Assets/Scripts/Prototype/Projectile.cs`
- Create: `Assets/Scripts/Prototype/PrototypeCameraFollow.cs`

**Interfaces:**
- Produces: `TopDownPlayerController.AimDirection`, `TopDownPlayerController.AimWorldPoint`
- Consumes: `GunController` reads player transform and mouse input through the controller.

- [ ] **Step 1: Create player controller**

Read WASD from `Keyboard.current`, move in the XZ plane with a `CharacterController`, raycast the mouse cursor onto a flat ground plane, and rotate the player toward the aim direction.

- [ ] **Step 2: Create gun controller**

Position a gun transform at an offset from the player along `AimDirection`, rotate it toward the cursor, and fire from a muzzle transform when `Mouse.current.leftButton` is pressed and cooldown allows.

- [ ] **Step 3: Create projectile**

Move forward at a fixed speed, keep a fixed Y height, and destroy after a lifetime or collision.

- [ ] **Step 4: Create camera follow**

Follow the player from an orthographic top-down angle with a small look-ahead toward the mouse aim point.

### Task 2: Scene Builder

**Files:**
- Create: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`
- Create by running builder: `Assets/Scenes/TopDownShooterPrototype.unity`

**Interfaces:**
- Consumes: all runtime scripts from Task 1.
- Produces: a playable scene named `TopDownShooterPrototype`.

- [ ] **Step 1: Create graybox scene setup**

Create ground, player, gun, muzzle, camera, light, a few obstacle blocks, and a text label.

- [ ] **Step 2: Save the scene**

Save it to `Assets/Scenes/TopDownShooterPrototype.unity` and set it as the first scene in build settings.

### Task 3: Verification

**Files:**
- Modify: generated Unity scene metadata as needed.

- [ ] **Step 1: Run Unity batchmode scene builder**

Run Unity with `-batchmode -quit -executeMethod TopDownPrototypeSceneBuilder.BuildPrototypeScene`.

- [ ] **Step 2: Confirm compile result**

Check the Unity log for compile errors. The expected result is exit code `0` and no C# compiler errors.
