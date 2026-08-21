using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    public interface IEventRuleProjectileDefinitionProvider
    {
        bool IsPrimaryProjectile { get; }
        ProjectileDefinition ProjectileDefinition { get; }
    }

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

    [EventRuleModuleMenu("结果/骰面/填充并强制骰面")]
    public sealed class ForceFaceResultModule : EventResultModule
    {
        [SerializeField, Range(1, DiceRevolverRules.FaceCount), InspectorName("指定骰面")]
        private int face = 4;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (context.Services == null)
            {
                return Skipped("缺少填充并强制骰面服务。");
            }

            bool requested = context.Services.RequestRefillAndForceNextFace(face);
            return requested
                ? new EventResult(EventResultStatus.Success, $"已请求填充并强制骰面 {face}。")
                : Skipped($"填充并强制骰面 {face} 的请求未被接受。");
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }

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

    [EventRuleModuleMenu("结果/骰面/覆盖下一骰面活动槽")]
    public sealed class QueueActiveOverlayResultModule : EventResultModule
    {
        public override EventResult Execute(EventExecutionContext context)
        {
            DiceFaceActivation activation = context.Signal.Activation;
            if (activation == null)
            {
                return Skipped("缺少来源骰面激活。");
            }

            if (context.Services == null)
            {
                return Skipped("缺少下一骰面覆盖服务。");
            }

            DiceFaceActiveOverlay overlay = DiceFaceActiveOverlay.FromSnapshot(
                activation.Configuration,
                true);
            if (overlay.IsEmpty)
            {
                return Skipped("来源骰面没有可复制的非空活动槽。");
            }

            bool requested = context.Services.QueueNextShotOverlay(overlay);
            return requested
                ? new EventResult(EventResultStatus.Success, "已请求覆盖下一骰面的非空活动槽。")
                : Skipped("下一骰面覆盖请求未被接受。");
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }

    [EventRuleModuleMenu("结果/流程/延迟结果")]
    public sealed class DelayResultModule : EventResultModule
    {
        [SerializeField, Min(0f), InspectorName("延迟（秒）")]
        private float delaySeconds;
        [SerializeField, InspectorName("延迟结果列表")]
        private List<EventResultEntry> entries = new();

        public IReadOnlyList<EventResultEntry> Entries => entries;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (entries == null || entries.Count == 0)
            {
                return Skipped("延迟结果列表为空。");
            }

            if (ContainsCycle(this))
            {
                return Skipped("延迟结果图包含循环引用，本版本不允许递归延迟。");
            }

            float delay = Mathf.Max(0f, delaySeconds);
            bool scheduled = context.ScheduleEntries(delay, entries);
            return scheduled
                ? new EventResult(EventResultStatus.Success, $"已安排 {delay:0.##} 秒后的结果列表。")
                : Skipped("延迟结果列表调度未被接受。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (entries == null || entries.Count == 0)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-delayed-results",
                    "延迟结果模块至少需要一个结果。",
                    this));
                return;
            }

            if (ContainsCycle(this))
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "delayed-result-cycle",
                    "延迟结果图包含循环引用，本版本不允许递归延迟。",
                    this));
            }

            CollectNestedValidationIssues(this, issues, new HashSet<DelayResultModule>());
        }

        private static void CollectNestedValidationIssues(
            DelayResultModule delay,
            List<EventRuleValidationIssue> issues,
            HashSet<DelayResultModule> visited)
        {
            if (delay == null || !visited.Add(delay))
            {
                return;
            }

            IReadOnlyList<EventResultEntry> nestedEntries = delay.Entries;
            if (nestedEntries == null || nestedEntries.Count == 0)
            {
                issues?.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-delayed-results",
                    "延迟结果模块至少需要一个结果。",
                    delay));
                return;
            }

            for (int index = 0; index < nestedEntries.Count; index++)
            {
                EventResultEntry entry = nestedEntries[index];
                if (entry == null || entry.Result == null)
                {
                    issues?.Add(new EventRuleValidationIssue(
                        EventRuleValidationSeverity.Error,
                        "missing-delayed-result",
                        "延迟结果列表包含空结果。",
                        delay));
                    continue;
                }

                IReadOnlyList<EventConditionModule> conditions = entry.Conditions;
                if (conditions != null)
                {
                    for (int conditionIndex = 0; conditionIndex < conditions.Count; conditionIndex++)
                    {
                        conditions[conditionIndex]?.CollectValidationIssues(issues);
                    }
                }

                if (entry.Result is DelayResultModule nestedDelay)
                {
                    CollectNestedValidationIssues(nestedDelay, issues, visited);
                }
                else
                {
                    entry.Result.CollectValidationIssues(issues);
                }
            }
        }

        private static bool ContainsCycle(DelayResultModule root)
        {
            return ContainsCycle(
                root,
                new HashSet<DelayResultModule>(),
                new HashSet<DelayResultModule>());
        }

        private static bool ContainsCycle(
            DelayResultModule delay,
            HashSet<DelayResultModule> visited,
            HashSet<DelayResultModule> activePath)
        {
            if (activePath.Contains(delay))
            {
                return true;
            }

            if (!visited.Add(delay))
            {
                return false;
            }

            activePath.Add(delay);
            IReadOnlyList<EventResultEntry> nestedEntries = delay.Entries;
            if (nestedEntries != null)
            {
                for (int index = 0; index < nestedEntries.Count; index++)
                {
                    if (nestedEntries[index]?.Result is DelayResultModule nestedDelay &&
                        ContainsCycle(nestedDelay, visited, activePath))
                    {
                        return true;
                    }
                }
            }

            activePath.Remove(delay);
            return false;
        }

        private static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);
    }
}
