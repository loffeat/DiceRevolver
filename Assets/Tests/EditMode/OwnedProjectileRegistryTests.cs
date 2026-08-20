using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class OwnedProjectileRegistryTests
    {
        [Test]
        public void RegistriesOnlyReturnTheirOwnNearbyTaggedProjectiles()
        {
            ProjectileTagDefinition lightning = CreateTag("Lightning");
            ProjectileTagDefinition elemental = CreateTag("Elemental");
            OwnedProjectileRegistry playerRegistry = new OwnedProjectileRegistry();
            OwnedProjectileRegistry enemyRegistry = new OwnedProjectileRegistry();
            Projectile nearLightning = CreateProjectile("Near", new Vector3(2f, 0f, 0f));
            Projectile farLightning = CreateProjectile("Far", new Vector3(8f, 0f, 0f));
            Projectile wrongTag = CreateProjectile("Wrong Tag", new Vector3(1f, 0f, 0f));
            Projectile enemyLightning = CreateProjectile("Enemy", new Vector3(1f, 0f, 0f));

            try
            {
                ProjectileRuntimeStats lightningStats = Stats(lightning);
                playerRegistry.Register(nearLightning, lightningStats);
                playerRegistry.Register(farLightning, lightningStats);
                playerRegistry.Register(wrongTag, Stats(elemental));
                enemyRegistry.Register(enemyLightning, lightningStats);
                List<ProjectileHandle> results = new List<ProjectileHandle>();

                playerRegistry.FindNearby(
                    Vector3.zero,
                    6f,
                    lightning,
                    null,
                    results);

                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].Projectile, Is.SameAs(nearLightning));
                Assert.That(results[0].Stats.HasTag(lightning), Is.True);
            }
            finally
            {
                DestroyProjectile(nearLightning);
                DestroyProjectile(farLightning);
                DestroyProjectile(wrongTag);
                DestroyProjectile(enemyLightning);
                Object.DestroyImmediate(lightning);
                Object.DestroyImmediate(elemental);
            }
        }

        [Test]
        public void FindNearbyExcludesRequestedProjectile()
        {
            ProjectileTagDefinition lightning = CreateTag("Lightning");
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            Projectile origin = CreateProjectile("Origin", Vector3.zero);
            Projectile other = CreateProjectile("Other", Vector3.right);

            try
            {
                registry.Register(origin, Stats(lightning));
                registry.Register(other, Stats(lightning));
                List<ProjectileHandle> results = new List<ProjectileHandle>();

                registry.FindNearby(Vector3.zero, 6f, lightning, origin, results);

                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].Projectile, Is.SameAs(other));
            }
            finally
            {
                DestroyProjectile(origin);
                DestroyProjectile(other);
                Object.DestroyImmediate(lightning);
            }
        }

        [Test]
        public void QueryRemovesDestroyedProjectileReferences()
        {
            ProjectileTagDefinition lightning = CreateTag("Lightning");
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            Projectile projectile = CreateProjectile("Temporary", Vector3.zero);
            registry.Register(projectile, Stats(lightning));
            Object.DestroyImmediate(projectile.gameObject);
            List<ProjectileHandle> results = new List<ProjectileHandle>();

            registry.FindNearby(Vector3.zero, 6f, lightning, null, results);

            Assert.That(results, Is.Empty);
            Assert.That(registry.Count, Is.Zero);
            Object.DestroyImmediate(lightning);
        }

        private static ProjectileRuntimeStats Stats(ProjectileTagDefinition tag)
        {
            return new ProjectileRuntimeStats(
                "LightningOrb",
                "Lightning",
                null,
                new[] { tag },
                1f,
                15f,
                5f,
                4);
        }

        private static ProjectileTagDefinition CreateTag(string displayName)
        {
            ProjectileTagDefinition tag =
                ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            FieldInfo field = typeof(ProjectileTagDefinition).GetField(
                "displayName",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(tag, displayName);
            return tag;
        }

        private static Projectile CreateProjectile(string name, Vector3 position)
        {
            GameObject owner = new GameObject(name);
            owner.transform.position = position;
            return owner.AddComponent<Projectile>();
        }

        private static void DestroyProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                Object.DestroyImmediate(projectile.gameObject);
            }
        }
    }
}
