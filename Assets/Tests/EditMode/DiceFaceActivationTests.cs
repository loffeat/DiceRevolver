using System.Collections.Generic;
using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceFaceActivationTests
    {
        [Test]
        public void PrimaryProjectileAlwaysAllowsHitEffects()
        {
            ProjectileDefinition definition = CreateDefinition(false);
            List<ProjectileSpawnRequest> requests = new List<ProjectileSpawnRequest>();
            DiceFaceActivation activation = CreateActivation(requests, 4);

            bool accepted = activation.RequestProjectile(
                definition,
                AttackEffectOverride.ForceDisabled,
                true,
                Vector3.zero,
                Vector3.forward);

            Assert.That(accepted, Is.True);
            Assert.That(requests[0].CanTriggerHitEffects, Is.True);
            Assert.That(activation.PrimaryProjectileDefinition, Is.SameAs(definition));

            Object.DestroyImmediate(definition);
        }

        [TestCase(false, AttackEffectOverride.UseProjectileDefault, false)]
        [TestCase(true, AttackEffectOverride.UseProjectileDefault, true)]
        [TestCase(false, AttackEffectOverride.ForceEnabled, true)]
        [TestCase(true, AttackEffectOverride.ForceDisabled, false)]
        public void NonPrimaryProjectileResolvesAttackEffectPolicy(
            bool projectileDefault,
            AttackEffectOverride policy,
            bool expected)
        {
            ProjectileDefinition definition = CreateDefinition(projectileDefault);
            List<ProjectileSpawnRequest> requests = new List<ProjectileSpawnRequest>();
            DiceFaceActivation activation = CreateActivation(requests, 4);

            activation.RequestProjectile(
                definition,
                policy,
                false,
                Vector3.zero,
                Vector3.forward);

            Assert.That(requests[0].CanTriggerHitEffects, Is.EqualTo(expected));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ActivationRejectsSpawnsAfterBudgetIsExhausted()
        {
            ProjectileDefinition definition = CreateDefinition(false);
            List<ProjectileSpawnRequest> requests = new List<ProjectileSpawnRequest>();
            DiceFaceActivation activation = CreateActivation(requests, 2);

            bool first = activation.RequestProjectile(
                definition,
                AttackEffectOverride.UseProjectileDefault,
                false,
                Vector3.zero,
                Vector3.forward);
            bool second = activation.RequestProjectile(
                definition,
                AttackEffectOverride.UseProjectileDefault,
                false,
                Vector3.zero,
                Vector3.forward);
            bool third = activation.RequestProjectile(
                definition,
                AttackEffectOverride.UseProjectileDefault,
                false,
                Vector3.zero,
                Vector3.forward);

            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(third, Is.False);
            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(activation.RemainingEventBudget, Is.Zero);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ActivationRetainsTheCapturedFourSlotSnapshot()
        {
            DiceFaceEntry onHitEntry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            TestBulletEventEffect onHitEffect = ScriptableObject.CreateInstance<TestBulletEventEffect>();
            SetPrivate(onHitEntry, "slotType", DiceFaceSlotType.OnHit);
            SetPrivate(onHitEntry, "effect", onHitEffect);
            DiceFaceConfigurationSnapshot snapshot = new DiceFaceConfigurationSnapshot(
                null,
                null,
                onHitEntry,
                null);

            DiceFaceActivation activation = new DiceFaceActivation(
                2,
                snapshot,
                Vector3.zero,
                Vector3.forward,
                (_, _) => { },
                _ => { },
                _ => false,
                _ => { });

            Assert.That(
                activation.Configuration.GetEntry(DiceFaceSlotType.OnHit),
                Is.SameAs(onHitEntry));

            Object.DestroyImmediate(onHitEntry);
            Object.DestroyImmediate(onHitEffect);
        }

        [Test]
        public void ActivationClampsBudgetToAtLeastOneAndWarnsOnce()
        {
            List<string> warnings = new List<string>();
            DiceFaceActivation activation = CreateActivation(
                eventBudget: 0,
                warningAction: warnings.Add);

            Assert.That(activation.RemainingEventBudget, Is.EqualTo(1));
            Assert.That(activation.TryConsumeEventBudget(), Is.True);
            Assert.That(activation.TryConsumeEventBudget(), Is.False);
            Assert.That(activation.TryConsumeEventBudget(), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void ActivationAndEventContextDoNotExposeGunOrChamber()
        {
            Assert.That(typeof(DiceFaceActivation).GetProperty("Gun"), Is.Null);
            Assert.That(typeof(DiceFaceActivation).GetProperty("Chamber"), Is.Null);
            Assert.That(typeof(BulletEventContext).GetProperty("Gun"), Is.Null);
            Assert.That(typeof(BulletEventContext).GetProperty("Chamber"), Is.Null);
        }

        private static DiceFaceActivation CreateActivation(
            ICollection<ProjectileSpawnRequest> requests = null,
            int eventBudget = DiceFaceActivation.DefaultEventBudget,
            System.Func<int, bool> refillAndForceNextFaceAction = null,
            System.Action<string> warningAction = null)
        {
            return new DiceFaceActivation(
                2,
                default,
                Vector3.zero,
                Vector3.forward,
                (_, _) => { },
                request => requests?.Add(request),
                refillAndForceNextFaceAction,
                warningAction,
                eventBudget);
        }

        private static ProjectileDefinition CreateDefinition(bool defaultAttackEffect)
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            FieldInfo field = typeof(ProjectileDefinition).GetField(
                "defaultAttackEffect",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, defaultAttackEffect);
            return definition;
        }

        private static void SetPrivate<TValue>(Object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class TestBulletEventEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }
    }
}
