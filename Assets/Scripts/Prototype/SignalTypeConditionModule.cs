using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/信号/信号类型")]
    public sealed class SignalTypeConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("信号类型")]
        private EventSignalMask signals;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            EventSignalMask current = ToMask(context.Signal.SignalType);
            return (signals & current) != 0
                ? Passed("信号类型匹配。")
                : Failed("信号类型不匹配。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (signals == EventSignalMask.None)
            {
                AddError(issues, "missing-signal-types", "信号类型条件至少需要一个信号。", this);
            }
        }

        private static EventSignalMask ToMask(EventSignalType signalType)
        {
            return signalType switch
            {
                EventSignalType.Base => EventSignalMask.Base,
                EventSignalType.OnFire => EventSignalMask.OnFire,
                EventSignalType.OnHit => EventSignalMask.OnHit,
                EventSignalType.OnFireEnd => EventSignalMask.OnFireEnd,
                EventSignalType.ProjectileSpawned => EventSignalMask.ProjectileSpawned,
                EventSignalType.ProjectileHit => EventSignalMask.ProjectileHit,
                EventSignalType.ReloadStarted => EventSignalMask.ReloadStarted,
                EventSignalType.ReloadCompleted => EventSignalMask.ReloadCompleted,
                EventSignalType.FaceConsumed => EventSignalMask.FaceConsumed,
                EventSignalType.DrawCandidate => EventSignalMask.DrawCandidate,
                EventSignalType.BeforeProjectileStats => EventSignalMask.BeforeProjectileStats,
                EventSignalType.EnemyStatusApplied => EventSignalMask.EnemyStatusApplied,
                _ => EventSignalMask.None
            };
        }
    }
}
