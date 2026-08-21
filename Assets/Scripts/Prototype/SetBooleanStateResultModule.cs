using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/状态/设置布尔状态")]
    public sealed class SetBooleanStateResultModule : EventResultModule
    {
        [SerializeField, InspectorName("状态键")]
        private string stateKey;
        [SerializeField, InspectorName("状态值")]
        private bool value = true;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(stateKey) || context.State == null)
            {
                return Skipped("状态键为空或缺少状态存储。");
            }

            context.State.SetBool(stateKey, value);
            return Success($"布尔状态 {stateKey} 已设置。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, stateKey, "missing-boolean-state-key", "设置布尔状态结果缺少状态键。", this);
        }
    }
}
