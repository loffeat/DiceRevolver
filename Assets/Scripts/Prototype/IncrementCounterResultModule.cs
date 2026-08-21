using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/状态/增加计数")]
    public sealed class IncrementCounterResultModule : EventResultModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;
        [SerializeField, Min(0), InspectorName("增加数量")]
        private int amount = 1;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(counterKey) || amount < 0 || context.State == null)
            {
                return Skipped("计数键为空、增加数量无效或缺少状态存储。");
            }

            long next = (long)context.State.GetInt(counterKey) + amount;
            context.State.SetInt(counterKey, next > int.MaxValue ? int.MaxValue : (int)next);
            return Success($"计数 {counterKey} 增加 {amount}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "增加计数结果缺少计数键。", this);
            if (amount < 0)
            {
                AddError(issues, "invalid-counter-increment", "增加数量不能为负数。", this);
            }
        }
    }
}
