using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public sealed class BulletEventRuleServices : IEventRuleServices
    {
        private readonly BulletEventContext context;
        private readonly Action<Exception, UnityEngine.Object> reportException;
        private readonly RoundProjectileStatistic roundProjectileStatistic;

        public BulletEventRuleServices(
            BulletEventContext context,
            Action<Exception, UnityEngine.Object> reportException,
            RoundProjectileStatistic roundProjectileStatistic = null)
        {
            this.context = context;
            this.reportException = reportException;
            this.roundProjectileStatistic = roundProjectileStatistic;
        }

        public DiceEventBudget EventBudget => context.Activation?.EventBudget;

        public RoundProjectileStatistic RoundProjectileStatistic => roundProjectileStatistic;

        public bool RequestProjectile(
            ProjectileDefinition definition,
            Vector3 origin,
            Vector3 direction,
            AttackEffectOverride attackEffectOverride,
            bool isPrimary)
        {
            return context.RequestProjectileAt(
                definition,
                origin,
                direction,
                attackEffectOverride,
                isPrimary);
        }

        public bool Schedule(float delaySeconds, Action callback)
        {
            return callback != null && context.Schedule(delaySeconds, _ => callback.Invoke());
        }

        public bool RequestBonusActivation(
            int face,
            float maximumSpreadAngle,
            float minimumSpreadSeparation,
            EventRuleDefinition sourceRule)
        {
            return false;
        }

        public bool RequestRefillAndForceNextFace(int face)
        {
            return context.RequestRefillAndForceNextFace(face);
        }

        public bool RequestLightningChain(
            ProjectileHandle origin,
            IReadOnlyList<ProjectileHandle> targets,
            LightningChainDefinition definition)
        {
            return context.RequestLightningChain(origin, targets, definition);
        }

        public bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay)
        {
            return context.QueueNextShotOverlay(overlay);
        }

        public IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(
            Vector3 origin,
            float radius,
            ProjectileTagDefinition requiredTag,
            Projectile excludedProjectile)
        {
            OwnedProjectileRegistry registry = context.Activation?.OwnedProjectiles;
            if (registry == null)
            {
                return Array.Empty<ProjectileHandle>();
            }

            List<ProjectileHandle> results = new List<ProjectileHandle>();
            registry.FindNearby(origin, radius, requiredTag, excludedProjectile, results);
            return results;
        }

        public void SetDrawPriority(int priority) { }

        public void RejectDrawCandidate(string reason) { }

        public void MultiplyProjectileDamage(float multiplier) { }

        public void RecordRuleDebug(
            EventRuleDefinition rule,
            string stage,
            string description,
            EventResultStatus status)
        {
            if (context.Activation == null)
            {
                return;
            }

            string ruleName = rule != null && !string.IsNullOrWhiteSpace(rule.DisplayName)
                ? rule.DisplayName
                : rule != null ? rule.name : "事件规则";
            string detail = string.IsNullOrWhiteSpace(description)
                ? $"{stage}: {status}"
                : $"{stage}: {status} - {description}";
            bool verbose = CombatDebugTrace.IsVerboseRuleRecord(stage, status);
            context.Activation.RecordDebug(
                MapEventType(stage),
                "规则",
                ruleName,
                detail,
                2,
                verbose);
        }

        public void ReportException(Exception exception, ScriptableObject module)
        {
            reportException?.Invoke(exception, module);
        }

        private static CombatDebugEventType MapEventType(string stage)
        {
            if (string.Equals(stage, "trigger", StringComparison.OrdinalIgnoreCase))
            {
                return CombatDebugEventType.RuleTrigger;
            }

            return stage != null &&
                   stage.IndexOf("condition", StringComparison.OrdinalIgnoreCase) >= 0
                ? CombatDebugEventType.RuleCondition
                : CombatDebugEventType.RuleResult;
        }
    }
}
