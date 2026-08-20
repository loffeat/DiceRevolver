using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class TeslaPassiveTests
    {
        [Test]
        public void LightningProjectileUsesOldStacksThenAddsOneStack()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            TeslaPassiveEffect effect = CreateEffect(lightning, 0.05f);
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(2, effect, type);
            ProjectileRuntimeStats stats = Stats(type, lightning, 1f);

            ProjectileRuntimeStats first = passives.ModifyProjectileStats(2, stats);
            passives.NotifyProjectileSpawned(2, new ProjectileHandle(null, first));
            ProjectileRuntimeStats second = passives.ModifyProjectileStats(2, stats);

            Assert.That(first.Damage, Is.EqualTo(1f));
            Assert.That(second.Damage, Is.EqualTo(1.05f).Within(0.0001f));

            Destroy(type, lightning, effect);
        }

        [Test]
        public void LightningFromOtherFaceBuildsStacksButOnlyOwnerFaceReceivesDamageBonus()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            TeslaPassiveEffect effect = CreateEffect(lightning, 0.05f);
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(3, effect, type);
            ProjectileRuntimeStats stats = Stats(type, lightning, 2f);

            passives.NotifyProjectileSpawned(6, new ProjectileHandle(null, stats));

            Assert.That(passives.ModifyProjectileStats(3, stats).Damage, Is.EqualTo(2.1f).Within(0.0001f));
            Assert.That(passives.ModifyProjectileStats(6, stats).Damage, Is.EqualTo(2f));

            Destroy(type, lightning, effect);
        }

        [Test]
        public void NonLightningProjectileDoesNotBuildStacksAndReloadClearsExistingStacks()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            ProjectileTagDefinition physical = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            TeslaPassiveEffect effect = CreateEffect(lightning, 0.05f);
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(4, effect, type);
            ProjectileRuntimeStats lightningStats = Stats(type, lightning, 1f);

            passives.NotifyProjectileSpawned(
                1,
                new ProjectileHandle(null, Stats(type, physical, 1f)));
            Assert.That(passives.ModifyProjectileStats(4, lightningStats).Damage, Is.EqualTo(1f));

            passives.NotifyProjectileSpawned(1, new ProjectileHandle(null, lightningStats));
            Assert.That(passives.ModifyProjectileStats(4, lightningStats).Damage, Is.EqualTo(1.05f).Within(0.0001f));
            passives.NotifyReloadStarted();
            Assert.That(passives.ModifyProjectileStats(4, lightningStats).Damage, Is.EqualTo(1f));

            Destroy(type, lightning, physical, effect);
        }

        [Test]
        public void ReplacingOneTeslaResetsOnlyThatInstanceAndNeverMutatesDefinition()
        {
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            TeslaPassiveEffect effect = CreateEffect(lightning, 0.05f);
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            SetField(definition, "projectileTypeDefinition", type);
            SetField(definition, "projectileTags", new[] { lightning });
            SetField(definition, "damage", 1f);
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(2, effect, type);
            passives.RebuildFace(5, effect, type);
            ProjectileRuntimeStats original = definition.BuildRuntimeStats();
            passives.NotifyProjectileSpawned(1, new ProjectileHandle(null, original));

            passives.RebuildFace(2, effect, type);

            Assert.That(passives.ModifyProjectileStats(2, original).Damage, Is.EqualTo(1f));
            Assert.That(passives.ModifyProjectileStats(5, original).Damage, Is.EqualTo(1.05f).Within(0.0001f));
            Assert.That(definition.BuildRuntimeStats().Damage, Is.EqualTo(1f));

            Destroy(type, lightning, effect, definition);
        }

        private static TeslaPassiveEffect CreateEffect(
            ProjectileTagDefinition lightning,
            float damagePerStack)
        {
            TeslaPassiveEffect effect = ScriptableObject.CreateInstance<TeslaPassiveEffect>();
            SetField(effect, "lightningTag", lightning);
            SetField(effect, "damagePerStack", damagePerStack);
            return effect;
        }

        private static ProjectileRuntimeStats Stats(
            ProjectileTypeDefinition type,
            ProjectileTagDefinition tag,
            float damage)
        {
            return new ProjectileRuntimeStats(
                "LightningOrb",
                "Lightning",
                type,
                new[] { tag },
                damage,
                15f,
                5f,
                4);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static void Destroy(params Object[] objects)
        {
            foreach (Object target in objects)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
