using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class DiceRevolverRulesTests
    {
        [Test]
        public void FaceCountIsTheDomainFixedSix()
        {
            Assert.That(DiceRevolverRules.FaceCount, Is.EqualTo(6));
        }

        [Test]
        public void AmmoFaceClampsDisplayedFaceToTheDomainRule()
        {
            GameObject owner = new GameObject("AmmoFace");
            DiceRevolverAmmoFace ammoFace = owner.AddComponent<DiceRevolverAmmoFace>();

            ammoFace.FaceValue = DiceRevolverRules.FaceCount + 1;

            Assert.That(ammoFace.FaceValue, Is.EqualTo(DiceRevolverRules.FaceCount));
            Object.DestroyImmediate(owner);
        }
    }
}
