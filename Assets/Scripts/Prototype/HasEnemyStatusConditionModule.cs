using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/敌人状态/目标处于状态")]
    public sealed class HasEnemyStatusConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("状态定义")] private EnemyStatusDefinition statusDefinition;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (statusDefinition == null)
            {
                return Failed("缺少状态定义。");
            }

            EnemyStatusHost host = ApplyEnemyStatusResultModule.ResolveHost(context.Signal);
            return host != null && host.HasStatus(statusDefinition.StatusId)
                ? Passed($"目标处于状态：{statusDefinition.DisplayName}。")
                : Failed($"目标未处于状态：{statusDefinition.DisplayName}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (statusDefinition == null)
            {
                AddError(issues, "missing-status-definition", "目标状态条件缺少状态定义。", this);
            }
        }
    }
}
