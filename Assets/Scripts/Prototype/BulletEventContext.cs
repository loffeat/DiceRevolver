using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public readonly struct BulletEventContext
    {
        public BulletEventContext(
            DiceFaceActivation activation,
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition)
        {
            Activation = activation;
            Shot = shot;
            HitCollider = hitCollider;
            HitPosition = hitPosition;
        }

        public DiceFaceActivation Activation { get; }
        public DiceRevolverShotContext Shot { get; }
        public Collider HitCollider { get; }
        public Vector3 HitPosition { get; }

        public bool RequestProjectile(
            ProjectileDefinition definition,
            AttackEffectOverride attackEffectOverride,
            bool isPrimary)
        {
            if (Activation == null)
            {
                return false;
            }

            Vector3 origin = Shot != null ? Shot.Origin : Activation.Origin;
            Vector3 direction = Shot != null ? Shot.Direction : Activation.Direction;
            return Activation.RequestProjectile(
                definition,
                attackEffectOverride,
                isPrimary,
                origin,
                direction);
        }

        public bool RequestProjectileAt(
            ProjectileDefinition definition,
            Vector3 origin,
            Vector3 direction,
            AttackEffectOverride attackEffectOverride,
            bool isPrimary = false)
        {
            return Activation != null && Activation.RequestProjectile(
                definition,
                attackEffectOverride,
                isPrimary,
                origin,
                direction);
        }

        public bool Schedule(float delaySeconds, Action<BulletEventContext> callback)
        {
            if (Activation == null || callback == null)
            {
                return false;
            }

            BulletEventContext scheduledContext = this;
            return Activation.Schedule(
                delaySeconds,
                () => callback.Invoke(scheduledContext));
        }

        public bool RequestRefillAndForceNextFace(int face)
        {
            return Activation != null && Activation.RequestRefillAndForceNextFace(face);
        }
    }
}
