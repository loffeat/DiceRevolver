using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class LightningProjectileAssetTests
    {
        [Test]
        public void LightningOrbApprovedDefaultsRemainStable()
        {
            Assert.That(LightningProjectileDefaults.Damage, Is.EqualTo(1f));
            Assert.That(LightningProjectileDefaults.FlightSpeed, Is.EqualTo(5f));
            Assert.That(LightningProjectileDefaults.FlightDistance, Is.EqualTo(15f));
            Assert.That(LightningProjectileDefaults.EnemyPierceCount, Is.EqualTo(4));
            Assert.That(LightningProjectileDefaults.ColliderRadius, Is.EqualTo(0.35f));
            Assert.That(LightningProjectileDefaults.DefaultAttackEffect, Is.False);
            Assert.That(LightningProjectileDefaults.ProjectileTypeName, Is.EqualTo("LightningOrb"));
            Assert.That(LightningProjectileDefaults.LightningTagName, Is.EqualTo("Lightning"));
            Assert.That(LightningProjectileDefaults.ElementalTagName, Is.EqualTo("Elemental"));
        }
    }
}
