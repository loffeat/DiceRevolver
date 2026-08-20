using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverGunIntegrationTests
    {
        private static readonly System.Collections.Generic.List<string> TriggerOrder = new();

        [Test]
        public void SpawnConfiguredProjectileAppliesShotStats()
        {
            GameObject gunOwner = new GameObject("Gun");
            DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
            GameObject prefabOwner = new GameObject("ProjectilePrefab");
            Projectile prefab = prefabOwner.AddComponent<Projectile>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats("Explosive", "PlayerBullet", 8f, 10f, 20f, 1);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                5,
                Vector3.zero,
                Vector3.forward,
                null,
                entry,
                stats,
                prefab);

            Projectile spawned = gun.SpawnConfiguredProjectile(shot, false);

            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.ProjectileType, Is.EqualTo("Explosive"));
            Assert.That(spawned.ProjectileTag, Is.EqualTo("PlayerBullet"));
            Assert.That(spawned.Damage, Is.EqualTo(8f));
            Assert.That(spawned.EnemyPierceCount, Is.EqualTo(1));

            Object.DestroyImmediate(spawned.gameObject);
            Object.DestroyImmediate(prefabOwner);
            Object.DestroyImmediate(gunOwner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void SpawnConfiguredProjectileFlattensOriginToGameplayPlane()
        {
            GameObject gunOwner = new GameObject("Gun");
            DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
            GameObject prefabOwner = new GameObject("ProjectilePrefab");
            Projectile prefab = prefabOwner.AddComponent<Projectile>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(
                "Default",
                "PlayerBullet",
                1f,
                10f,
                20f,
                0);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                1,
                new Vector3(2f, 5f, 3f),
                Vector3.forward,
                null,
                entry,
                stats,
                prefab);

            Projectile spawned = gun.SpawnConfiguredProjectile(shot, false);

            try
            {
                Assert.That(spawned, Is.Not.Null);
                Assert.That(spawned.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned.gameObject);
                }

                Object.DestroyImmediate(prefabOwner);
                Object.DestroyImmediate(gunOwner);
                Object.DestroyImmediate(entry);
            }
        }

        [Test]
        public void PlayerControllerAwakeSnapsRootToGameplayPlane()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject playerInstance = Object.Instantiate(playerPrefab, new Vector3(2f, 3f, 4f), Quaternion.identity);
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

        [Test]
        public void SpawnConfiguredProjectileToleratesZeroDirection()
        {
            GameObject gunOwner = new GameObject("Gun");
            DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
            GameObject prefabOwner = new GameObject("ProjectilePrefab");
            Projectile prefab = prefabOwner.AddComponent<Projectile>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats("Default", "PlayerBullet", 1f, 10f, 20f, 0);
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                1,
                Vector3.zero,
                Vector3.zero,
                null,
                entry,
                stats,
                prefab);

            Projectile spawned = null;

            Assert.DoesNotThrow(() => spawned = gun.SpawnConfiguredProjectile(shot, false));
            Assert.That(spawned, Is.Not.Null);

            Object.DestroyImmediate(spawned.gameObject);
            Object.DestroyImmediate(prefabOwner);
            Object.DestroyImmediate(gunOwner);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void ProjectileIgnoresAnotherProjectileCollider()
        {
            GameObject firstOwner = new GameObject("FirstProjectile");
            firstOwner.AddComponent<SphereCollider>().isTrigger = true;
            Projectile firstProjectile = firstOwner.AddComponent<Projectile>();

            GameObject secondOwner = new GameObject("SecondProjectile");
            SphereCollider secondCollider = secondOwner.AddComponent<SphereCollider>();
            secondCollider.isTrigger = true;
            secondOwner.AddComponent<Projectile>();

            try
            {
                MethodInfo projectileTrigger = typeof(Projectile).GetMethod(
                    "OnTriggerEnter",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(projectileTrigger, Is.Not.Null);
                projectileTrigger.Invoke(firstProjectile, new object[] { secondCollider });

                Assert.That(firstProjectile, Is.Not.Null);
            }
            finally
            {
                if (firstOwner != null)
                {
                    Object.DestroyImmediate(firstOwner);
                }

                if (secondOwner != null)
                {
                    Object.DestroyImmediate(secondOwner);
                }
            }
        }

        [Test]
        public void ProjectileHitReporterIgnoresAnotherProjectileCollider()
        {
            GameObject reporterOwner = new GameObject("ProjectileReporter");
            ProjectileHitReporter reporter = reporterOwner.AddComponent<ProjectileHitReporter>();

            GameObject projectileOwner = new GameObject("OtherProjectile");
            SphereCollider projectileCollider = projectileOwner.AddComponent<SphereCollider>();
            projectileOwner.AddComponent<Projectile>();

            int hitCount = 0;
            reporter.Hit += _ => hitCount++;

            try
            {
                MethodInfo reporterTrigger = typeof(ProjectileHitReporter).GetMethod(
                    "OnTriggerEnter",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(reporterTrigger, Is.Not.Null);
                reporterTrigger.Invoke(reporter, new object[] { projectileCollider });

                Assert.That(hitCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(reporterOwner);
                Object.DestroyImmediate(projectileOwner);
            }
        }

        [Test]
        public void GunEventContextSchedulesThroughOwnedTimeScheduler()
        {
            GameObject gunOwner = new GameObject("Gun");
            DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                2,
                Vector3.zero,
                Vector3.forward,
                null);
            int executionCount = 0;

            try
            {
                FieldInfo schedulerField = typeof(DiceRevolverGun).GetField(
                    "eventTimeScheduler",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(schedulerField, Is.Not.Null);

                BulletEventTimeScheduler scheduler =
                    (BulletEventTimeScheduler)schedulerField.GetValue(gun);
                DiceFaceActivation activation = new DiceFaceActivation(
                    2,
                    default,
                    Vector3.zero,
                    Vector3.forward,
                    (delay, callback) => scheduler.Schedule(0f, delay, callback),
                    _ => { },
                    _ => false,
                    _ => { });
                MethodInfo createEventContext = typeof(DiceRevolverGun).GetMethod(
                    "CreateEventContext",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createEventContext, Is.Not.Null);

                BulletEventContext context = (BulletEventContext)createEventContext.Invoke(
                    gun,
                    new object[] { activation, shot, null, Vector3.zero });
                bool accepted = context.Schedule(0.25f, _ => executionCount++);

                Assert.That(accepted, Is.True);
                Assert.That(scheduler.PendingCount, Is.EqualTo(1));

                scheduler.Tick(float.MaxValue);

                Assert.That(executionCount, Is.EqualTo(1));
                Assert.That(scheduler.PendingCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gunOwner);
            }
        }

        [Test]
        public void PlayerPrefabBaseEffectSpawnsConfiguredProjectileThroughGunScheduler()
        {
            const string playerPrefabPath = "Assets/Prefab/Player.prefab";
            const string spawnEffectPath =
                "Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset";

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            ProjectileSpawnEffect spawnEffect =
                AssetDatabase.LoadAssetAtPath<ProjectileSpawnEffect>(spawnEffectPath);
            GameObject playerInstance = Object.Instantiate(playerPrefab);
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            Projectile spawned = null;

            try
            {
                Assert.That(gun, Is.Not.Null);
                Assert.That(loadout, Is.Not.Null);
                Assert.That(loadout.GetBaseEffect(1), Is.SameAs(spawnEffect));

                InvokePrivate(gun, "Awake");
                BulletEventTimeScheduler scheduler = GetPrivateField<BulletEventTimeScheduler>(
                    gun,
                    "eventTimeScheduler");
                MethodInfo spawnActivationProjectile = typeof(DiceRevolverGun).GetMethod(
                    "SpawnActivationProjectile",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(spawnActivationProjectile, Is.Not.Null);

                DiceFaceActivation activation = null;
                activation = new DiceFaceActivation(
                    1,
                    default,
                    new Vector3(40f, 5f, 40f),
                    Vector3.forward,
                    (delay, callback) => scheduler.Schedule(0f, delay, callback),
                    request => spawned = (Projectile)spawnActivationProjectile.Invoke(
                        gun,
                        new object[] { activation, request }),
                    _ => false,
                    _ => { });

                spawnEffect.Trigger(new BulletEventContext(activation, null, null, Vector3.zero));
                scheduler.Tick(0f);

                Assert.That(spawned, Is.Not.Null);
                Assert.That(spawned.ProjectileType, Is.EqualTo("Revolver"));
                Assert.That(spawned.Damage, Is.EqualTo(1f));
            }
            finally
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned.gameObject);
                }

                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void PlayerPrefabLeftClickConsumesOneRoundAndSpawnsBaseProjectile()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject playerInstance = Object.Instantiate(playerPrefab);
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();
            TopDownAimHandRig aimRig = playerInstance.GetComponentInChildren<TopDownAimHandRig>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Projectile spawned = null;

            try
            {
                InvokePrivate(player, "Awake");
                InvokePrivate(aimRig, "Awake");
                InvokePrivate(gun, "Awake");
                mouse.MakeCurrent();
                InputSystem.QueueStateEvent(
                    mouse,
                    new MouseState().WithButton(MouseButton.Left));
                InputSystem.Update();

                InvokePrivate(gun, "TryFire");
                BulletEventTimeScheduler scheduler = GetPrivateField<BulletEventTimeScheduler>(
                    gun,
                    "eventTimeScheduler");
                scheduler.Tick(Time.time);
                Projectile[] projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
                for (int i = 0; i < projectiles.Length; i++)
                {
                    if (projectiles[i].gameObject.scene.IsValid())
                    {
                        spawned = projectiles[i];
                        break;
                    }
                }

                Assert.That(gun.RemainingRounds, Is.EqualTo(5));
                Assert.That(spawned, Is.Not.Null);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned.gameObject);
                }

                Object.DestroyImmediate(playerInstance);
            }
        }

        [Test]
        public void PlayerFireTriggersIndependentBaseOnFireAndOnFireEndSlotsInOrder()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject playerInstance = Object.Instantiate(playerPrefab);
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            DiceFaceLoadout loadout = playerInstance.GetComponent<DiceFaceLoadout>();
            TopDownPlayerController player = playerInstance.GetComponent<TopDownPlayerController>();
            TopDownAimHandRig aimRig = playerInstance.GetComponentInChildren<TopDownAimHandRig>();
            RecordingEffect baseEffect = CreateRecordingEffect("base");
            RecordingEffect onFireEffect = CreateRecordingEffect("on-fire");
            RecordingEffect onFireEndEffect = CreateRecordingEffect("on-fire-end");
            DiceFaceEntry onFireEntry = CreateEntry(DiceFaceSlotType.OnFire, onFireEffect);
            DiceFaceEntry onFireEndEntry = CreateEntry(DiceFaceSlotType.OnFireEnd, onFireEndEffect);
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            TriggerOrder.Clear();

            try
            {
                for (int face = 1; face <= 6; face++)
                {
                    loadout.SetBaseEffect(face, baseEffect);
                    loadout.Equip(face, onFireEntry);
                    loadout.Equip(face, onFireEndEntry);
                }

                gun.FireStarted += _ => TriggerOrder.Add("fire-started");
                gun.FireEnded += _ => TriggerOrder.Add("fire-ended");
                InvokePrivate(player, "Awake");
                InvokePrivate(aimRig, "Awake");
                InvokePrivate(gun, "Awake");
                mouse.MakeCurrent();
                InputSystem.QueueStateEvent(
                    mouse,
                    new MouseState().WithButton(MouseButton.Left));
                InputSystem.Update();

                InvokePrivate(gun, "TryFire");

                Assert.That(
                    TriggerOrder,
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
                InputSystem.RemoveDevice(mouse);
                Object.DestroyImmediate(playerInstance);
                Object.DestroyImmediate(onFireEntry);
                Object.DestroyImmediate(onFireEndEntry);
                Object.DestroyImmediate(baseEffect);
                Object.DestroyImmediate(onFireEffect);
                Object.DestroyImmediate(onFireEndEffect);
                TriggerOrder.Clear();
            }
        }

        [Test]
        public void ReloadBlinkDoesNotMoveOrRotateArmVisual()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            GameObject playerInstance = Object.Instantiate(playerPrefab);
            DiceRevolverGun gun = playerInstance.GetComponentInChildren<DiceRevolverGun>();
            Transform armVisual = playerInstance.transform.Find("VisualRoot/HandRig/AimRoot/ArmVisual");

            try
            {
                Assert.That(gun, Is.Not.Null);
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

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void ProjectileHitDispatchesFaceHitEffectsOnlyWhenAllowed(
            bool allowHitEffects,
            int expectedTriggerCount)
        {
            GameObject gunOwner = new GameObject("Gun");
            DiceRevolverGun gun = gunOwner.AddComponent<DiceRevolverGun>();
            GameObject projectileOwner = new GameObject("Projectile");
            Projectile projectile = projectileOwner.AddComponent<Projectile>();
            ProjectileHitReporter reporter = projectileOwner.AddComponent<ProjectileHitReporter>();
            GameObject target = new GameObject("Target");
            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            CountingHitEffect effect = ScriptableObject.CreateInstance<CountingHitEffect>();
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            typeof(DiceFaceEntry).GetField(
                "onHitEffects",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                entry,
                new BulletEventEffect[] { effect });
            DiceFaceActivation activation = new DiceFaceActivation(
                3,
                DiceFaceConfigurationSnapshot.FromEntry(entry),
                Vector3.zero,
                Vector3.forward,
                (_, callback) => callback.Invoke(),
                _ => { },
                _ => false,
                _ => { });
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                3,
                Vector3.zero,
                Vector3.forward,
                projectile,
                DiceFaceConfigurationSnapshot.FromEntry(entry),
                default,
                null,
                null,
                activation,
                allowHitEffects);

            try
            {
                MethodInfo bridge = typeof(DiceRevolverGun).GetMethod(
                    "BridgeProjectileHit",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bridge.Invoke(gun, new object[] { projectile, shot, allowHitEffects });

                MethodInfo reportHit = typeof(ProjectileHitReporter).GetMethod(
                    "OnTriggerEnter",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                reportHit.Invoke(reporter, new object[] { targetCollider });

                Assert.That(effect.TriggerCount, Is.EqualTo(expectedTriggerCount));
            }
            finally
            {
                Object.DestroyImmediate(entry);
                Object.DestroyImmediate(effect);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(projectileOwner);
                Object.DestroyImmediate(gunOwner);
            }
        }

        private static DiceFaceEntry CreateEntry(DiceFaceSlotType slotType, BulletEventEffect effect)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            typeof(DiceFaceEntry).GetField("slotType", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(entry, slotType);
            typeof(DiceFaceEntry).GetField("effect", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(entry, effect);
            return entry;
        }

        private static RecordingEffect CreateRecordingEffect(string marker)
        {
            RecordingEffect effect = ScriptableObject.CreateInstance<RecordingEffect>();
            effect.Marker = marker;
            return effect;
        }

        private sealed class RecordingEffect : BulletEventEffect
        {
            public string Marker { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                TriggerOrder.Add(Marker);
            }
        }

        private sealed class CountingHitEffect : BulletEventEffect
        {
            public int TriggerCount { get; private set; }

            public override void Trigger(BulletEventContext context)
            {
                TriggerCount++;
            }
        }

        private static T GetPrivateField<T>(object owner, string fieldName)
        {
            FieldInfo field = owner.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(owner);
        }

        private static void InvokePrivate(object owner, string methodName, params object[] arguments)
        {
            MethodInfo method = owner.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(owner, arguments);
        }
    }
}
