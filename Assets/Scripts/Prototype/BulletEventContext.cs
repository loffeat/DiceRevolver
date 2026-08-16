using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct BulletEventContext
    {
        private readonly Action<DiceRevolverShotContext> additionalShotRequested;

        public BulletEventContext(
            DiceRevolverGun gun,
            DiceChamber chamber,
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition,
            bool canTriggerAdditionalShots,
            Action<DiceRevolverShotContext> additionalShotRequested = null)
        {
            Gun = gun;
            Chamber = chamber;
            Shot = shot;
            HitCollider = hitCollider;
            HitPosition = hitPosition;
            CanTriggerAdditionalShots = canTriggerAdditionalShots;
            this.additionalShotRequested = additionalShotRequested;
        }

        public DiceRevolverGun Gun { get; }
        public DiceChamber Chamber { get; }
        public DiceRevolverShotContext Shot { get; }
        public Collider HitCollider { get; }
        public Vector3 HitPosition { get; }
        public bool CanTriggerAdditionalShots { get; }

        public bool RequestAdditionalShot()
        {
            if (!CanTriggerAdditionalShots || Shot == null || additionalShotRequested == null)
            {
                return false;
            }

            additionalShotRequested.Invoke(Shot);
            return true;
        }
    }
}
