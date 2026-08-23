using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class CombatDebugTraceTests
    {
        [Test]
        public void RecordsUseExecutionOrderAndChildActivationKeepsCausalChain()
        {
            CombatDebugTrace trace = new CombatDebugTrace(8);
            CombatDebugScope root = trace.BeginActivation(3, false, default, 1f);
            trace.Record(root, CombatDebugEventType.ShotStarted, "射击", "开始", null, 0, 1f);
            CombatDebugScope child = trace.BeginActivation(5, true, root, 1.1f);
            trace.Record(child, CombatDebugEventType.EffectTriggered, "基础", "雷电球", null, 1, 1.1f);

            Assert.That(trace.Records.Count, Is.EqualTo(2));
            Assert.That(trace.Records[0].Sequence, Is.EqualTo(1));
            Assert.That(trace.Records[1].Sequence, Is.EqualTo(2));
            Assert.That(child.ChainId, Is.EqualTo(root.ChainId));
            Assert.That(child.ParentActivationId, Is.EqualTo(root.ActivationId));
            Assert.That(trace.Records[0].Depth, Is.EqualTo(0));
            Assert.That(trace.Records[1].Depth, Is.EqualTo(2));
        }

        [Test]
        public void CapacityDropsOldestRecordsWithoutResettingSequence()
        {
            CombatDebugTrace trace = new CombatDebugTrace(2);
            CombatDebugScope scope = trace.BeginActivation(1, false, default, 0f);

            trace.Record(scope, CombatDebugEventType.ShotStarted, "射击", "一", null, 0, 0f);
            trace.Record(scope, CombatDebugEventType.EffectTriggered, "基础", "二", null, 1, 0f);
            trace.Record(scope, CombatDebugEventType.ShotEnded, "射击", "三", null, 0, 0f);

            Assert.That(trace.Records.Count, Is.EqualTo(2));
            Assert.That(trace.Records[0].Sequence, Is.EqualTo(2));
            Assert.That(trace.Records[1].Sequence, Is.EqualTo(3));
        }

        [Test]
        public void FormatterUsesSequenceFaceAndCausalIndentation()
        {
            CombatDebugRecord record = new CombatDebugRecord(
                12,
                4,
                8,
                7,
                2,
                6,
                CombatDebugEventType.EffectTriggered,
                "开火时",
                "电磁共鸣",
                "连接 3 个雷电球",
                2.5f);

            Assert.That(
                CombatDebugFormatter.Format(record),
                Is.EqualTo("    #12 骰面6 · 开火时：电磁共鸣：连接 3 个雷电球"));
        }

        [Test]
        public void BonusRequestCarriesTheActivationThatCausedIt()
        {
            DiceFaceActivation source = new DiceFaceActivation(
                2,
                default,
                UnityEngine.Vector3.zero,
                UnityEngine.Vector3.forward,
                null,
                (System.Action<ProjectileSpawnRequest>)null,
                null,
                null);
            BonusDiceActivationRequest request = new BonusDiceActivationRequest(
                4,
                new DiceEventBudget(8),
                7,
                8f,
                2f,
                source);

            Assert.That(request.SourceActivation, Is.SameAs(source));
        }
    }
}
