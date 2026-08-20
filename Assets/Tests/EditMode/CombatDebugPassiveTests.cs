using System.Reflection;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class CombatDebugPassiveTests
    {
        [Test]
        public void TeslaReportsOnlyWhenLightningActuallyAddsAStack()
        {
            CombatDebugTrace trace = new CombatDebugTrace();
            ProjectileTypeDefinition type = ScriptableObject.CreateInstance<ProjectileTypeDefinition>();
            ProjectileTagDefinition lightning = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            ProjectileTagDefinition physical = ScriptableObject.CreateInstance<ProjectileTagDefinition>();
            TeslaPassiveEffect effect = ScriptableObject.CreateInstance<TeslaPassiveEffect>();
            effect.name = "特斯拉";
            SetField(effect, "lightningTag", lightning);
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.ConfigureDebugTrace(trace, () => 1f);
            passives.RebuildFace(2, effect, type);
            DiceFaceActivation source = Activation(trace, 5);

            passives.NotifyProjectileSpawned(
                5,
                new ProjectileHandle(null, Stats(type, physical)),
                source);
            Assert.That(trace.Records, Is.Empty);

            passives.NotifyProjectileSpawned(
                5,
                new ProjectileHandle(null, Stats(type, lightning)),
                source);

            Assert.That(trace.Records.Count, Is.EqualTo(1));
            Assert.That(trace.Records[0].EventType, Is.EqualTo(CombatDebugEventType.PassiveTriggered));
            Assert.That(trace.Records[0].Name, Is.EqualTo("特斯拉"));
            Assert.That(trace.Records[0].ChainId, Is.EqualTo(source.DebugScope.ChainId));

            Object.DestroyImmediate(type);
            Object.DestroyImmediate(lightning);
            Object.DestroyImmediate(physical);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void FinisherReportsWhenItRemovesItsFaceFromTheDrawCandidates()
        {
            CombatDebugTrace trace = new CombatDebugTrace();
            FinisherPassiveEffect effect = ScriptableObject.CreateInstance<FinisherPassiveEffect>();
            effect.name = "收尾者";
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.ConfigureDebugTrace(trace, () => 2f);
            passives.RebuildFace(4, effect);

            DiceDrawConstraintResult result = passives.FilterDrawCandidates(
                new[] { 1, 2, 3, 4 },
                null);

            Assert.That(result.Candidates, Has.No.Member(4));
            Assert.That(trace.Records.Count, Is.EqualTo(1));
            Assert.That(trace.Records[0].Name, Is.EqualTo("收尾者"));
            Assert.That(trace.Records[0].Detail, Is.EqualTo("骰面 4 保留到最后"));

            Object.DestroyImmediate(effect);
        }

        private static DiceFaceActivation Activation(CombatDebugTrace trace, int face)
        {
            DiceFaceActivation activation = new DiceFaceActivation(
                face,
                default,
                Vector3.zero,
                Vector3.forward,
                null,
                (System.Action<ProjectileSpawnRequest>)null,
                null,
                null);
            activation.ConfigureDebugScope(
                trace,
                trace.BeginActivation(face, false, default, 0f),
                () => 0f);
            return activation;
        }

        private static ProjectileRuntimeStats Stats(
            ProjectileTypeDefinition type,
            ProjectileTagDefinition tag)
        {
            return new ProjectileRuntimeStats("", "", type, new[] { tag }, 1f, 1f, 1f, 0);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
