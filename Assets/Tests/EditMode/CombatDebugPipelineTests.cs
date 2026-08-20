using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class CombatDebugPipelineTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(created[index]);
            }

            created.Clear();
        }

        [Test]
        public void PipelineReportsConfiguredEffectsInActualLifecycleOrder()
        {
            CombatDebugTrace trace = new CombatDebugTrace(16);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.ConfigureDebugTrace(trace);
            DiceFaceConfigurationSnapshot configuration = new DiceFaceConfigurationSnapshot(
                Entry("基础左轮子弹", DiceFaceSlotType.Base),
                Entry("双发射击", DiceFaceSlotType.OnFire),
                null,
                Entry("链式反应", DiceFaceSlotType.OnFireEnd));

            pipeline.ExecuteShot(2, configuration, Vector3.zero, Vector3.forward, 16, null, null);

            Assert.That(trace.Records.Count, Is.EqualTo(5));
            Assert.That(trace.Records[0].EventType, Is.EqualTo(CombatDebugEventType.ShotStarted));
            Assert.That(trace.Records[1].Name, Is.EqualTo("基础左轮子弹"));
            Assert.That(trace.Records[2].Name, Is.EqualTo("双发射击"));
            Assert.That(trace.Records[3].EventType, Is.EqualTo(CombatDebugEventType.ShotEnded));
            Assert.That(trace.Records[4].Name, Is.EqualTo("链式反应"));
            Assert.That(trace.Records[4].Sequence, Is.EqualTo(5));
        }

        [Test]
        public void BonusActivationIsNestedUnderItsSourceActivation()
        {
            CombatDebugTrace trace = new CombatDebugTrace(16);
            DiceShotPipeline pipeline = CreatePipeline();
            pipeline.ConfigureDebugTrace(trace);
            DiceFaceConfigurationSnapshot configuration = new DiceFaceConfigurationSnapshot(
                Entry("基础左轮子弹", DiceFaceSlotType.Base), null, null, null);
            DiceFaceActivation source = pipeline.ExecuteShot(
                1, configuration, Vector3.zero, Vector3.forward, 16, null, null);

            pipeline.ExecuteBonusShot(
                4,
                configuration,
                Vector3.zero,
                Vector3.forward,
                source.EventBudget,
                0,
                source,
                null,
                null);

            CombatDebugRecord rootStart = trace.Records[0];
            CombatDebugRecord bonusStart = trace.Records[3];
            Assert.That(bonusStart.EventType, Is.EqualTo(CombatDebugEventType.BonusShotStarted));
            Assert.That(bonusStart.ChainId, Is.EqualTo(rootStart.ChainId));
            Assert.That(bonusStart.ParentActivationId, Is.EqualTo(rootStart.ActivationId));
            Assert.That(bonusStart.Depth, Is.EqualTo(1));
        }

        [Test]
        public void DelayedResultAppearsOnlyWhenScheduledCallbackExecutes()
        {
            float time = 0f;
            CombatDebugTrace trace = new CombatDebugTrace(16);
            DiceShotPipeline pipeline = new DiceShotPipeline(
                () => time,
                (activation, request) => default,
                face => true,
                null,
                (exception, context) => Assert.Fail(exception.ToString()));
            pipeline.ConfigureDebugTrace(trace);
            DelayedForceEffect effect = ScriptableObject.CreateInstance<DelayedForceEffect>();
            created.Add(effect);
            DiceFaceEntry entry = Entry("延迟检索", DiceFaceSlotType.OnFire, effect);

            pipeline.ExecuteShot(
                2,
                new DiceFaceConfigurationSnapshot(null, entry, null, null),
                Vector3.zero,
                Vector3.forward,
                16,
                null,
                null);

            Assert.That(trace.Records, Has.None.Matches<CombatDebugRecord>(item => item.Name == "执行延迟事件"));
            time = 0.3f;
            pipeline.Tick(time);

            Assert.That(trace.Records, Has.Some.Matches<CombatDebugRecord>(item => item.Name == "执行延迟事件"));
            Assert.That(trace.Records, Has.Some.Matches<CombatDebugRecord>(item => item.Name == "检索骰面" && item.Detail == "骰面 4"));
        }

        private DiceShotPipeline CreatePipeline()
        {
            return new DiceShotPipeline(
                () => 0f,
                (activation, request) => default,
                face => true,
                null,
                (exception, context) => Assert.Fail(exception.ToString()));
        }

        private DiceFaceEntry Entry(string displayName, DiceFaceSlotType slot)
        {
            RecordingEffect effect = ScriptableObject.CreateInstance<RecordingEffect>();
            effect.name = displayName + "Effect";
            created.Add(effect);
            return Entry(displayName, slot, effect);
        }

        private DiceFaceEntry Entry(string displayName, DiceFaceSlotType slot, BulletEventEffect effect)
        {
            DiceFaceEntry entry = ScriptableObject.CreateInstance<DiceFaceEntry>();
            created.Add(entry);
            SerializedObject serialized = new SerializedObject(entry);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("slotType").enumValueIndex = (int)slot;
            serialized.FindProperty("effect").objectReferenceValue = effect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private sealed class RecordingEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
            }
        }

        private sealed class DelayedForceEffect : BulletEventEffect
        {
            public override void Trigger(BulletEventContext context)
            {
                context.Schedule(0.25f, delayed => delayed.RequestRefillAndForceNextFace(4));
            }
        }
    }
}
