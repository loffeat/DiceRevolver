using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class DiceShotPipeline
    {
        private readonly Func<float> currentTime;
        private readonly Func<DiceFaceActivation, ProjectileSpawnRequest, ProjectileHandle> spawnProjectile;
        private readonly Func<int, bool> refillAndForceNextFace;
        private readonly Action<string> logWarning;
        private readonly Action<Exception, UnityEngine.Object> logException;
        private readonly BulletEventTimeScheduler scheduler = new BulletEventTimeScheduler();
        private OwnedProjectileRegistry ownedProjectiles;
        private Func<ProjectileHandle, IReadOnlyList<ProjectileHandle>, LightningChainDefinition, bool>
            requestLightningChain;

        public DiceShotPipeline(
            Func<float> currentTime,
            Action<DiceFaceActivation, ProjectileSpawnRequest> spawnProjectile,
            Func<int, bool> refillAndForceNextFace,
            Action<string> logWarning,
            Action<Exception, UnityEngine.Object> logException)
            : this(
                currentTime,
                spawnProjectile == null
                    ? null
                    : (activation, request) =>
                    {
                        spawnProjectile.Invoke(activation, request);
                        return default;
                    },
                refillAndForceNextFace,
                logWarning,
                logException)
        {
        }

        public DiceShotPipeline(
            Func<float> currentTime,
            Func<DiceFaceActivation, ProjectileSpawnRequest, ProjectileHandle> spawnProjectile,
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
            return ExecuteActivation(
                face,
                configuration,
                origin,
                direction,
                new DiceEventBudget(eventBudget),
                false,
                0,
                fireStarted,
                fireEnded);
        }

        public DiceFaceActivation ExecuteBonusShot(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            DiceEventBudget sharedEventBudget,
            long suppressedPassiveInstanceId,
            Action<DiceRevolverShotContext> fireStarted,
            Action<DiceRevolverShotContext> fireEnded)
        {
            return ExecuteActivation(
                face,
                configuration,
                origin,
                direction,
                sharedEventBudget,
                true,
                suppressedPassiveInstanceId,
                fireStarted,
                fireEnded);
        }

        private DiceFaceActivation ExecuteActivation(
            int face,
            DiceFaceConfigurationSnapshot configuration,
            Vector3 origin,
            Vector3 direction,
            DiceEventBudget eventBudget,
            bool isBonusActivation,
            long suppressedPassiveInstanceId,
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
                request => spawnProjectile != null
                    ? spawnProjectile.Invoke(activation, request)
                    : default,
                refillAndForceNextFace,
                logWarning,
                eventBudget,
                isBonusActivation,
                suppressedPassiveInstanceId);
            activation.ConfigureLightningServices(ownedProjectiles, requestLightningChain);
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
            TickScheduledEvents(currentTime.Invoke());
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFire), eventContext);
            fireEnded?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEffect(DiceFaceSlotType.OnFireEnd), eventContext);

            return activation;
        }

        public void ConfigureLightningServices(
            OwnedProjectileRegistry registry,
            Func<ProjectileHandle, IReadOnlyList<ProjectileHandle>, LightningChainDefinition, bool>
                chainRequest)
        {
            ownedProjectiles = registry;
            requestLightningChain = chainRequest;
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
            TickScheduledEvents(currentTime);
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

        private void TickScheduledEvents(float time)
        {
            scheduler.Tick(
                time,
                exception => logException?.Invoke(exception, null));
        }
    }
}
