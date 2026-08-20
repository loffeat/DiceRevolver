using System;
using System.Collections.Generic;

namespace DiceRevolver.Prototype
{
    public interface IDicePassiveEffectRuntime : IDisposable
    {
        bool AllowsDraw(int face, IReadOnlyList<int> remainingFaces);
        void OnReloadStarted();
        void OnReloadCompleted();
        void OnFaceConsumed(int face);
    }

    public interface IDiceDrawPriorityProvider
    {
        int DrawPriority { get; }
    }
}
