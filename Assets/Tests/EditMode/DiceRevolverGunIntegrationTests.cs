using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverGunIntegrationTests
    {
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
                    null,
                    Vector3.zero,
                    Vector3.forward,
                    gun,
                    null,
                    (delay, callback) => scheduler.Schedule(0f, delay, callback),
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
                entry,
                Vector3.zero,
                Vector3.forward,
                gun,
                null,
                (_, callback) => callback.Invoke(),
                _ => { });
            DiceRevolverShotContext shot = new DiceRevolverShotContext(
                3,
                Vector3.zero,
                Vector3.forward,
                projectile,
                entry,
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

        private sealed class CountingHitEffect : BulletEventEffect
        {
            public int TriggerCount { get; private set; }

            public override void Trigger(BulletEventContext context)
            {
                TriggerCount++;
            }
        }
    }
}
