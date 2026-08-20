using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class BonusShotSpreadAllocator
    {
        private readonly Func<float, float, float> randomRange;
        private readonly List<float> currentFrameOffsets = new List<float>();
        private int currentFrame = int.MinValue;

        public BonusShotSpreadAllocator(Func<float, float, float> randomRange = null)
        {
            this.randomRange = randomRange ?? UnityEngine.Random.Range;
        }

        public float Next(float maximumAngle, float minimumSeparation)
        {
            return Next(Time.frameCount, maximumAngle, minimumSeparation);
        }

        public float Next(int frame, float maximumAngle, float minimumSeparation)
        {
            if (frame != currentFrame)
            {
                currentFrame = frame;
                currentFrameOffsets.Clear();
            }

            float max = Mathf.Max(0f, maximumAngle);
            float separation = Mathf.Max(0f, minimumSeparation);
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float candidate = Mathf.Clamp(randomRange(-max, max), -max, max);
                if (IsSeparated(candidate, separation))
                {
                    currentFrameOffsets.Add(candidate);
                    return candidate;
                }
            }

            float step = Mathf.Max(0.1f, separation);
            for (float candidate = -max; candidate <= max + 0.0001f; candidate += step)
            {
                if (IsSeparated(candidate, separation))
                {
                    currentFrameOffsets.Add(candidate);
                    return candidate;
                }
            }

            float fallback = currentFrameOffsets.Count % 2 == 0 ? -max : max;
            currentFrameOffsets.Add(fallback);
            return fallback;
        }

        private bool IsSeparated(float candidate, float minimumSeparation)
        {
            for (int index = 0; index < currentFrameOffsets.Count; index++)
            {
                if (Mathf.Abs(currentFrameOffsets[index] - candidate) < minimumSeparation)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
