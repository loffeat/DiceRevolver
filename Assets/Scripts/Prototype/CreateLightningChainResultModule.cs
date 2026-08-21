using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [EventRuleModuleMenu("结果/雷电/生成闪电链")]
    public sealed class CreateLightningChainResultModule : EventResultModule
    {
        [SerializeField, InspectorName("雷电标签")]
        private ProjectileTagDefinition lightningTag;
        [SerializeField, InspectorName("闪电链定义")]
        private LightningChainDefinition chainDefinition;
        [SerializeField, Min(0f), InspectorName("共鸣搜索半径")]
        private float searchRadius = 6f;
        [SerializeField, Min(0), InspectorName("最大连接数量")]
        private int maximumConnections = 3;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (lightningTag == null)
            {
                return Skipped("未配置雷电标签。");
            }

            if (chainDefinition == null)
            {
                return Skipped("未配置闪电链定义。");
            }

            ProjectileHandle origin = context.Signal.Projectile;
            if (!origin.IsAlive)
            {
                return Skipped("缺少存活的闪电链来源弹丸。");
            }

            if (!origin.Stats.HasTag(lightningTag))
            {
                return Skipped("来源弹丸不包含雷电标签。");
            }

            if (context.Services == null)
            {
                return Skipped("缺少同归属弹丸或闪电链服务。");
            }

            IReadOnlyList<ProjectileHandle> candidates = context.Services.FindOwnedProjectiles(
                origin.Position,
                Mathf.Max(0f, searchRadius),
                lightningTag,
                origin.Projectile);
            IReadOnlyList<ProjectileHandle> targets = ElectromagneticResonanceEffect.SelectTargets(
                candidates,
                Mathf.Max(0, maximumConnections),
                count => UnityEngine.Random.Range(0, count));
            if (targets.Count == 0)
            {
                return Skipped("附近没有可连接的存活雷电弹丸。");
            }

            bool requested = context.Services.RequestLightningChain(
                origin,
                targets,
                chainDefinition);
            return requested
                ? new EventResult(EventResultStatus.Success, $"已请求连接 {targets.Count} 个雷电弹丸。")
                : Skipped("闪电链请求未被接受。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (lightningTag == null)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-lightning-tag",
                    "闪电链结果缺少雷电标签。",
                    this));
            }

            if (chainDefinition == null)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-lightning-chain-definition",
                    "闪电链结果缺少闪电链定义。",
                    this));
            }
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }
}
