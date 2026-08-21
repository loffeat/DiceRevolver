using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/状态/布尔状态")]
    public sealed class BooleanStateConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("状态键")]
        private string stateKey;
        [SerializeField, InspectorName("期望值")]
        private bool expectedValue = true;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (!HasKey(stateKey) || context.State == null)
            {
                return Failed("状态键为空或缺少状态存储。");
            }

            return context.State.GetBool(stateKey) == expectedValue
                ? Passed($"布尔状态 {stateKey} 匹配。")
                : Failed($"布尔状态 {stateKey} 不匹配。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, stateKey, "missing-boolean-state-key", "布尔状态条件缺少状态键。", this);
        }
    }
}
