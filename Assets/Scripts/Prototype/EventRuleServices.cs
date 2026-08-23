using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public interface IEventRuleServices
    {
        DiceEventBudget EventBudget { get; }
        bool RequestProjectile(ProjectileDefinition definition, Vector3 origin, Vector3 direction,
            AttackEffectOverride attackEffectOverride, bool isPrimary);
        bool Schedule(float delaySeconds, Action callback);
        bool RequestBonusActivation(int face, float maximumSpreadAngle,
            float minimumSpreadSeparation, EventRuleDefinition sourceRule);
        bool RequestRefillAndForceNextFace(int face);
        bool RequestLightningChain(ProjectileHandle origin,
            IReadOnlyList<ProjectileHandle> targets, LightningChainDefinition definition);
        bool QueueNextShotOverlay(DiceFaceActiveOverlay overlay);
        IReadOnlyList<ProjectileHandle> FindOwnedProjectiles(Vector3 origin, float radius,
            ProjectileTagDefinition requiredTag, Projectile excludedProjectile);
        void SetDrawPriority(int priority);
        void RejectDrawCandidate(string reason);
        void MultiplyProjectileDamage(float multiplier);
        RoundProjectileStatistic RoundProjectileStatistic { get; }
        void RecordRuleDebug(EventRuleDefinition rule, string stage,
            string description, EventResultStatus status);
        void ReportException(Exception exception, ScriptableObject module);
    }

    public readonly struct EventEvaluationContext
    {
        public EventEvaluationContext(
            EventSignal signal,
            EventRuleStateStore state,
            IEventRuleServices services)
        {
            Signal = signal;
            State = state;
            Services = services;
        }

        public EventSignal Signal { get; }
        public EventRuleStateStore State { get; }
        public IEventRuleServices Services { get; }
    }

    public readonly struct EventExecutionContext
    {
        private readonly Func<float, IReadOnlyList<EventResultEntry>, bool> scheduleEntries;

        public EventExecutionContext(
            EventSignal signal,
            EventRuleStateStore state,
            IEventRuleServices services)
            : this(signal, state, services, null, null)
        {
        }

        internal EventExecutionContext(
            EventSignal signal,
            EventRuleStateStore state,
            IEventRuleServices services,
            Func<float, IReadOnlyList<EventResultEntry>, bool> scheduleEntries,
            EventRuleDefinition sourceRule = null)
        {
            Signal = signal;
            State = state;
            Services = services;
            SourceRule = sourceRule;
            this.scheduleEntries = scheduleEntries;
        }

        public EventSignal Signal { get; }
        public EventRuleStateStore State { get; }
        public IEventRuleServices Services { get; }
        public EventRuleDefinition SourceRule { get; }

        public bool ScheduleEntries(float delaySeconds, IReadOnlyList<EventResultEntry> entries)
        {
            return scheduleEntries != null && scheduleEntries.Invoke(delaySeconds, entries);
        }
    }
}
