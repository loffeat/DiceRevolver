using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/弹丸/生成弹丸")]
    public sealed class SpawnProjectileResultModule : EventResultModule,
        IEventRuleProjectileDefinitionProvider
    {
        [SerializeField, InspectorName("弹丸定义")]
        private ProjectileDefinition projectileDefinition;
        [SerializeField, InspectorName("使用当前主弹定义")]
        private bool useCurrentPrimaryDefinition;
        [SerializeField, InspectorName("使用命中位置")]
        private bool useHitOrigin;
        [SerializeField, Min(0f), InspectorName("生成延迟（秒）")]
        private float delaySeconds;
        [SerializeField, InspectorName("攻击特效判定")]
        private AttackEffectOverride attackEffectOverride = AttackEffectOverride.UseProjectileDefault;
        [SerializeField, InspectorName("视为主弹")]
        private bool primaryProjectile = true;

        public bool IsPrimaryProjectile => primaryProjectile;
        public ProjectileDefinition ProjectileDefinition => projectileDefinition;

        public override EventResult Execute(EventExecutionContext context)
        {
            IEventRuleServices services = context.Services;
            if (services == null)
            {
                return Skipped("缺少弹丸生成服务。");
            }

            ProjectileDefinition definition = ResolveDefinition(context.Signal);
            if (definition == null)
            {
                return Skipped("未配置可用的弹丸定义。");
            }

            Vector3 origin = ResolveOrigin(context.Signal);
            Vector3 direction = ResolveDirection(context.Signal);
            float delay = Mathf.Max(0f, delaySeconds);
            if (delay > 0f)
            {
                bool scheduled = services.Schedule(delay, () => services.RequestProjectile(
                    definition,
                    origin,
                    direction,
                    attackEffectOverride,
                    primaryProjectile));
                return scheduled
                    ? Succeeded($"已安排 {delay:0.##} 秒后生成弹丸。")
                    : Skipped("弹丸延迟调度未被接受。");
            }

            bool requested = services.RequestProjectile(
                definition,
                origin,
                direction,
                attackEffectOverride,
                primaryProjectile);
            return requested
                ? Succeeded("已请求生成弹丸。")
                : Skipped("弹丸生成请求未被接受。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (projectileDefinition == null && !useCurrentPrimaryDefinition)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-projectile-definition",
                    "生成弹丸结果缺少弹丸定义。",
                    this));
            }
        }

        private ProjectileDefinition ResolveDefinition(EventSignal signal)
        {
            if (projectileDefinition != null)
            {
                return projectileDefinition;
            }

            if (!useCurrentPrimaryDefinition)
            {
                return null;
            }

            return signal.Activation?.PrimaryProjectileDefinition ?? signal.Shot?.ProjectileDefinition;
        }

        private Vector3 ResolveOrigin(EventSignal signal)
        {
            if (useHitOrigin)
            {
                return signal.HitPosition;
            }

            if (signal.Shot != null)
            {
                return signal.Shot.Origin;
            }

            if (signal.Activation != null)
            {
                return signal.Activation.Origin;
            }

            return signal.Projectile.IsAlive ? signal.Projectile.Position : Vector3.zero;
        }

        private static Vector3 ResolveDirection(EventSignal signal)
        {
            if (signal.Shot != null)
            {
                return signal.Shot.Direction;
            }

            return signal.Activation != null ? signal.Activation.Direction : Vector3.forward;
        }

        private static EventResult Succeeded(string description) =>
            new EventResult(EventResultStatus.Success, description);

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }
}
