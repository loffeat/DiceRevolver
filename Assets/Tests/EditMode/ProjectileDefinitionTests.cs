using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class ProjectileDefinitionTests
    {
        [Test]
        public void DefinitionOwnsEveryRuntimeProjectileAttribute()
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            SetField(definition, "projectileType", "Revolver");
            SetField(definition, "projectileTag", "PlayerBullet");
            SetField(definition, "damage", 7f);
            SetField(definition, "flightDistance", 23f);
            SetField(definition, "flightSpeed", 31f);
            SetField(definition, "enemyPierceCount", 2);

            ProjectileRuntimeStats stats = definition.BuildRuntimeStats();

            Assert.That(stats.ProjectileType, Is.EqualTo("Revolver"));
            Assert.That(stats.ProjectileTag, Is.EqualTo("PlayerBullet"));
            Assert.That(stats.Damage, Is.EqualTo(7f));
            Assert.That(stats.FlightDistance, Is.EqualTo(23f));
            Assert.That(stats.FlightSpeed, Is.EqualTo(31f));
            Assert.That(stats.EnemyPierceCount, Is.EqualTo(2));
            Assert.That(definition.ExtensionPorts, Is.Not.Null);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void DefinitionExposesAttackEffectDefault()
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            SetField(definition, "defaultAttackEffect", true);

            Assert.That(definition.DefaultAttackEffect, Is.True);

            Object.DestroyImmediate(definition);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }
    }
}
