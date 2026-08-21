using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/被动/按计数叠加弹丸伤害")]
    public sealed class MultiplyProjectileDamageFromCounterResultModule : EventResultModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;
        [SerializeField, InspectorName("每层伤害倍率增量")]
        private float damagePerStack;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(counterKey) || damagePerStack < 0f ||
                float.IsNaN(damagePerStack) || float.IsInfinity(damagePerStack))
            {
                return Skipped("计数键为空或每层伤害倍率增量无效。");
            }

            if (context.State == null || context.Services == null)
            {
                return Skipped("缺少被动状态或伤害倍率服务。");
            }

            int stacks = Mathf.Max(0, context.State.GetInt(counterKey));
            context.Services.MultiplyProjectileDamage(1f + stacks * damagePerStack);
            return Success($"按 {stacks} 层叠加弹丸伤害。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "伤害叠层结果缺少计数键。", this);
            if (damagePerStack < 0f || float.IsNaN(damagePerStack) || float.IsInfinity(damagePerStack))
            {
                AddError(issues, "invalid-damage-per-stack", "每层伤害倍率增量必须是非负有限数值。", this);
            }
        }
    }
}
