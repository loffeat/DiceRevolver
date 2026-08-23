using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class RelicTests
    {
        [Test]
        public void FirstDrawRelicForcesFaceAtRoundStartUnlessPassive()
        {
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(5f, 2f, true, true);
            runtime.RebuildActiveFaces(new[] { 3 });
            LoadedFirstFaceRelicDefinition relic = ScriptableObject.CreateInstance<LoadedFirstFaceRelicDefinition>();
            relic.Face = 4;
            try
            {
                relic.ApplyAtRoundStart(new RelicContext(runtime, new[] { 3 }, 6));
                Assert.That(runtime.TryBeginShot(0f).Face, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(relic);
            }
        }

        [Test]
        public void FirstDrawRelicIgnoresPassiveTargetFace()
        {
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(5f, 2f, true, true);
            runtime.RebuildActiveFaces(new[] { 4 });
            LoadedFirstFaceRelicDefinition relic = ScriptableObject.CreateInstance<LoadedFirstFaceRelicDefinition>();
            relic.Face = 4;
            try
            {
                relic.ApplyAtRoundStart(new RelicContext(runtime, new[] { 4 }, 6));
                // 面4被动：不强制，首抽为活动面（1/2/3/5/6）
                DiceRevolverDrawResult result = runtime.TryBeginShot(0f);
                Assert.That(result.Face, Is.Not.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(relic);
            }
        }

        [Test]
        public void RelicRuntimeAppliesAllRelicsInOrder()
        {
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(5f, 2f, true, true);
            runtime.RebuildActiveFaces(new int[0]);
            RecordingRelic first = ScriptableObject.CreateInstance<RecordingRelic>();
            RecordingRelic second = ScriptableObject.CreateInstance<RecordingRelic>();
            RelicRuntime relicRuntime = new RelicRuntime();
            try
            {
                relicRuntime.SetRelics(new[] { first, second });
                relicRuntime.ApplyRoundStart(new RelicContext(runtime, new int[0], 6));
                Assert.That(first.Applications, Is.EqualTo(1));
                Assert.That(second.Applications, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(first);
            }
        }

        private sealed class RecordingRelic : RelicDefinition
        {
            public int Applications { get; private set; }

            public override void ApplyAtRoundStart(RelicContext context)
            {
                Applications++;
            }
        }
    }
}
