using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/弹丸/弹丸标签")]
    public sealed class ProjectileTagConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("弹丸标签")]
        private ProjectileTagDefinition projectileTag;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (projectileTag == null)
            {
                return Failed("未配置弹丸标签。");
            }

            bool passed = context.Signal.CurrentStats.HasTag(projectileTag);
            return passed
                ? Passed("弹丸包含指定标签。")
                : Failed("弹丸不包含指定标签。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (projectileTag == null)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-projectile-tag",
                    "弹丸标签条件缺少弹丸标签。",
                    this));
            }
        }

        private static EventConditionResult Passed(string description) =>
            new EventConditionResult(true, description);

        private static EventConditionResult Failed(string reason) =>
            new EventConditionResult(false, reason, reason);
    }
}
