using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/状态/计数比较")]
    public sealed class CounterComparisonConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;
        [SerializeField, InspectorName("比较方式")]
        private CounterComparisonOperator comparison;
        [SerializeField, InspectorName("比较值")]
        private int value;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (!HasKey(counterKey) || context.State == null)
            {
                return Failed("计数键为空或缺少状态存储。");
            }

            int current = context.State.GetInt(counterKey);
            bool passed = comparison switch
            {
                CounterComparisonOperator.Equal => current == value,
                CounterComparisonOperator.NotEqual => current != value,
                CounterComparisonOperator.LessThan => current < value,
                CounterComparisonOperator.LessThanOrEqual => current <= value,
                CounterComparisonOperator.GreaterThan => current > value,
                CounterComparisonOperator.GreaterThanOrEqual => current >= value,
                _ => false
            };
            return passed ? Passed($"计数 {counterKey} 满足比较条件。") : Failed($"计数 {counterKey} 不满足比较条件。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "计数比较条件缺少计数键。", this);
            if (!System.Enum.IsDefined(typeof(CounterComparisonOperator), comparison))
            {
                AddError(issues, "invalid-counter-comparison", "计数比较方式无效。", this);
            }
        }
    }
}
