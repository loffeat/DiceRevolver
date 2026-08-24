using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverGunIntegrationTests
    {
        private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
        private const string BasicSpawnEffectPath =
            "Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset";
        private const string EchoSynergyEntryPath =
            "Assets/Resources/DiceFacePrototype/DiceFaces/EchoSynergy.asset";
        private const string BurningBulletEntryPath =
            "Assets/Resources/DiceFacePrototype/DiceFaces/BurningBullet.asset";

        [Test]
        public void GunStartsWithTheFixedRulesFaceCount()
        {
            GameObject gunOwner = new GameObject("Gun");
            try
            {
                DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();

                InvokePrivate(gun, "Awake");

                Assert.That(gun.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount));
                Assert.That(gun.RemainingRounds, Is.EqualTo(6));
            }
            finally
            {
                Object.DestroyImmediate(gunOwner);
            }
        }

        [Test]
        public void PlayerPrefabStartsWithNoDefaultRelics()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            try
            {
                InvokePrivate(gun, "Awake");

                Assert.That(gun.Relics, Is.Not.Null);
                Assert.That(gun.Relics.Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void GunCanPickupRelicAndRaiseChangedEvent()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            LoadedFirstFaceRelicDefinition relic = ScriptableObject.CreateInstance<LoadedFirstFaceRelicDefinition>();
            relic.Face = 4;
            int changeEvents = 0;
            System.Action<IReadOnlyList<RelicDefinition>> handler = _ => changeEvents++;
            try
            {
                InitializePlayerGun(playerInstance, gun);
                gun.RelicsChanged += handler;

                bool added = gun.AddRelic(relic);

                Assert.That(added, Is.True);
                Assert.That(gun.Relics.Count, Is.EqualTo(1));
                Assert.That(gun.Relics[0], Is.EqualTo(relic));
                Assert.That(changeEvents, Is.EqualTo(1));
            }
            finally
            {
                if (gun != null)
                {
                    gun.RelicsChanged -= handler;
                }

                Object.DestroyImmediate(relic);
                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void PlayerPrefabShotConsumesOneRoundAndSpawnsConfiguredProjectile()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            Mouse mouse = null;
            Projectile spawned = null;

            try
            {
                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();

                InvokePrivate(gun, "Update");
                InvokePrivate(gun, "LateUpdate");
                spawned = FindSceneProjectile();

                Assert.That(gun.RemainingRounds, Is.EqualTo(5));
                Assert.That(spawned, Is.Not.Null);
                Assert.That(spawned.ProjectileType, Is.EqualTo("Revolver"));
                Assert.That(spawned.ProjectileTag, Is.EqualTo("PlayerBullet"));
                Assert.That(spawned.Damage, Is.EqualTo(1f));
                Assert.That(spawned.EnemyPierceCount, Is.Zero);
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyProjectile(spawned);
                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void GunPreservesFireStageOrder()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            List<string> order = new List<string>();
            OrderRecordingEffect baseEffect = CreateRecordingEffect("base", order);
            OrderRecordingEffect onFireEffect = CreateRecordingEffect("on-fire", order);
            OrderRecordingEffect onFireEndEffect = CreateRecordingEffect("on-fire-end", order);
            DiceFaceEntry onFireEntry = CreateEntry(DiceFaceSlotType.OnFire, onFireEffect);
            DiceFaceEntry onFireEndEntry = CreateEntry(DiceFaceSlotType.OnFireEnd, onFireEndEffect);
            Mouse mouse = null;

            try
            {
                ConfigureEveryFace(loadout, baseEffect, onFireEntry, onFireEndEntry);
                gun.FireStarted += _ => order.Add("fire-started");
                gun.FireEnded += _ => order.Add("fire-ended");
                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();

                InvokePrivate(gun, "LateUpdate");

                Assert.That(
                    order,
                    Is.EqualTo(new[]
                    {
                        "fire-started",
                        "base",
                        "on-fire",
                        "fire-ended",
                        "on-fire-end"
                    }));
            }
            finally
            {
                RemoveDevice(mouse);
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(onFireEntry);
                Object.DestroyImmediate(onFireEndEntry);
                Object.DestroyImmediate(baseEffect);
                Object.DestroyImmediate(onFireEffect);
                Object.DestroyImmediate(onFireEndEffect);
            }
        }

        [Test]
        public void LoadedFourAtFireEndPreventsIncorrectAutomaticReload()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            ConditionalLoadedFourEffect loadedFour =
                ScriptableObject.CreateInstance<ConditionalLoadedFourEffect>();
            DiceFaceEntry loadedFourEntry = CreateEntry(DiceFaceSlotType.OnFireEnd, loadedFour);
            Mouse mouse = null;

            try
            {
                SetPrivateField(gun, "shotsPerSecond", float.PositiveInfinity);
                loadedFour.Gun = gun;
                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    loadout.Equip(face, loadedFourEntry);
                }

                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();

                for (int shot = 0; shot < DiceRevolverRules.FaceCount; shot++)
                {
                    InvokePrivate(gun, "LateUpdate");
                }

                Assert.That(gun.RemainingRounds, Is.EqualTo(1));
                Assert.That(gun.IsReloading, Is.False);
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyAllSceneProjectiles();
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(loadedFourEntry);
                Object.DestroyImmediate(loadedFour);
            }
        }

        [Test]
        public void GunRelaysProjectileHitBeforeOnHitAndDirectDamage()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            List<string> order = new List<string>();
            OrderRecordingEffect onHitEffect = CreateRecordingEffect("on-hit", order);
            DiceFaceEntry onHitEntry = CreateEntry(DiceFaceSlotType.OnHit, onHitEffect);
            GameObject target = new GameObject("Target");
            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            RecordingDamageReceiver receiver = target.AddComponent<RecordingDamageReceiver>();
            Mouse mouse = null;
            Projectile spawned = null;

            try
            {
                receiver.Order = order;
                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    loadout.Equip(face, onHitEntry);
                }

                gun.ProjectileHit += _ => order.Add("projectile-hit");
                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();
                InvokePrivate(gun, "LateUpdate");
                spawned = FindSceneProjectile();
                Assert.That(spawned, Is.Not.Null);

                ExpectEditModeDestroy();
                InvokePrivate(spawned, "OnTriggerEnter", targetCollider);

                Assert.That(
                    order,
                    Is.EqualTo(new[] { "projectile-hit", "on-hit", "damage" }));
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyProjectile(spawned);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(onHitEntry);
                Object.DestroyImmediate(onHitEffect);
            }
        }

        [Test]
        public void BurningHitTriggersEchoAdjacentFacesThroughTheGunNotificationBoundary()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            DiceFaceEntry echo = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(EchoSynergyEntryPath);
            DiceFaceEntry burning = AssetDatabase.LoadAssetAtPath<DiceFaceEntry>(BurningBulletEntryPath);
            GameObject target = new GameObject("IgniteTarget");
            target.AddComponent<EnemyHealth>();
            target.AddComponent<EnemyStatusHost>();
            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            List<int> firedFaces = new List<int>();
            Mouse mouse = null;

            try
            {
                Assert.That(echo, Is.Not.Null);
                Assert.That(burning, Is.Not.Null);
                Assert.That(loadout.Equip(3, echo), Is.True);
                Assert.That(loadout.Equip(4, burning), Is.True);
                gun.FireStarted += shot => firedFaces.Add(shot.Face);
                InitializePlayerGun(playerInstance, gun);
                DiceRevolverRuntime runtime = GetPrivateField<DiceRevolverRuntime>(gun, "runtime");
                Assert.That(runtime.SetFirstDrawForce(4), Is.True);
                mouse = HoldLeftMouse();

                InvokePrivate(gun, "LateUpdate");
                Projectile initialProjectile = FindSceneProjectile();
                Assert.That(initialProjectile, Is.Not.Null);
                Assert.That(firedFaces, Is.EqualTo(new[] { 4 }));

                ExpectEditModeDestroy();
                InvokePrivate(initialProjectile, "OnTriggerEnter", targetCollider);

                Assert.That(firedFaces, Is.EqualTo(new[] { 4, 1, 2, 4, 6 }));
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyAllSceneProjectiles();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void MissingCharacterControllerMakesUpdateAndLateUpdateSafeNoOps()
        {
            GameObject gunOwner = new GameObject("Gun");
            try
            {
                DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
                InvokePrivate(gun, "Awake");

                Assert.DoesNotThrow(() => InvokePrivate(gun, "Update"));
                Assert.DoesNotThrow(() => InvokePrivate(gun, "LateUpdate"));
                Assert.That(gun.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount));
            }
            finally
            {
                Object.DestroyImmediate(gunOwner);
            }
        }

        [Test]
        public void MissingMuzzleDoesNotConsumeADieFace()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            Mouse mouse = null;

            try
            {
                InitializePlayerGun(playerInstance, gun);
                SetPrivateField<Transform>(gun, "muzzle", null);
                mouse = HoldLeftMouse();

                InvokePrivate(gun, "Update");
                InvokePrivate(gun, "LateUpdate");

                Assert.That(gun.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount));
            }
            finally
            {
                RemoveDevice(mouse);
                Object.DestroyImmediate(playerInstance);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingProjectileDefinitionOrPrefabWarnsAndContinuesLaterActivation(
            bool definitionExistsWithoutPrefab)
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            ProjectileSpawnEffect missingSpawn = ScriptableObject.CreateInstance<ProjectileSpawnEffect>();
            ProjectileDefinition missingPrefabDefinition = null;
            ProjectileSpawnEffect validSpawn =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(BasicSpawnEffectPath);
            DiceFaceEntry validEntry = CreateEntry(DiceFaceSlotType.OnFire, validSpawn);
            Mouse mouse = null;
            Projectile spawned = null;

            try
            {
                if (definitionExistsWithoutPrefab)
                {
                    missingPrefabDefinition = ScriptableObject.CreateInstance<ProjectileDefinition>();
                    SetPrivateField(missingSpawn, "projectileDefinition", missingPrefabDefinition);
                    LogAssert.Expect(
                        LogType.Warning,
                        new Regex("Projectile spawn skipped.*missing", RegexOptions.Singleline));
                }
                else
                {
                    LogAssert.Expect(
                        LogType.Warning,
                        $"{nameof(ProjectileSpawnEffect)} skipped because no projectile definition is assigned.");
                }

                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    loadout.SetBaseEffect(face, missingSpawn);
                    loadout.Equip(face, validEntry);
                }

                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();
                InvokePrivate(gun, "LateUpdate");
                spawned = FindSceneProjectile();

                Assert.That(spawned, Is.Not.Null);
                Assert.That(spawned.ProjectileType, Is.EqualTo("Revolver"));
            }
            finally
            {
                RemoveDevice(mouse);
                DestroyProjectile(spawned);
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(validEntry);
                Object.DestroyImmediate(missingSpawn);
                if (missingPrefabDefinition != null)
                {
                    Object.DestroyImmediate(missingPrefabDefinition);
                }
            }
        }

        [Test]
        public void GunPassesConfiguredEventBudgetIntoEachActivation()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            List<string> order = new List<string>();
            OrderRecordingEffect baseEffect = CreateRecordingEffect("base", order);
            OrderRecordingEffect onFireEffect = CreateRecordingEffect("on-fire", order);
            DiceFaceEntry onFireEntry = CreateEntry(DiceFaceSlotType.OnFire, onFireEffect);
            Mouse mouse = null;

            try
            {
                SetPrivateField(gun, "eventBudgetPerActivation", 1);
                for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
                {
                    loadout.SetBaseEffect(face, baseEffect);
                    loadout.Equip(face, onFireEntry);
                }

                InitializePlayerGun(playerInstance, gun);
                mouse = HoldLeftMouse();
                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("Dice face .* event budget was exhausted", RegexOptions.Singleline));

                InvokePrivate(gun, "LateUpdate");

                Assert.That(order, Is.EqualTo(new[] { "base" }));
            }
            finally
            {
                RemoveDevice(mouse);
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(onFireEntry);
                Object.DestroyImmediate(baseEffect);
                Object.DestroyImmediate(onFireEffect);
            }
        }

        [Test]
        public void ReloadCompletionAllowsLateUpdateToFireInTheSameFrame()
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.AddComponent<CharacterController>();
            ControllableCharacterController player =
                playerObject.AddComponent<ControllableCharacterController>();
            GameObject gunObject = new GameObject("Gun");
            gunObject.transform.SetParent(playerObject.transform);
            DiceRevolverGun gun = gunObject.AddComponent<DiceRevolverGun>();
            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(gunObject.transform);

            try
            {
                SetPrivateField<TopDownCharacterController>(gun, "player", player);
                SetPrivateField(gun, "visualRoot", gunObject.transform);
                SetPrivateField(gun, "muzzle", muzzleObject.transform);
                SetPrivateField(gun, "shotsPerSecond", float.PositiveInfinity);
                InvokePrivate(gun, "Awake");
                DiceRevolverRuntime runtime = new DiceRevolverRuntime(
                    float.PositiveInfinity,
                    0.05f,
                    true,
                    true);
                Assert.That(runtime.TryBeginShot(0f).Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
                Assert.That(runtime.Tick(-1f, true).ReloadStarted, Is.True);
                SetPrivateField(gun, "runtime", runtime);
                player.SetFireHeld(true);

                InvokePrivate(gun, "Update");
                Assert.That(gun.IsReloading, Is.False);
                Assert.That(gun.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount));

                InvokePrivate(gun, "LateUpdate");

                Assert.That(gun.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount - 1));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReloadBlinkDoesNotMoveOrRotateArmVisual()
        {
            GameObject playerInstance = InstantiatePlayer();
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            Transform armVisual = playerInstance.transform.Find("VisualRoot/HandRig/AimRoot/ArmVisual");

            try
            {
                Assert.That(armVisual, Is.Not.Null);
                InvokePrivate(gun, "Awake");
                Vector3 originalPosition = armVisual.localPosition;
                Quaternion originalRotation = armVisual.localRotation;
                Color originalColor = armVisual.GetComponent<SpriteRenderer>().color;

                InvokePrivate(gun, "AnimateReload", 0.5f);

                Assert.That(armVisual.localPosition, Is.EqualTo(originalPosition));
                Assert.That(armVisual.localRotation, Is.EqualTo(originalRotation));
                Assert.That(armVisual.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(originalColor));
            }
            finally
            {
                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void PlayerControllerAwakeSnapsRootToGameplayPlane()
        {
            GameObject playerInstance = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath),
                new Vector3(2f, 3f, 4f),
                Quaternion.identity);
            TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();

            try
            {
                InvokePrivate(player, "Awake");

                Assert.That(playerInstance.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(playerInstance);
            }
        }

        private static GameObject InstantiatePlayer()
        {
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
        }

        private static void InitializePlayerGun(GameObject playerInstance, DiceRevolverGun gun)
        {
            TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();
            TopDownAimHandRig aimRig = playerInstance.GetComponentInChildren<TopDownAimHandRig>();
            Assert.That(player, Is.Not.Null);
            Assert.That(aimRig, Is.Not.Null);
            Assert.That(gun, Is.Not.Null);
            InvokePrivate(player, "Awake");
            InvokePrivate(aimRig, "Awake");
            InvokePrivate(gun, "Awake");
        }

        private static Mouse HoldLeftMouse()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState().WithButton(MouseButton.Left));
            InputSystem.Update();
            return mouse;
        }

        private static void RemoveDevice(InputDevice device)
        {
            if (device != null && device.added)
            {
                InputSystem.RemoveDevice(device);
            }
        }

        private static Projectile FindSceneProjectile()
        {
            Projectile[] projectiles =
                Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i].gameObject.scene.IsValid())
                {
                    return projectiles[i];
                }
            }

            return null;
        }

        private static void DestroyProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                Object.DestroyImmediate(projectile.gameObject);
            }
        }

        private static void DestroyAllSceneProjectiles()
        {
            Projectile[] projectiles =
                Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null && projectiles[i].gameObject.scene.IsValid())
                {
                    Object.DestroyImmediate(projectiles[i].gameObject);
                }
            }
        }

        private static void ConfigureEveryFace(
            DiceFaceLoadout loadout,
            BulletEventEffect baseEffect,
            params DiceFaceEntry[] entries)
        {
            for (int face = 1; face <= DiceRevolverRules.FaceCount; face++)
            {
                loadout.SetBaseEffect(face, baseEffect);
                for (int entry = 0; entry < entries.Length; entry++)
                {
                    loadout.Equip(face, entries[entry]);
                }
            }
        }

        private static DiceFaceEntry CreateEntry(DiceFaceSlotType slotType, BulletEventEffect effect)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            SetPrivateField(entry, "slotType", slotType);
            SetPrivateField(entry, "effect", effect);
            return entry;
        }

        private static OrderRecordingEffect CreateRecordingEffect(
            string marker,
            List<string> order)
        {
            OrderRecordingEffect effect = ScriptableObject.CreateInstance<OrderRecordingEffect>();
            effect.Marker = marker;
            effect.Order = order;
            return effect;
        }

        private static void ExpectEditModeDestroy()
        {
            LogAssert.Expect(
                LogType.Error,
                new Regex("Destroy may not be called from edit mode!.*", RegexOptions.Singleline));
        }

        private static void SetPrivateField<T>(object owner, string fieldName, T value)
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {owner.GetType().Name}.{fieldName}");
            field.SetValue(owner, value);
        }

        private static T GetPrivateField<T>(object owner, string fieldName)
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {owner.GetType().Name}.{fieldName}");
            return (T)field.GetValue(owner);
        }

        private static void InvokePrivate(object owner, string methodName, params object[] arguments)
        {
            MethodInfo method = owner.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {owner.GetType().Name}.{methodName}");
            method.Invoke(owner, arguments);
        }

        private sealed class OrderRecordingEffect : BulletEventEffect
        {
            public string Marker { get; set; }
            public List<string> Order { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                Order.Add(Marker);
            }
        }

        private sealed class ConditionalLoadedFourEffect : BulletEventEffect
        {
            public DiceRevolverGun Gun { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                if (Gun.RemainingRounds == 0)
                {
                    context.RequestRefillAndForceNextFace(4);
                }
            }
        }

        public sealed class RecordingDamageReceiver : MonoBehaviour, IDamageReceiver
        {
            public List<string> Order { get; set; }

            public void ReceiveDamage(DamageInfo damage)
            {
                Order.Add("damage");
            }
        }

        public sealed class ControllableCharacterController : TopDownCharacterController
        {
            public override Vector3 AimWorldPoint { get; protected set; }
            public override Vector3 AimDirection { get; protected set; } = Vector3.forward;
            public override Vector2 MoveInput { get; protected set; }
            public override bool FireHeld { get; protected set; }
            public override bool ReloadPressedThisFrame { get; protected set; }

            public override void RefreshControlIntent(float time)
            {
            }

            public void SetFireHeld(bool value)
            {
                FireHeld = value;
            }

        }
    }
}
