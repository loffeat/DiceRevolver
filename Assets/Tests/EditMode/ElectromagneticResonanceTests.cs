using System;
using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class ElectromagneticResonanceTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = owned.Count - 1; index >= 0; index--)
            {
                if (owned[index] != null)
                {
                    Object.DestroyImmediate(owned[index]);
                }
            }

            owned.Clear();
        }

        [Test]
        public void SelectionUsesAtMostThreeDistinctCandidatesWithoutReplacement()
        {
            List<ProjectileHandle> candidates = new List<ProjectileHandle>();
            for (int index = 0; index < 5; index++)
            {
                Projectile projectile = CreateProjectile($"Candidate {index}", Vector3.right * index);
                candidates.Add(new ProjectileHandle(projectile, default));
            }

            Queue<int> indices = new Queue<int>(new[] { 4, 0, 1 });
            IReadOnlyList<ProjectileHandle> selected =
                ElectromagneticResonanceEffect.SelectTargets(
                    candidates,
                    3,
                    count => indices.Dequeue() % count);

            Assert.That(selected, Has.Count.EqualTo(3));
            Assert.That(selected[0].Projectile, Is.SameAs(candidates[4].Projectile));
            Assert.That(selected[1].Projectile, Is.SameAs(candidates[0].Projectile));
            Assert.That(selected[2].Projectile, Is.SameAs(candidates[2].Projectile));
        }

        [Test]
        public void TriggerSelectsOnlyNearbySameRegistryLightningProjectiles()
        {
            ProjectileTagDefinition lightning = Own(
                ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileTagDefinition physical = Own(
                ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            LightningChainDefinition chain = Own(
                ScriptableObject.CreateInstance<LightningChainDefinition>());
            ElectromagneticResonanceEffect resonance = Own(
                ScriptableObject.CreateInstance<ElectromagneticResonanceEffect>());
            SetField(resonance, "lightningTag", lightning);
            SetField(resonance, "chainDefinition", chain);
            SetField(resonance, "searchRadius", 6f);
            SetField(resonance, "maximumConnections", 3);
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            Projectile primary = CreateProjectile("Primary", Vector3.zero);
            Projectile near = CreateProjectile("Near", Vector3.right * 2f);
            Projectile far = CreateProjectile("Far", Vector3.right * 7f);
            Projectile wrongTag = CreateProjectile("Wrong", Vector3.forward);
            ProjectileRuntimeStats lightningStats = Stats(lightning);
            ProjectileHandle primaryHandle = registry.Register(primary, lightningStats);
            registry.Register(near, lightningStats);
            registry.Register(far, lightningStats);
            registry.Register(wrongTag, Stats(physical));
            IReadOnlyList<ProjectileHandle> requestedTargets = null;
            DiceShotPipeline pipeline = CreatePipeline(primaryHandle);
            pipeline.ConfigureLightningServices(
                registry,
                (_, targets, _) =>
                {
                    requestedTargets = new List<ProjectileHandle>(targets);
                    return true;
                });

            pipeline.ExecuteShot(
                1,
                Snapshot(
                    Effect(context => context.RequestProjectile(
                        Own(ScriptableObject.CreateInstance<ProjectileDefinition>()),
                        AttackEffectOverride.ForceDisabled,
                        true)),
                    resonance),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(requestedTargets, Is.Not.Null);
            Assert.That(requestedTargets, Has.Count.EqualTo(1));
            Assert.That(requestedTargets[0].Projectile, Is.SameAs(near));
        }

        [Test]
        public void NonLightningPrimaryDoesNotRequestAChain()
        {
            ProjectileTagDefinition lightning = Own(
                ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ProjectileTagDefinition physical = Own(
                ScriptableObject.CreateInstance<ProjectileTagDefinition>());
            ElectromagneticResonanceEffect resonance = Own(
                ScriptableObject.CreateInstance<ElectromagneticResonanceEffect>());
            SetField(resonance, "lightningTag", lightning);
            SetField(resonance, "chainDefinition", Own(
                ScriptableObject.CreateInstance<LightningChainDefinition>()));
            OwnedProjectileRegistry registry = new OwnedProjectileRegistry();
            Projectile primary = CreateProjectile("Physical Primary", Vector3.zero);
            ProjectileHandle primaryHandle = registry.Register(primary, Stats(physical));
            int requestCount = 0;
            DiceShotPipeline pipeline = CreatePipeline(primaryHandle);
            pipeline.ConfigureLightningServices(
                registry,
                (_, _, _) =>
                {
                    requestCount++;
                    return true;
                });

            pipeline.ExecuteShot(
                2,
                Snapshot(
                    Effect(context => context.RequestProjectile(
                        Own(ScriptableObject.CreateInstance<ProjectileDefinition>()),
                        AttackEffectOverride.ForceDisabled,
                        true)),
                    resonance),
                Vector3.zero,
                Vector3.forward,
                8,
                null,
                null);

            Assert.That(requestCount, Is.Zero);
        }

        private DiceShotPipeline CreatePipeline(ProjectileHandle primary)
        {
            return new DiceShotPipeline(
                () => 0f,
                (_, _) => primary,
                null,
                null,
                null);
        }

        private DiceFaceConfigurationSnapshot Snapshot(
            BulletEventEffect baseEffect,
            BulletEventEffect onFire)
        {
            return new DiceFaceConfigurationSnapshot(
                Entry(DiceFaceSlotType.Base, baseEffect),
                Entry(DiceFaceSlotType.OnFire, onFire),
                null,
                null);
        }

        private DiceFaceEntry Entry(DiceFaceSlotType slot, BulletEventEffect effect)
        {
            DiceFaceEntry entry = Own(ScriptableObject.CreateInstance<DiceFaceEntry>());
            SetField(entry, "slotType", slot);
            SetField(entry, "effect", effect);
            return entry;
        }

        private RecordingEffect Effect(Action<BulletEventContext> action)
        {
            RecordingEffect effect = Own(ScriptableObject.CreateInstance<RecordingEffect>());
            effect.Action = action;
            return effect;
        }

        private Projectile CreateProjectile(string name, Vector3 position)
        {
            GameObject owner = Own(new GameObject(name));
            owner.transform.position = position;
            return owner.AddComponent<Projectile>();
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

        private T Own<T>(T target) where T : Object
        {
            owned.Add(target);
            return target;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private sealed class RecordingEffect : BulletEventEffect
        {
            public Action<BulletEventContext> Action { get; set; }

            public override void Trigger(BulletEventContext context)
            {
                Action?.Invoke(context);
            }
        }
    }
}
