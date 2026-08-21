using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("条件/弹丸/附近同归属弹丸数量")]
    public sealed class OwnedProjectileCountConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("弹丸标签")]
        private ProjectileTagDefinition projectileTag;
        [SerializeField, Min(0f), InspectorName("搜索半径")]
        private float searchRadius = 6f;
        [SerializeField, Min(0), InspectorName("至少数量")]
        private int atLeast = 1;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (projectileTag == null)
            {
                return Failed("未配置附近弹丸标签。");
            }

            if (!context.Signal.Projectile.IsAlive)
            {
                return Failed("缺少存活的来源弹丸。");
            }

            if (context.Services == null)
            {
                return Failed("缺少同归属弹丸查询服务。");
            }

            IReadOnlyList<ProjectileHandle> projectiles = context.Services.FindOwnedProjectiles(
                context.Signal.Projectile.Position,
                Mathf.Max(0f, searchRadius),
                projectileTag,
                context.Signal.Projectile.Projectile);
            int count = projectiles?.Count ?? 0;
            int required = Mathf.Max(0, atLeast);
            return count >= required
                ? new EventConditionResult(true, $"附近同归属弹丸数量 {count}，满足至少 {required}。")
                : Failed($"附近同归属弹丸数量 {count}，不足 {required}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (projectileTag == null)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-owned-projectile-tag",
                    "附近弹丸数量条件缺少弹丸标签。",
                    this));
            }
        }

        private static EventConditionResult Failed(string reason) =>
            new EventConditionResult(false, reason, reason);
    }
}
