# Player Prefab Rig Rebuild Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the player as a reusable prefab with separated movement, visual animation, hand aiming, and dice revolver weapon logic.

**Architecture:** The player root owns movement and aim input. A dedicated hand rig owns arm orbit, mirroring, sorting, and muzzle orientation. The dice revolver fires from the muzzle transform so the visible barrel and projectile direction stay aligned.

**Tech Stack:** Unity 6000.3, C#, built-in physics, SpriteRenderer/Animator, editor prefab generation.

## Global Constraints

- Keep gameplay on the existing XZ floor plane.
- Save the player prefab at `Assets/Prefab/Player.prefab`.
- Keep dice revolver tuning exposed in `DiceRevolverGun`.
- Keep visual rig separate from `TopDownPlayerController` and dice chamber logic.

---

### Task 1: Introduce Aim Hand Rig

**Files:**
- Create: `Assets/Scripts/Prototype/TopDownAimHandRig.cs`
- Modify: `Assets/Scripts/Prototype/DiceRevolverGun.cs`

**Interfaces:**
- Consumes: `TopDownPlayerController.AimDirection`.
- Produces: stable `muzzle.forward` for `DiceRevolverGun`.

- [ ] Add `TopDownAimHandRig` with serialized references for player, body renderer, arm renderer, aim root, muzzle, orbit radius, sorting orders, and local scale controls.
- [ ] Rotate `aimRoot` using `Quaternion.LookRotation(player.AimDirection, Vector3.up)`.
- [ ] Mirror body and arm based on aim direction without rotating the player root.
- [ ] Sort arm in front when aiming toward camera and behind when aiming away.
- [ ] Update `DiceRevolverGun` so projectile launch direction is `muzzle.forward` flattened to XZ.

### Task 2: Generate Player Prefab

**Files:**
- Modify: `Assets/Scripts/Editor/TopDownPrototypeSceneBuilder.cs`

**Interfaces:**
- Produces: `Assets/Prefab/Player.prefab`.
- Consumes: `Assets/PrototypeProjectile.prefab`, player body animation, player arm sprite.

- [ ] Ensure `Assets/Prefab` exists.
- [ ] Build a `Player` hierarchy with `VisualRoot`, `Body`, `HandRig`, `AimRoot`, `ArmSprite`, `GunBody`, `Muzzle`, and `CameraTarget`.
- [ ] Attach `TopDownPlayerController`, `PlayerMovementAnimatorBridge`, `TopDownAimHandRig`, and `DiceRevolverGun`.
- [ ] Save the hierarchy as `Assets/Prefab/Player.prefab`.
- [ ] Instantiate the prefab into `TopDownShooterPrototype.unity`.

### Task 3: Verify

**Files:**
- Validate: `Assets/Prefab/Player.prefab`
- Validate: `Assets/Scenes/TopDownShooterPrototype.unity`

- [ ] Run Unity batchmode with `TopDownPrototypeSceneBuilder.BuildPrototypeScene`.
- [ ] Confirm compile log contains `Tundra build success` and `return code 0`.
- [ ] Confirm scene references `Player.prefab`, `TopDownAimHandRig`, and `DiceRevolverGun`.
