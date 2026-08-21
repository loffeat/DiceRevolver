using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/弹丸/弹丸类型")]
    public sealed class ProjectileTypeConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("弹丸类型")]
        private ProjectileTypeDefinition projectileType;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (projectileType == null)
            {
                return Failed("未配置弹丸类型。");
            }

            bool passed = ReferenceEquals(
                context.Signal.CurrentStats.ProjectileTypeDefinition,
                projectileType);
            return passed
                ? Passed("弹丸类型匹配。")
                : Failed("弹丸类型不匹配。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (projectileType == null)
            {
                AddMissingReference(issues, "missing-projectile-type", "弹丸类型条件缺少弹丸类型。", this);
            }
        }

        private static EventConditionResult Passed(string description) =>
            new EventConditionResult(true, description);

        private static EventConditionResult Failed(string reason) =>
            new EventConditionResult(false, reason, reason);

        private static void AddMissingReference(
            List<EventRuleValidationIssue> issues,
            string code,
            string message,
            UnityEngine.Object context)
        {
            issues?.Add(new EventRuleValidationIssue(
                EventRuleValidationSeverity.Error, code, message, context));
        }
    }
}
