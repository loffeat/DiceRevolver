using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/被动/按弹幕统计增伤")]
    public sealed class ScaleActivationDamageFromStatisticResultModule : EventResultModule
    {
        [SerializeField, InspectorName("统计弹丸定义")]
        private ProjectileDefinition statisticDefinition;
        [SerializeField, Min(0f), InspectorName("每颗伤害倍率增量")]
        private float damagePerCount;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (statisticDefinition == null ||
                context.Services == null ||
                context.Services.RoundProjectileStatistic == null ||
                context.Signal.Activation == null)
            {
                return Skipped("缺少统计弹丸定义、本轮弹丸统计或当前激活。");
            }

            int count = context.Services.RoundProjectileStatistic.Count(statisticDefinition);
            if (count <= 0)
            {
                return Skipped("本轮尚未生成匹配弹幕，不增伤。");
            }

            float multiplier = 1f + count * damagePerCount;
            context.Signal.Activation.DamageMultiplier *= multiplier;
            return Success($"本轮 {count} 颗弹幕，本次激活伤害倍率 {multiplier:0.##}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (statisticDefinition == null)
            {
                AddError(issues, "missing-statistic-definition", "按弹幕统计增伤缺少统计弹丸定义。", this);
            }
        }
    }
}
