using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class RoundProjectileStatisticTests
    {
        [Test]
        public void StatisticCountsByDefinitionAndResets()
        {
            RoundProjectileStatistic statistic = new RoundProjectileStatistic();
            ProjectileDefinition orb = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ProjectileDefinition bullet = ScriptableObject.CreateInstance<ProjectileDefinition>();
            try
            {
                statistic.Increment(orb);
                statistic.Increment(orb);
                statistic.Increment(bullet);
                Assert.That(statistic.Count(orb), Is.EqualTo(2));
                Assert.That(statistic.Count(bullet), Is.EqualTo(1));

                statistic.Reset();
                Assert.That(statistic.Count(orb), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bullet);
                UnityEngine.Object.DestroyImmediate(orb);
            }
        }

        [Test]
        public void ScaleActivationDamageMultipliesByRoundCount()
        {
            RoundProjectileStatistic statistic = new RoundProjectileStatistic();
            ProjectileDefinition orb = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ScaleActivationDamageFromStatisticResultModule module =
                ScriptableObject.CreateInstance<ScaleActivationDamageFromStatisticResultModule>();
            SetField(module, "statisticDefinition", orb);
            SetField(module, "damagePerCount", 0.05f);
            DiceFaceActivation activation = new DiceFaceActivation(
                1, default, Vector3.zero, Vector3.forward, null, null, null, null);
            TestRuleServices services = new TestRuleServices(statistic);
            try
            {
                statistic.Increment(orb);
                statistic.Increment(orb);

                EventResult result = module.Execute(new EventExecutionContext(
                    SignalWithActivation(activation),
                    null,
                    services));

                Assert.That(result.Status, Is.EqualTo(EventResultStatus.Success));
                Assert.That(activation.DamageMultiplier, Is.EqualTo(1.1f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(module);
                UnityEngine.Object.DestroyImmediate(orb);
            }
        }

        [Test]
        public void ScaleActivationDamageSkipsWhenNoMatchingOrbsWereSpawned()
        {
            RoundProjectileStatistic statistic = new RoundProjectileStatistic();
            ProjectileDefinition orb = ScriptableObject.CreateInstance<ProjectileDefinition>();
            ScaleActivationDamageFromStatisticResultModule module =
                ScriptableObject.CreateInstance<ScaleActivationDamageFromStatisticResultModule>();
            SetField(module, "statisticDefinition", orb);
            SetField(module, "damagePerCount", 0.05f);
            DiceFaceActivation activation = new DiceFaceActivation(
                1, default, Vector3.zero, Vector3.forward, null, null, null, null);
            TestRuleServices services = new TestRuleServices(statistic);
            try
            {
                EventResult result = module.Execute(new EventExecutionContext(
                    SignalWithActivation(activation),
                    null,
                    services));

                Assert.That(result.Status, Is.EqualTo(EventResultStatus.Skipped));
                Assert.That(activation.DamageMultiplier, Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(module);
                UnityEngine.Object.DestroyImmediate(orb);
            }
        }

        private static EventSignal SignalWithActivation(DiceFaceActivation activation)
        {
            return new EventSignal(
                EventSignalType.OnFire,
                5,
                5,
                DiceFaceSlotType.OnFire,
                activation,
                null,
                default,
                null,
                Vector3.zero,
                System.Array.Empty<int>(),
                0,
                default,
                activation != null ? activation.EventBudget : null,
                false,
                default);
        }

        private static void SetField(object target, string name, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

        private sealed class TestRuleServices : IEventRuleServices
        {
            private readonly RoundProjectileStatistic statistic;

            public TestRuleServices(RoundProjectileStatistic statistic)
            {
                this.statistic = statistic;
            }

            public DiceEventBudget EventBudget => null;
            public RoundProjectileStatistic RoundProjectileStatistic => statistic;

            public bool RequestProjectile(ProjectileDefinition definition, Vector3 origin, Vector3 direction,
                AttackEffectOverride attackEffectOverride, bool isPrimary) => false;
            public bool Schedule(float delaySeconds, Action callback) => false;
            public bool RequestBonusActivation(int face, float maximumSpreadAngle,
                float minimumSpreadSeparation, EventRuleDefinition sourceRule) => false;
            public bool RequestRefillAndForceNextFace(int face) => false;
            public bool RequestLightningChain(ProjectileHandle origin,
                IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition) => false;
            public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay) => false;
            public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius,
                ProjectileTagDefinition requiredTag, Projectile excludedProjectile) =>
                Array.Empty<ProjectileHandle>();
            public void SetDrawPriority(int priority) { }
            public void RejectDrawCandidate(string reason) { }
            public void MultiplyProjectileDamage(float multiplier) { }
            public void RecordRuleDebug(EventRuleDefinition rule, string stage,
                string description, EventResultStatus status) { }
            public void ReportException(Exception exception, ScriptableObject module) { }
        }
    }
}
