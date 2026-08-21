using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct EventSignal
    {
        public EventSignal(
            EventSignalType signalType,
            int equippedFace,
            int sourceFace,
            DiceFaceSlotType slot,
            DiceFaceActivation activation,
            DiceRevolverShotContext shot,
            ProjectileHandle projectile,
            Collider hitCollider,
            Vector3 hitPosition,
            IReadOnlyList<int> remainingFaces,
            int drawCandidate,
            ProjectileRuntimeStats currentStats,
            DiceEventBudget eventBudget,
            bool isBonusActivation,
            CombatDebugScope debugScope)
        {
            SignalType = signalType;
            EquippedFace = equippedFace;
            SourceFace = sourceFace;
            Slot = slot;
            Activation = activation;
            Shot = shot;
            Projectile = projectile;
            HitCollider = hitCollider;
            HitPosition = hitPosition;
            RemainingFaces = remainingFaces;
            DrawCandidate = drawCandidate;
            CurrentStats = currentStats;
            EventBudget = eventBudget;
            IsBonusActivation = isBonusActivation;
            DebugScope = debugScope;
        }

        public EventSignalType SignalType { get; }
        public int EquippedFace { get; }
        public int SourceFace { get; }
        public DiceFaceSlotType Slot { get; }
        public DiceFaceActivation Activation { get; }
        public DiceRevolverShotContext Shot { get; }
        public ProjectileHandle Projectile { get; }
        public Collider HitCollider { get; }
        public Vector3 HitPosition { get; }
        public IReadOnlyList<int> RemainingFaces { get; }
        public int DrawCandidate { get; }
        public ProjectileRuntimeStats CurrentStats { get; }
        public DiceEventBudget EventBudget { get; }
        public bool IsBonusActivation { get; }
        public CombatDebugScope DebugScope { get; }
    }
}
