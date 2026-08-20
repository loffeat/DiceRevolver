using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class FinisherPassiveTests
    {
        [Test]
        public void FinisherFaceIsDrawnAfterEveryOrdinaryFace()
        {
            FinisherPassiveEffect effect = ScriptableObject.CreateInstance<FinisherPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(4, effect);
            DiceRevolverRuntime chamber = new DiceRevolverRuntime(100f, 2f, false, true);
            List<int> firstFive = Draw(chamber, passives, 5);

            DiceRevolverDrawResult finalDraw = chamber.TryBeginShot(
                1f,
                passives.FilterDrawCandidates);

            Assert.That(firstFive, Has.No.Member(4));
            Assert.That(finalDraw.Face, Is.EqualTo(4));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void MultipleFinishersShareTheFinalDrawPool()
        {
            FinisherPassiveEffect effect = ScriptableObject.CreateInstance<FinisherPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(5, effect);
            passives.RebuildFace(6, effect);
            DiceRevolverRuntime chamber = new DiceRevolverRuntime(100f, 2f, false, true);

            List<int> ordinaryDraws = Draw(chamber, passives, 4);
            List<int> finisherDraws = Draw(chamber, passives, 2, startTime: 1f);

            Assert.That(ordinaryDraws, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
            Assert.That(finisherDraws, Is.EquivalentTo(new[] { 5, 6 }));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ForcedFinisherWaitsUntilOrdinaryFacesAreConsumed()
        {
            Queue<int> drawIndices = new Queue<int>(new[] { 3, 0, 0, 0, 0, 0, 0 });
            DiceRevolverRuntime chamber = new DiceRevolverRuntime(
                100f,
                2f,
                false,
                true,
                _ => drawIndices.Dequeue());
            Assert.That(chamber.TryBeginShot(0f).Face, Is.EqualTo(4));
            Assert.That(chamber.TryRefillAndForceNextFace(4), Is.True);

            FinisherPassiveEffect effect = ScriptableObject.CreateInstance<FinisherPassiveEffect>();
            using DicePassiveRuntime passives = new DicePassiveRuntime();
            passives.RebuildFace(4, effect);

            List<int> ordinaryDraws = Draw(chamber, passives, 5, startTime: 0.1f);
            DiceRevolverDrawResult finalDraw = chamber.TryBeginShot(
                1f,
                passives.FilterDrawCandidates);

            Assert.That(ordinaryDraws, Has.No.Member(4));
            Assert.That(finalDraw.Face, Is.EqualTo(4));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void EmptyConstraintResultFallsBackAndWarnsOnce()
        {
            DenyAllPassiveEffect effect = ScriptableObject.CreateInstance<DenyAllPassiveEffect>();
            List<string> warnings = new List<string>();
            using DicePassiveRuntime passives = new DicePassiveRuntime(warnings.Add, null);
            passives.RebuildFace(1, effect);

            DiceDrawConstraintResult first = passives.FilterDrawCandidates(
                new[] { 1, 2, 3 },
                null);
            DiceDrawConstraintResult second = passives.FilterDrawCandidates(
                new[] { 1, 2, 3 },
                null);

            Assert.That(first.Candidates, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(second.Candidates, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(warnings, Has.Count.EqualTo(1));

            Object.DestroyImmediate(effect);
        }

        private static List<int> Draw(
            DiceRevolverRuntime chamber,
            DicePassiveRuntime passives,
            int count,
            float startTime = 0f)
        {
            List<int> faces = new List<int>();
            for (int shot = 0; shot < count; shot++)
            {
                DiceRevolverDrawResult result = chamber.TryBeginShot(
                    startTime + shot * 0.02f,
                    passives.FilterDrawCandidates);
                Assert.That(result.Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
                faces.Add(result.Face);
            }

            return faces;
        }

        private sealed class DenyAllPassiveEffect : PassiveEventEffect
        {
            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                return new DenyAllRuntime();
            }
        }

        private sealed class DenyAllRuntime : IDicePassiveEffectRuntime
        {
            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return false;
            }

            public void OnReloadStarted()
            {
            }

            public void OnReloadCompleted()
            {
            }

            public void OnFaceConsumed(int face)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
