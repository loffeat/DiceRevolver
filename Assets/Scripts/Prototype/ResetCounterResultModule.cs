using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/状态/重置计数")]
    public sealed class ResetCounterResultModule : EventResultModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(counterKey) || context.State == null)
            {
                return Skipped("计数键为空或缺少状态存储。");
            }

            context.State.SetInt(counterKey, 0);
            return Success($"计数 {counterKey} 已重置。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "重置计数结果缺少计数键。", this);
        }
    }
}
