# Task 2 Review Package

## Changed Files

- `Assets/Scripts/Prototype/DiceChamber.cs`
- `Assets/Tests/EditMode/DiceChamberTests.cs`
- `.superpowers/sdd/2026-08-16-dice-face-build-system/task-2-report.md`

## Notes

Git commit-range diff is unavailable because this repository has no valid `HEAD` in the sandbox and git index writes are permission-blocked. Review the changed files directly against the task brief.

Controller verification was run in a temporary project copy. The EditMode results file reported `total="4" passed="4" failed="0"`.

## File: Assets/Scripts/Prototype/DiceChamber.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceChamber
    {
        private readonly List<int> remainingFaces = new();
        private readonly int faceCount;
        private int? forcedNextFace;

        public DiceChamber(int faceCount = 6)
        {
            this.faceCount = Mathf.Max(1, faceCount);
            Reset();
        }

        public IReadOnlyList<int> RemainingFaces => remainingFaces;
        public int RemainingCount => remainingFaces.Count;
        public bool IsEmpty => remainingFaces.Count == 0;

        public bool ContainsFace(int face)
        {
            return remainingFaces.Contains(face);
        }

        public bool TryRefillFace(int face)
        {
            if (face < 1 || face > faceCount || remainingFaces.Contains(face))
            {
                return false;
            }

            remainingFaces.Add(face);
            remainingFaces.Sort();
            return true;
        }

        public bool TryForceNextFace(int face)
        {
            if (!remainingFaces.Contains(face))
            {
                return false;
            }

            forcedNextFace = face;
            return true;
        }

        public void Reset()
        {
            remainingFaces.Clear();
            forcedNextFace = null;
            for (int face = 1; face <= faceCount; face++)
            {
                remainingFaces.Add(face);
            }
        }

        public bool TryDrawFace(out int face)
        {
            if (forcedNextFace.HasValue)
            {
                if (remainingFaces.Contains(forcedNextFace.Value))
                {
                    face = forcedNextFace.Value;
                    forcedNextFace = null;
                    remainingFaces.Remove(face);
                    return true;
                }

                forcedNextFace = null;
            }

            if (remainingFaces.Count == 0)
            {
                face = 0;
                return false;
            }

            int index = Random.Range(0, remainingFaces.Count);
            face = remainingFaces[index];
            remainingFaces.RemoveAt(index);
            return true;
        }
    }
}
```

## File: Assets/Tests/EditMode/DiceChamberTests.cs

```csharp
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
```

