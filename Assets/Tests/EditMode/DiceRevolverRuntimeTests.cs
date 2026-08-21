using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverRuntimeTests
    {
        [Test]
        public void DrawsAllSixFacesWithoutReplacement()
        {
            DiceRevolverRuntime runtime = new(5f, 1.8f, true, true);
            HashSet<int> faces = new();

            for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
            {
                DiceRevolverDrawResult result = runtime.TryBeginShot(i);

                Assert.That(result.Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
                faces.Add(result.Face);
            }

            Assert.That(faces, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(runtime.RemainingRounds, Is.Zero);
        }

        [Test]
        public void RemainingFaceSnapshotsAreReadOnlyAndDetachedFromLaterChamberChanges()
        {
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(
                100f,
                2f,
                false,
                true,
                _ => 3);
            IReadOnlyList<int> before = runtime.CreateRemainingFacesSnapshot();

            DiceRevolverDrawResult draw = runtime.TryBeginShot(0f);
            IReadOnlyList<int> after = runtime.CreateRemainingFacesSnapshot();

            Assert.That(draw.Face, Is.EqualTo(4));
            Assert.That(before, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(after, Is.EqualTo(new[] { 1, 2, 3, 5, 6 }));
            Assert.That(before, Is.Not.SameAs(after));
            Assert.That(before, Is.AssignableTo<IList<int>>());
            Assert.Throws<System.NotSupportedException>(() => ((IList<int>)before)[0] = 6);
            Assert.That(runtime.CreateRemainingFacesSnapshot(),
                Is.EqualTo(new[] { 1, 2, 3, 5, 6 }));
        }

        [Test]
        public void TryBeginShotReturnsCoolingDownUntilShotIntervalElapses()
        {
            DiceRevolverRuntime runtime = new(5f, 1.8f, true, true);

            Assert.That(runtime.TryBeginShot(0f).Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
            Assert.That(runtime.TryBeginShot(0.19f).Status, Is.EqualTo(DiceRevolverDrawStatus.CoolingDown));
            Assert.That(runtime.TryBeginShot(0.2f).Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
        }

        [Test]
        public void TryBeginShotReturnsReloadingWhileReloadIsInProgress()
        {
            DiceRevolverRuntime runtime = new(5f, 2f, true, true);

            Assert.That(runtime.TryBeginShot(0f).Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
            Assert.That(runtime.Tick(0f, true).ReloadStarted, Is.True);

            Assert.That(runtime.TryBeginShot(0.1f).Status, Is.EqualTo(DiceRevolverDrawStatus.Reloading));
        }

        [Test]
        public void EmptyChamberReturnsEmptyWhenAutomaticReloadIsDisabled()
        {
            DiceRevolverRuntime runtime = new(100f, 2f, false, true);

            for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
                runtime.TryBeginShot(i * 0.02f);

            Assert.That(runtime.TryBeginShot(1f).Status, Is.EqualTo(DiceRevolverDrawStatus.Empty));
        }

        [Test]
        public void CompleteShotStartsAutomaticReloadWhenChamberIsEmpty()
        {
            DiceRevolverRuntime runtime = new(100f, 2f, true, true);

            for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
                runtime.TryBeginShot(i * 0.02f);

            DiceRevolverRuntimeUpdate update = runtime.CompleteShot(1f);

            Assert.That(update.ReloadStarted, Is.True);
            Assert.That(runtime.IsReloading, Is.True);
        }

        [Test]
        public void EmptyDrawDoesNotBypassCompleteShotToStartAutomaticReload()
        {
            DiceRevolverRuntime runtime = new DiceRevolverRuntime(100f, 2f, true, true);

            for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
                runtime.TryBeginShot(i * 0.02f);

            DiceRevolverDrawResult emptyDraw = runtime.TryBeginShot(1f);

            Assert.That(emptyDraw.Status, Is.EqualTo(DiceRevolverDrawStatus.Empty));
            Assert.That(runtime.IsReloading, Is.False);
            Assert.That(runtime.CompleteShot(1f).ReloadStarted, Is.True);
            Assert.That(runtime.IsReloading, Is.True);
        }

        [Test]
        public void ManualReloadOnlyStartsWhenChamberIsNotFull()
        {
            DiceRevolverRuntime runtime = new(5f, 2f, true, true);

            Assert.That(runtime.Tick(0f, true).ReloadStarted, Is.False);
            runtime.TryBeginShot(0f);

            Assert.That(runtime.Tick(0.1f, true).ReloadStarted, Is.True);
        }

        [Test]
        public void CompletingReloadRestoresAllSixFaces()
        {
            DiceRevolverRuntime runtime = new(5f, 2f, true, true);
            runtime.TryBeginShot(0f);
            runtime.Tick(0f, true);

            Assert.That(runtime.GetReloadProgress(1f), Is.EqualTo(0.5f));
            Assert.That(runtime.Tick(2f, false).ReloadCompleted, Is.True);
            Assert.That(runtime.RemainingRounds, Is.EqualTo(DiceRevolverRules.FaceCount));
            Assert.That(runtime.GetReloadProgress(2f), Is.Zero);
        }

        [Test]
        public void ShotCanBeginAtTheSameTimeReloadCompletes()
        {
            DiceRevolverRuntime runtime = new(0.1f, 0.05f, true, true);
            runtime.TryBeginShot(0f);
            runtime.Tick(0f, true);

            Assert.That(runtime.Tick(0.05f, false).ReloadCompleted, Is.True);
            Assert.That(runtime.TryBeginShot(0.05f).Status, Is.EqualTo(DiceRevolverDrawStatus.Fired));
        }

        [Test]
        public void CompleteShotChecksAutomaticReloadAfterLoadedFourCapability()
        {
            DiceRevolverRuntime runtime = new(100f, 2f, true, true);
            for (int i = 0; i < DiceRevolverRules.FaceCount; i++)
                runtime.TryBeginShot(i * 0.02f);

            Assert.That(runtime.TryRefillAndForceNextFace(4), Is.True);
            Assert.That(runtime.CompleteShot(1f).ReloadStarted, Is.False);
            Assert.That(runtime.TryBeginShot(1.1f).Face, Is.EqualTo(4));
        }

        [Test]
        public void ReloadDurationIsClampedToMinimum()
        {
            DiceRevolverRuntime runtime = new(5f, 2f, true, true)
            {
                ReloadDuration = 0f
            };

            Assert.That(runtime.ReloadDuration, Is.EqualTo(0.05f));
        }
    }
}
