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

    [EventRuleModuleMenu("条件/弹丸/攻击特效")]
    public sealed class AttackEffectConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("期望触发攻击特效")]
        private bool expectedCanTriggerHitEffects = true;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            DiceRevolverShotContext shot = context.Signal.Shot;
            if (shot == null)
            {
                return new EventConditionResult(false, "缺少射击上下文。", "缺少射击上下文。");
            }

            bool passed = shot.CanTriggerHitEffects == expectedCanTriggerHitEffects;
            return passed
                ? new EventConditionResult(true, "攻击特效判定匹配。")
                : new EventConditionResult(false, "攻击特效判定不匹配。", "攻击特效判定不匹配。");
        }
    }

    [EventRuleModuleMenu("条件/骰面/指定骰面可检索")]
    public sealed class FaceAvailableConditionModule : EventConditionModule
    {
        [SerializeField, Range(1, DiceRevolverRules.FaceCount), InspectorName("指定骰面")]
        private int face = 4;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            IReadOnlyList<int> remainingFaces = context.Signal.RemainingFaces;
            if (remainingFaces != null)
            {
                for (int index = 0; index < remainingFaces.Count; index++)
                {
                    if (remainingFaces[index] == face)
                    {
                        return new EventConditionResult(
                            false,
                            $"骰面 {face} 仍在剩余骰面中。",
                            $"骰面 {face} 仍在剩余骰面中。");
                    }
                }
            }

            return new EventConditionResult(true, $"骰面 {face} 可通过填充进行检索。");
        }
    }

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
