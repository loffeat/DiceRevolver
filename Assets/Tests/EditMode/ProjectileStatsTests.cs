using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileStatsTests
    {
        [Test]
        public void ConfigureAppliesRuntimeStats()
        {
            GameObject owner = new GameObject("Projectile");
            Projectile projectile = owner.AddComponent<Projectile>();

            projectile.Configure(new ProjectileRuntimeStats("Piercing", "PlayerBullet", 7f, 12f, 24f, 2));

            Assert.That(projectile.ProjectileType, Is.EqualTo("Piercing"));
            Assert.That(projectile.ProjectileTag, Is.EqualTo("PlayerBullet"));
            Assert.That(projectile.Damage, Is.EqualTo(7f));
            Assert.That(projectile.EnemyPierceCount, Is.EqualTo(2));

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void RuntimeStatsClampFlightValuesAndPierceCount()
        {
            ProjectileRuntimeStats stats = new ProjectileRuntimeStats(null, "", 3f, -2f, 0f, -4);

            Assert.That(stats.ProjectileType, Is.EqualTo("Default"));
            Assert.That(stats.ProjectileTag, Is.EqualTo("Default"));
            Assert.That(stats.FlightDistance, Is.GreaterThan(0f));
            Assert.That(stats.FlightSpeed, Is.GreaterThan(0f));
            Assert.That(stats.EnemyPierceCount, Is.EqualTo(0));
        }
    }
}
