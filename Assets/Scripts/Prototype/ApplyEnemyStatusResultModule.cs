using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/敌人状态/施加状态")]
    public sealed class ApplyEnemyStatusResultModule : EventResultModule
    {
        [SerializeField, InspectorName("状态定义")] private EnemyStatusDefinition statusDefinition;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (statusDefinition == null)
            {
                return Skipped("缺少状态定义。");
            }

            EnemyStatusHost host = ResolveHost(context.Signal);
            if (host == null)
            {
                return Skipped("命中目标没有状态容器（EnemyStatusHost）。");
            }

            host.ApplyStatus(statusDefinition, context.Signal.Activation);
            return Success($"已施加状态：{statusDefinition.DisplayName}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (statusDefinition == null)
            {
                AddError(issues, "missing-status-definition", "施加状态结果缺少状态定义。", this);
            }
        }

        internal static EnemyStatusHost ResolveHost(EventSignal signal)
        {
            if (signal.HitCollider != null)
            {
                EnemyStatusHost host = signal.HitCollider.GetComponentInParent<EnemyStatusHost>();
                if (host != null)
                {
                    return host;
                }
            }

            return signal.StatusTarget;
        }
    }
}
