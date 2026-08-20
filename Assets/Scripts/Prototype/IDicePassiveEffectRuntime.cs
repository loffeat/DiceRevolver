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
        bool OnProjectileSpawned(int sourceFace, ProjectileHandle projectile);
    }

    public interface IDiceProjectileHitObserver
    {
        void OnProjectileHit(
            DiceRevolverShotContext shot,
            UnityEngine.Collider hitCollider,
            UnityEngine.Vector3 hitPosition);
    }
}
