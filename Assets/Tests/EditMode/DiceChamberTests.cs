using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class DiceChamberTests
    {
        [Test]
        public void ResetRestoresAllSixFaces()
        {
            DiceChamber chamber = new DiceChamber(6);

            chamber.TryDrawFace(out _);
            chamber.Reset();

            Assert.That(chamber.RemainingCount, Is.EqualTo(6));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, chamber.RemainingFaces);
        }

        [Test]
        public void TryRefillFaceAddsMissingFaceOnce()
        {
            DiceChamber chamber = new DiceChamber(6);

            while (chamber.ContainsFace(4))
            {
                chamber.TryDrawFace(out _);
            }

            Assert.That(chamber.TryRefillFace(4), Is.True);
            Assert.That(chamber.TryRefillFace(4), Is.False);
            Assert.That(chamber.ContainsFace(4), Is.True);
        }

        [Test]
        public void TryForceNextFaceMakesNextDrawReturnThatFace()
        {
            DiceChamber chamber = new DiceChamber(6);

            Assert.That(chamber.TryForceNextFace(4), Is.True);

            chamber.TryDrawFace(out int face);

            Assert.That(face, Is.EqualTo(4));
            Assert.That(chamber.ContainsFace(4), Is.False);
        }

        [Test]
        public void TryForceNextFaceFailsWhenFaceIsNotInPool()
        {
            DiceChamber chamber = new DiceChamber(1);
            chamber.TryDrawFace(out _);

            Assert.That(chamber.TryForceNextFace(1), Is.False);
        }
    }
}
