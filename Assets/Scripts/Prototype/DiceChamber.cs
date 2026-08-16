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
