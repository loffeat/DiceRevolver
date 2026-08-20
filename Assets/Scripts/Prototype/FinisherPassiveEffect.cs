using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [CreateAssetMenu(
        fileName = "FinisherPassiveEffect",
        menuName = "Dice Revolver/Bullet Events/Passive/Finisher")]
    public sealed class FinisherPassiveEffect : PassiveEventEffect
    {
        public override IDicePassiveEffectRuntime CreateRuntime(PassiveBindingContext context)
        {
            return new FinisherRuntime();
        }

        private sealed class FinisherRuntime : IDicePassiveEffectRuntime, IDiceDrawPriorityProvider
        {
            public int DrawPriority => 1;

            public bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces)
            {
                return true;
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
