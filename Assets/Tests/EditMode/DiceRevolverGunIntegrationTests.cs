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
    }
}
