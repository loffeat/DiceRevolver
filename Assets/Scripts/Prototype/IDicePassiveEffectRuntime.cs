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

    public interface IDiceProjectileStatsModifier
    {
        ProjectileRuntimeStats ModifyProjectileStats(
            int sourceFace,
            ProjectileRuntimeStats stats);
    }

    public interface IDiceProjectileSpawnObserver
    {
        void OnProjectileSpawned(int sourceFace, ProjectileHandle projectile);
    }
}
