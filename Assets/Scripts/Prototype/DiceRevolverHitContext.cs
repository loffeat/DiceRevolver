using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceRevolverHitContext
    {
        public DiceRevolverHitContext(DiceRevolverShotContext shot, Collider hitCollider)
            : this(shot, hitCollider, Vector3.zero)
        {
        }

        public DiceRevolverHitContext(DiceRevolverShotContext shot, Collider hitCollider, Vector3 hitPosition)
        {
            Shot = shot;
            HitCollider = hitCollider;
            HitPosition = hitPosition;
        }

        public DiceRevolverShotContext Shot { get; }
        public Collider HitCollider { get; }
        public Vector3 HitPosition { get; }
    }
}
