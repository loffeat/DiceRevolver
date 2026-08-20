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
        private DiceFaceActiveOverlay pendingNormalShotOverlay;
        private bool hasPendingNormalShotOverlay;
        private CombatDebugTrace debugTrace;

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
                null,
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
            return ExecuteBonusShot(
                face,
                configuration,
                origin,
                direction,
                sharedEventBudget,
                suppressedPassiveInstanceId,
                null,
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
            DiceFaceActivation sourceActivation,
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
                sourceActivation,
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
            DiceFaceActivation sourceActivation,
            Action<DiceRevolverShotContext> fireStarted,
            Action<DiceRevolverShotContext> fireEnded)
        {
            if (!isBonusActivation && hasPendingNormalShotOverlay)
            {
                configuration = configuration.MergeActiveOverlay(pendingNormalShotOverlay);
                pendingNormalShotOverlay = default;
                hasPendingNormalShotOverlay = false;
            }

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
            activation.ConfigureOverlayService(QueueNextShotOverlay);
            CombatDebugScope debugScope = debugTrace != null
                ? debugTrace.BeginActivation(
                    face,
                    isBonusActivation,
                    sourceActivation != null ? sourceActivation.DebugScope : default,
                    currentTime.Invoke())
                : default;
            activation.ConfigureDebugScope(debugTrace, debugScope, currentTime);
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

            Record(
                activation,
                isBonusActivation
                    ? CombatDebugEventType.BonusShotStarted
                    : CombatDebugEventType.ShotStarted,
                "射击",
                isBonusActivation ? "奖励射击开始" : "开始射击",
                null,
                0);
            fireStarted?.Invoke(faceTrigger);
            TriggerEffect(configuration.GetEntry(DiceFaceSlotType.Base), DiceFaceSlotType.Base, eventContext);
            TickScheduledEvents(currentTime.Invoke());
            TriggerEffect(configuration.GetEntry(DiceFaceSlotType.OnFire), DiceFaceSlotType.OnFire, eventContext);
            Record(activation, CombatDebugEventType.ShotEnded, "射击", "结束开火", null, 0);
            fireEnded?.Invoke(faceTrigger);
            TriggerEffect(
                configuration.GetEntry(DiceFaceSlotType.OnFireEnd),
                DiceFaceSlotType.OnFireEnd,
                eventContext);

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

        public void ConfigureDebugTrace(CombatDebugTrace trace)
        {
            debugTrace = trace;
        }

        public void QueueNextShotOverlay(DiceFaceActiveOverlay overlay)
        {
            if (overlay.IsEmpty)
            {
                return;
            }

            pendingNormalShotOverlay = hasPendingNormalShotOverlay
                ? pendingNormalShotOverlay.Merge(overlay)
                : overlay;
            hasPendingNormalShotOverlay = true;
        }

        public void HandleHit(
            DiceRevolverShotContext shot,
            Collider hitCollider,
            Vector3 hitPosition,
            Action<DiceRevolverHitContext> hitObserved)
        {
            DiceRevolverHitContext hit = new DiceRevolverHitContext(shot, hitCollider, hitPosition);
            hitObserved?.Invoke(hit);
            if (shot?.Activation != null)
            {
                Record(
                    shot.Activation,
                    CombatDebugEventType.Hit,
                    "命中",
                    "弹丸命中",
                    hitCollider != null ? hitCollider.name : null,
                    1);
            }
            if (shot == null || !shot.CanTriggerHitEffects)
            {
                return;
            }

            TriggerEffect(
                shot.Configuration.GetEntry(DiceFaceSlotType.OnHit),
                DiceFaceSlotType.OnHit,
                new BulletEventContext(shot.Activation, shot, hitCollider, hitPosition));
        }

        public void Tick(float currentTime)
        {
            TickScheduledEvents(currentTime);
        }

        public void Clear()
        {
            ClearForReload();
        }

        public void ClearForReload()
        {
            scheduler.Clear();
            pendingNormalShotOverlay = default;
            hasPendingNormalShotOverlay = false;
        }

        private void TriggerEffect(
            DiceFaceEntry entry,
            DiceFaceSlotType slotType,
            BulletEventContext context)
        {
            BulletEventEffect effect = entry != null
                ? entry.Effect
                : context.Activation?.Configuration.GetEffect(slotType);
            if (effect == null ||
                context.Activation == null ||
                !context.Activation.TryConsumeEventBudget())
            {
                return;
            }

            try
            {
                Record(
                    context.Activation,
                    CombatDebugEventType.EffectTriggered,
                    slotType.ToChineseLabel(),
                    entry == null || string.IsNullOrWhiteSpace(entry.DisplayName)
                        ? effect.name
                        : entry.DisplayName,
                    null,
                    1);
                effect.Trigger(context);
            }
            catch (Exception exception)
            {
                logException?.Invoke(exception, effect);
            }
        }

        private void Record(
            DiceFaceActivation activation,
            CombatDebugEventType eventType,
            string phase,
            string name,
            string detail,
            int additionalDepth)
        {
            if (debugTrace == null || activation == null || !activation.DebugScope.IsValid)
            {
                return;
            }

            debugTrace.Record(
                activation.DebugScope,
                eventType,
                phase,
                name,
                detail,
                additionalDepth,
                currentTime.Invoke());
        }

        private void TickScheduledEvents(float time)
        {
            scheduler.Tick(
                time,
                exception => logException?.Invoke(exception, null));
        }
    }
}
