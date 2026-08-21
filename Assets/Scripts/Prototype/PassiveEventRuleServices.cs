using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class PassiveEventRuleServices : IEventRuleServices
    {
        private readonly EventSignal signal;
        private readonly OwnedProjectileRegistry ownedProjectiles;
        private readonly Func<BonusDiceActivationRequest, bool> bonusActivation;
        private readonly CombatDebugTrace debugTrace;
        private readonly Func<float> currentTime;
        private readonly Action<Exception, UnityEngine.Object> exceptionLogger;

        public PassiveEventRuleServices(
            EventSignal signal,
            OwnedProjectileRegistry ownedProjectiles,
            Func<BonusDiceActivationRequest, bool> bonusActivation,
            CombatDebugTrace debugTrace,
            Func<float> currentTime,
            Action<Exception, UnityEngine.Object> exceptionLogger)
        {
            this.signal = signal;
            this.ownedProjectiles = ownedProjectiles;
            this.bonusActivation = bonusActivation;
            this.debugTrace = debugTrace;
            this.currentTime = currentTime;
            this.exceptionLogger = exceptionLogger;
        }

        public DiceEventBudget EventBudget => signal.EventBudget;
        public int HighestDrawPriority { get; private set; }
        public bool DrawRejected { get; private set; }
        public float ProjectileDamageMultiplier { get; private set; } = 1f;

        public bool RequestProjectile(
            ProjectileDefinition definition,
            Vector3 origin,
            Vector3 direction,
            AttackEffectOverride attackEffectOverride,
            bool isPrimary) => false;

        public bool Schedule(float delaySeconds, Action callback) => false;

        public bool RequestBonusActivation(
            int face,
            float maximumSpreadAngle,
            float minimumSpreadSeparation,
            EventRuleDefinition sourceRule)
        {
            if (bonusActivation == null || signal.EventBudget == null || signal.Activation == null ||
                face < 1 || face > DiceRevolverRules.FaceCount ||
                float.IsNaN(maximumSpreadAngle) || float.IsInfinity(maximumSpreadAngle) ||
                float.IsNaN(minimumSpreadSeparation) || float.IsInfinity(minimumSpreadSeparation) ||
                maximumSpreadAngle < 0f || minimumSpreadSeparation < 0f ||
                minimumSpreadSeparation > maximumSpreadAngle)
            {
                return false;
            }

            return bonusActivation.Invoke(new BonusDiceActivationRequest(
                face,
                signal.EventBudget,
                0,
                Mathf.Max(0f, maximumSpreadAngle),
                Mathf.Max(0f, minimumSpreadSeparation),
                signal.Activation));
        }

        public bool RequestRefillAndForceNextFace(int face) => false;

        public bool RequestLightningChain(
            ProjectileHandle origin,
            IReadOnlyList<ProjectileHandle> targets,
            LightningChainDefinition definition) => false;

        public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay) => false;

        public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(
            Vector3 origin,
            float radius,
            ProjectileTagDefinition requiredTag,
            Projectile excludedProjectile)
        {
            if (ownedProjectiles == null)
            {
                return Array.Empty<ProjectileHandle>();
            }

            List<ProjectileHandle> results = new();
            ownedProjectiles.FindNearby(
                origin,
                Mathf.Max(0f, radius),
                requiredTag,
                excludedProjectile,
                results);
            return results;
        }

        public void SetDrawPriority(int priority)
        {
            HighestDrawPriority = Math.Max(HighestDrawPriority, priority);
        }

        public void RejectDrawCandidate(string reason)
        {
            DrawRejected = true;
        }

        public void MultiplyProjectileDamage(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
            {
                return;
            }

            float accumulated = ProjectileDamageMultiplier * multiplier;
            if (!float.IsNaN(accumulated) && !float.IsInfinity(accumulated))
            {
                ProjectileDamageMultiplier = accumulated;
            }
        }

        public void RecordRuleDebug(
            EventRuleDefinition rule,
            string stage,
            string description,
            EventResultStatus status)
        {
            if (debugTrace == null)
            {
                return;
            }

            float time = currentTime != null ? currentTime.Invoke() : 0f;
            CombatDebugScope scope = signal.DebugScope.IsValid
                ? signal.DebugScope
                : signal.Activation != null && signal.Activation.DebugScope.IsValid
                    ? signal.Activation.DebugScope
                    : debugTrace.BeginActivation(signal.EquippedFace, false, default, time);
            string ruleName = rule != null && !string.IsNullOrWhiteSpace(rule.DisplayName)
                ? rule.DisplayName
                : rule != null && !string.IsNullOrWhiteSpace(rule.name) ? rule.name : "事件规则";
            string detail = string.IsNullOrWhiteSpace(description)
                ? $"{stage}: {status}"
                : $"{stage}: {status} - {description}";
            debugTrace.Record(
                scope,
                CombatDebugEventType.Result,
                "规则被动",
                ruleName,
                detail,
                1,
                time);
        }

        public void ReportException(Exception exception, ScriptableObject module)
        {
            exceptionLogger?.Invoke(exception, module);
        }
    }
}
