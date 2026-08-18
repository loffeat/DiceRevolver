# Test Robot Behavior Tree Design

## Goal

Add a reusable test robot enemy that continuously aims and fires at the player while maintaining a configurable combat distance: approach when too far, retreat when too close, and strafe when inside the preferred range.

## Approved Behavior

- The robot continuously targets the player while a player exists.
- Distance greater than the exposed far threshold selects approach.
- Distance less than the exposed near threshold selects retreat.
- Distance inside the combat band selects perpendicular strafing.
- Strafe direction changes on an exposed interval.
- The robot continuously requests fire while it has a target; the existing revolver controls cadence and reload lockout.
- All six dice faces use the basic revolver projectile event.
- The robot projectile definition reuses the existing basic revolver projectile prefab but has damage `0`.
- The robot remains an infinite-health damage-number target for later combat testing.

## Architecture

### Shared Character Control Seam

`TopDownCharacterController` becomes the shared character-control module. Its interface exposes current aim/movement state and fire/reload intent, while its implementation owns the existing CharacterController movement, gameplay-plane snap, and body rotation. `TopDownPlayerController` remains the player adapter and reads WASD/mouse input. `TestRobotController` is the enemy adapter and reads decisions from the combat brain.

The existing serialized field name `player` remains unchanged in the aim rig, animator bridge, and revolver gun, but its type becomes the shared controller. Existing Player Prefab object references therefore remain valid without resaving the prefab.

### Behavior Tree Module

A small internal generic behavior-tree module provides `Sequence`, `Selector`, `Parallel`, `Condition`, and `Action` nodes. It has no Unity scene dependencies.

`TestRobotCombatBrain` is the deep module at the AI decision seam. Its single tick interface accepts self position, target position, and time, then returns a decision containing movement, aim point, fire intent, and movement mode. The tree is:

1. Root sequence verifies a target decision can be produced.
2. A parallel node updates aim, fire, and movement in the same tick.
3. Movement selector evaluates far approach, near retreat, then in-range strafe.

This keeps behavior selection independently testable and prevents AI failures from spreading into aiming, gun, dice-face, or player-input implementations.

### Robot Unity Adapter

`TestRobotController` finds or accepts a `TopDownPlayerController`, ticks the combat brain, and feeds the resulting intent into the shared movement implementation. Inspector ports use Chinese names for minimum combat distance, maximum combat distance, and strafe-direction interval. Movement speed and turn speed remain the same shared ports used by the player controller.

### Assets

`Assets/Prefab/TestRobot.prefab` is created from the current `TargetDummy.prefab` visual hierarchy so it inherits the existing body, arm, muzzle, animation, infinite health, and damage-number packaging. Only the new prefab is wired with the robot controller, CharacterController, DiceFaceLoadout, and DiceRevolverGun. The source TargetDummy Prefab and Player Prefab are not resaved.

`TestRobotRevolverBullet.asset` references `BasicRevolverBullet.prefab`, copies its travel/type settings, and sets damage to `0`. `FireTestRobotRevolverProjectile.asset` is assigned as the base effect for all six robot faces and appended to the existing libraries without replacing existing entries.

## Serialization And Protection Rules

- Do not resave or rewrite `Assets/Prefab/Player.prefab`.
- Do not modify Player AimRoot child transforms, sorting layers, body scale/height, or DiceRevolverGun tuning.
- Do not modify `Assets/Prefab/TargetDummy.prefab`; clone it into the new robot prefab.
- Do not run `TopDownPrototypeSceneBuilder`.
- Only append robot assets to shared libraries; preserve existing object references and ordering.
- The robot and its projectiles remain on gameplay height `0`.

## Testing

- Pure EditMode tests verify approach, retreat, strafe, direction switching, and continuous aim/fire decisions.
- Integration tests verify the shared controller references remain valid on Player Prefab instances.
- Asset tests verify the robot prefab wiring, all six base effects, zero-damage projectile definition, and one scene instance.
- Final verification runs the complete EditMode suite and compares protected Player/TargetDummy Prefab hashes and player gun tuning against their pre-task values.

## Alternatives Considered

- Unity Behavior package: rejected because the package is not installed and this prototype does not justify a new network/package dependency.
- A robot-only movement implementation: rejected because it would duplicate the player motor and animation state.
- Direct AI logic inside DiceRevolverGun: rejected because it couples combat decisions to weapon execution and risks contaminating the player weapon path.

