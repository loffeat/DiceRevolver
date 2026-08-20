using System.Collections.Generic;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace DiceRevolver.Tests
{
    public sealed class BonusShotSpreadAllocatorTests
    {
        [Test]
        public void SameFrameOffsetsStayInRangeAndRespectMinimumSeparation()
        {
            Queue<float> samples = new Queue<float>(new[] { -8f, -7f, -4f, 0f, 4f });
            BonusShotSpreadAllocator allocator =
                new BonusShotSpreadAllocator((minimum, maximum) => samples.Dequeue());

            float first = allocator.Next(12, 8f, 2f);
            float second = allocator.Next(12, 8f, 2f);
            float third = allocator.Next(12, 8f, 2f);

            Assert.That(first, Is.InRange(-8f, 8f));
            Assert.That(second, Is.InRange(-8f, 8f));
            Assert.That(third, Is.InRange(-8f, 8f));
            Assert.That(Mathf.Abs(first - second), Is.GreaterThanOrEqualTo(2f));
            Assert.That(Mathf.Abs(first - third), Is.GreaterThanOrEqualTo(2f));
            Assert.That(Mathf.Abs(second - third), Is.GreaterThanOrEqualTo(2f));
        }

        [Test]
        public void NewFrameClearsPreviousOffsets()
        {
            Queue<float> samples = new Queue<float>(new[] { 1f, 1f });
            BonusShotSpreadAllocator allocator =
                new BonusShotSpreadAllocator((minimum, maximum) => samples.Dequeue());

            float first = allocator.Next(20, 8f, 2f);
            float nextFrame = allocator.Next(21, 8f, 2f);

            Assert.That(first, Is.EqualTo(1f));
            Assert.That(nextFrame, Is.EqualTo(1f));
        }
    }
}
