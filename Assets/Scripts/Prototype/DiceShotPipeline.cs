using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceShotPipeline
    {
        private readonly Func<float> currentTime;
        private readonly Action<DiceFaceActivation, ProjectileSpawnRequest> spawnProjectile;
        private readonly Func<int, bool> refillAndForceNextFace;
        private readonly Action<string> logWarning;
        private readonly Action<Exception, UnityEngine.Object> logException;
        private readonly BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();

        public DiceShotPipeline(
            Func<float> currentTime,
            Action<DiceFaceActivation, ProjectileSpawnRequest> spawnProjectile,
            Func<int, bool> refillAndForceNextFace,
            Action<string> logWarning,
            Action<Exception, UnityEngine.Object> logException)
        {
            this.currentTime = currentTime;
            this.spawnProjectile = spawnProjectile;
            this.refillAndForceNextFace = refillAndForceNextFace;
            this.logWarning = logWarning;
            this.logException = logException;
        }

        public DiceFaceActivation ExecuteShot(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            int eventBudget,
            Action<DiceRevolverShotContext> fireStarted,
            Action<DiceRevolverShotContext> fireEnded)
        {
            DiceFaceActivation activation = null;
            activation = new DiceFaceActivation(
                face,
                configuration,
                origin,
                direction,
                (delay, callback) => scheduler.Schedule(currentTime.Invoke(), delay, callback),
                request => spawnProjectile?.Invoke(activation, request),
                refillAndForceNextFace,
                logWarning,
                eventBudget);
            DiceRevolverShotContext faceTrigger = new DiceRevolverShotContext(
                face,
                origin,
                activation.Direction,
                null,
                configuration,
                default,
                null,
                null,
                activation,
                false);
            BulletEventContext eventContext = new BulletEventContext(
                activation,
                faceTrigger,
                null,
                origin);

            fireStarted?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.Base), eventContext);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFire), eventContext);
            fireEnded?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFireEnd), eventContext);

            return activation;
        }

        public void HandleHit(
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition,
            Action<DiceRevolverHitContext> hitObserved)
        {
            DiceRevolverHitContext hit = new DiceRevolverHitContext(shot, hitCollider, hitPosition);
            hitObserved?.Invoke(hit);
            if (shot == null || !shot.CanTriggerHitEffects)
            {
                return;
            }

            TriggerEffect(
                shot.Configuration.GetEffect(DiceFaceSlotType.OnHit),
                new BulletEventContext(shot.Activation, shot, hitCollider, hitPosition));
        }

        public void Tick(float currentTime)
        {
            scheduler.Tick(
                currentTime,
                exception => logException?.Invoke(exception, null));
        }

        public void Clear()
        {
            scheduler.Clear();
        }

        private void TriggerEffect(BulletEventEffect effect, BulletEventContext context)
        {
            if (effect == null ||
                context.Activation == null ||
                !context.Activation.TryConsumeEventBudget())
            {
                return;
            }

            try
            {
                effect.Trigger(context);
            }
            catch (Exception exception)
            {
                logException?.Invoke(exception, effect);
            }
        }
    }
}
