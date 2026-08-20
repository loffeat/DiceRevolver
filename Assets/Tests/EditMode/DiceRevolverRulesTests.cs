using DiceRevolver.Prototype;
using NUnit.Framework;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverRulesTests
    {
        [Test]
        public void FaceCountIsTheDomainFixedSix()
        {
            Assert.That(DiceRevolverRules.FaceCount, Is.EqualTo(6));
        }
    }
}
