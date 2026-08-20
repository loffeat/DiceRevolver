using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class DicePassiveRuntimeTests
    {
        [Test]
        public void DuplicatePassiveAssetsCreateIndependentFaceInstances()
        {
            RecordingPassiveEffect effect = ScriptableObject.CreateInstance<RecordingPassiveEffect>();
            using DicePassiveRuntime runtime = new DicePassiveRuntime();

            runtime.RebuildFace(2, effect);
            runtime.RebuildFace(5, effect);

            Assert.That(effect.Contexts, Is.EqualTo(new[] { 2, 5 }));
            Assert.That(effect.Created, Has.Count.EqualTo(2));
            Assert.That(effect.Created[0], Is.Not.SameAs(effect.Created[1]));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ReplacingOneFaceDisposesOnlyThatFacesOldInstance()
        {
            RecordingPassiveEffect original = ScriptableObject.CreateInstance<RecordingPassiveEffect>();
            RecordingPassiveEffect replacement = ScriptableObject.CreateInstance<RecordingPassiveEffect>();
            using DicePassiveRuntime runtime = new DicePassiveRuntime();
            runtime.RebuildFace(1, original);
            runtime.RebuildFace(2, original);

            runtime.RebuildFace(1, replacement);

            Assert.That(original.Created[0].DisposeCount, Is.EqualTo(1));
            Assert.That(original.Created[1].DisposeCount, Is.Zero);
            Assert.That(replacement.Created, Has.Count.EqualTo(1));

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(replacement);
        }

        [Test]
        public void OnePassiveFailureDoesNotPreventOtherInstancesFromReceivingNotifications()
        {
            ThrowingPassiveEffect throwing = ScriptableObject.CreateInstance<ThrowingPassiveEffect>();
            RecordingPassiveEffect healthy = ScriptableObject.CreateInstance<RecordingPassiveEffect>();
            List<Exception> exceptions = new List<Exception>();
            using DicePassiveRuntime runtime = new DicePassiveRuntime(null, exceptions.Add);
            runtime.RebuildFace(1, throwing);
            runtime.RebuildFace(2, healthy);

            runtime.NotifyReloadStarted();
            runtime.NotifyReloadCompleted();
            runtime.NotifyFaceConsumed(2);

            Assert.That(exceptions, Has.Count.EqualTo(3));
            Assert.That(healthy.Created[0].ReloadStartedCount, Is.EqualTo(1));
            Assert.That(healthy.Created[0].ReloadCompletedCount, Is.EqualTo(1));
            Assert.That(healthy.Created[0].ConsumedFaces, Is.EqualTo(new[] { 2 }));

            Object.DestroyImmediate(throwing);
            Object.DestroyImmediate(healthy);
        }

        [Test]
        public void DrawFilterFailureLeavesCandidatesAvailable()
        {
            ThrowingPassiveEffect throwing = ScriptableObject.CreateInstance<ThrowingPassiveEffect>();
            RecordingPassiveEffect healthy = ScriptableObject.CreateInstance<RecordingPassiveEffect>();
            List<Exception> exceptions = new List<Exception>();
            using DicePassiveRuntime runtime = new DicePassiveRuntime(null, exceptions.Add);
            runtime.RebuildFace(1, throwing);
            runtime.RebuildFace(2, healthy);

            DiceDrawConstraintResult result = runtime.FilterDrawCandidates(
                new[] { 1, 2, 3 },
                null);

            Assert.That(result.Candidates, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(exceptions, Has.Count.EqualTo(3));

            Object.DestroyImmediate(throwing);
            Object.DestroyImmediate(healthy);
        }

        private sealed class RecordingPassiveEffect : PassiveEventEffect
        {
            public List<int> Contexts { get; } = new List<int>();
            public List<RecordingPassiveRuntime> Created { get; } =
                new List<RecordingPassiveRuntime>();

            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                Contexts.Add(context.Face);
                RecordingPassiveRuntime runtime = new RecordingPassiveRuntime();
                Created.Add(runtime);
                return runtime;
            }
        }

        private sealed class ThrowingPassiveEffect : PassiveEventEffect
        {
            public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
            {
                return new ThrowingPassiveRuntime();
            }
        }

        private sealed class RecordingPassiveRuntime : IDicePassiveEffectRuntime
        {
            public int DisposeCount { get; private set; }
            public int ReloadStartedCount { get; private set; }
            public int ReloadCompletedCount { get; private set; }
            public List<int> ConsumedFaces { get; } = new List<int>();

            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return true;
            }

            public void OnReloadStarted()
            {
                ReloadStartedCount++;
            }

            public void OnReloadCompleted()
            {
                ReloadCompletedCount++;
            }

            public void OnFaceConsumed(int face)
            {
                ConsumedFaces.Add(face);
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private sealed class ThrowingPassiveRuntime : IDicePassiveEffectRuntime
        {
            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                throw new InvalidOperationException("draw failed");
            }

            public void OnReloadStarted()
            {
                throw new InvalidOperationException("reload start failed");
            }

            public void OnReloadCompleted()
            {
                throw new InvalidOperationException("reload completion failed");
            }

            public void OnFaceConsumed(int face)
            {
                throw new InvalidOperationException("consume failed");
            }

            public void Dispose()
            {
            }
        }
    }
}
