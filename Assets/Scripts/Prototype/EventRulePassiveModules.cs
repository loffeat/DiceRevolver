using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
    public enum CounterComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    [EventRuleModuleMenu("结果/被动/设置抽取优先级")]
    public sealed class SetDrawPriorityResultModule : EventResultModule
    {
        [SerializeField, InspectorName("抽取优先级")]
        private int priority;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (context.Services == null)
            {
                return Skipped("缺少抽面优先级服务。");
            }

            context.Services.SetDrawPriority(priority);
            return Success($"抽取优先级设为 {priority}。");
        }
    }

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

    [EventRuleModuleMenu("结果/状态/增加计数")]
    public sealed class IncrementCounterResultModule : EventResultModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;
        [SerializeField, Min(0), InspectorName("增加数量")]
        private int amount = 1;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(counterKey) || amount < 0 || context.State == null)
            {
                return Skipped("计数键为空、增加数量无效或缺少状态存储。");
            }

            long next = (long)context.State.GetInt(counterKey) + amount;
            context.State.SetInt(counterKey, next > int.MaxValue ? int.MaxValue : (int)next);
            return Success($"计数 {counterKey} 增加 {amount}。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "增加计数结果缺少计数键。", this);
            if (amount < 0)
            {
                AddError(issues, "invalid-counter-increment", "增加数量不能为负数。", this);
            }
        }
    }

    [EventRuleModuleMenu("结果/状态/重置计数")]
    public sealed class ResetCounterResultModule : EventResultModule
    {
        [SerializeField, InspectorName("计数键")]
        private string counterKey;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(counterKey) || context.State == null)
            {
                return Skipped("计数键为空或缺少状态存储。");
            }

            context.State.SetInt(counterKey, 0);
            return Success($"计数 {counterKey} 已重置。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-counter-key", "重置计数结果缺少计数键。", this);
        }
    }

    [EventRuleModuleMenu("结果/状态/设置布尔状态")]
    public sealed class SetBooleanStateResultModule : EventResultModule
    {
        [SerializeField, InspectorName("状态键")]
        private string stateKey;
        [SerializeField, InspectorName("状态值")]
        private bool value = true;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (!HasKey(stateKey) || context.State == null)
            {
                return Skipped("状态键为空或缺少状态存储。");
            }

            context.State.SetBool(stateKey, value);
            return Success($"布尔状态 {stateKey} 已设置。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, stateKey, "missing-boolean-state-key", "设置布尔状态结果缺少状态键。", this);
        }
    }

    [EventRuleModuleMenu("结果/被动/请求奖励骰面激活")]
    public sealed class RequestBonusActivationResultModule : EventResultModule
    {
        private const string DefaultCounterKey = "bonusActivationTriggers";

        [SerializeField, Min(1), InspectorName("每轮最大触发次数")]
        private int maximumTriggers = 1;
        [SerializeField, Min(0f), InspectorName("最大散布角度")]
        private float maximumSpreadAngle;
        [SerializeField, Min(0f), InspectorName("最小散布间隔")]
        private float minimumSpreadSeparation;
        [SerializeField, InspectorName("触发次数计数键")]
        private string counterKey = DefaultCounterKey;

        public override EventResult Execute(EventExecutionContext context)
        {
            if (maximumTriggers < 1 || maximumSpreadAngle < 0f || minimumSpreadSeparation < 0f ||
                float.IsNaN(maximumSpreadAngle) || float.IsInfinity(maximumSpreadAngle) ||
                float.IsNaN(minimumSpreadSeparation) || float.IsInfinity(minimumSpreadSeparation) ||
                minimumSpreadSeparation > maximumSpreadAngle ||
                !HasKey(counterKey) || context.State == null || context.Services == null ||
                context.Signal.EventBudget == null || context.Signal.Activation == null)
            {
                return Skipped("奖励激活参数无效，或缺少来源激活、共享预算、状态与服务。");
            }

            int used = Mathf.Max(0, context.State.GetInt(counterKey));
            if (used >= maximumTriggers)
            {
                return Skipped("奖励激活已达到本轮最大触发次数。");
            }

            int reserved = used + 1;
            context.State.SetInt(counterKey, reserved);
            bool accepted;
            try
            {
                accepted = context.Services.RequestBonusActivation(
                    context.Signal.EquippedFace,
                    maximumSpreadAngle,
                    minimumSpreadSeparation,
                    null);
            }
            catch
            {
                RollBackReservationWithoutNestedProgress(context.State, used, reserved);
                throw;
            }

            if (!accepted)
            {
                RollBackReservationWithoutNestedProgress(context.State, used, reserved);
                return Skipped("奖励骰面激活请求未被接受。");
            }

            return Success("已请求奖励骰面激活。");
        }

        private void RollBackReservationWithoutNestedProgress(
            EventRuleStateStore state,
            int previous,
            int reserved)
        {
            if (state.GetInt(counterKey) == reserved)
            {
                state.SetInt(counterKey, previous);
            }
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, counterKey, "missing-bonus-counter-key", "奖励激活结果缺少触发次数计数键。", this);
            if (maximumTriggers < 1)
            {
                AddError(issues, "invalid-bonus-trigger-limit", "每轮最大触发次数必须至少为 1。", this);
            }

            if (maximumSpreadAngle < 0f || minimumSpreadSeparation < 0f ||
                float.IsNaN(maximumSpreadAngle) || float.IsInfinity(maximumSpreadAngle) ||
                float.IsNaN(minimumSpreadSeparation) || float.IsInfinity(minimumSpreadSeparation) ||
                minimumSpreadSeparation > maximumSpreadAngle)
            {
                AddError(issues, "invalid-bonus-spread", "奖励激活散布必须是有限非负数，且最小间隔不能大于最大角度。", this);
            }
        }
    }

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

    [EventRuleModuleMenu("条件/状态/布尔状态")]
    public sealed class BooleanStateConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("状态键")]
        private string stateKey;
        [SerializeField, InspectorName("期望值")]
        private bool expectedValue = true;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            if (!HasKey(stateKey) || context.State == null)
            {
                return Failed("状态键为空或缺少状态存储。");
            }

            return context.State.GetBool(stateKey) == expectedValue
                ? Passed($"布尔状态 {stateKey} 匹配。")
                : Failed($"布尔状态 {stateKey} 不匹配。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            ValidateKey(issues, stateKey, "missing-boolean-state-key", "布尔状态条件缺少状态键。", this);
        }
    }

    [EventRuleModuleMenu("条件/骰面/来源为装备骰面")]
    public sealed class SourceFaceConditionModule : EventConditionModule
    {
        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            return context.Signal.SourceFace == context.Signal.EquippedFace
                ? Passed("来源骰面与装备骰面相同。")
                : Failed("来源骰面与装备骰面不同。");
        }
    }

    [EventRuleModuleMenu("条件/弹丸/与装备骰面主弹同类型")]
    public sealed class SameProjectileTypeConditionModule : EventConditionModule
    {
        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            ProjectileTypeDefinition equippedType = context.Signal.EquippedBaseProjectileType;
            if (equippedType == null)
            {
                return Failed("装备骰面没有可解析的基础弹丸类型。");
            }

            return context.Signal.CurrentStats.ProjectileTypeDefinition == equippedType
                ? Passed("弹丸类型与装备骰面主弹相同。")
                : Failed("弹丸类型与装备骰面主弹不同。");
        }
    }

    [EventRuleModuleMenu("条件/信号/信号类型")]
    public sealed class SignalTypeConditionModule : EventConditionModule
    {
        [SerializeField, InspectorName("信号类型")]
        private EventSignalMask signals;

        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            EventSignalMask current = ToMask(context.Signal.SignalType);
            return (signals & current) != 0
                ? Passed("信号类型匹配。")
                : Failed("信号类型不匹配。");
        }

        public override void CollectValidationIssues(List<EventRuleValidationIssue> issues)
        {
            if (signals == EventSignalMask.None)
            {
                AddError(issues, "missing-signal-types", "信号类型条件至少需要一个信号。", this);
            }
        }

        private static EventSignalMask ToMask(EventSignalType signalType)
        {
            return signalType switch
            {
                EventSignalType.Base => EventSignalMask.Base,
                EventSignalType.OnFire => EventSignalMask.OnFire,
                EventSignalType.OnHit => EventSignalMask.OnHit,
                EventSignalType.OnFireEnd => EventSignalMask.OnFireEnd,
                EventSignalType.ProjectileSpawned => EventSignalMask.ProjectileSpawned,
                EventSignalType.ProjectileHit => EventSignalMask.ProjectileHit,
                EventSignalType.ReloadStarted => EventSignalMask.ReloadStarted,
                EventSignalType.ReloadCompleted => EventSignalMask.ReloadCompleted,
                EventSignalType.FaceConsumed => EventSignalMask.FaceConsumed,
                EventSignalType.DrawCandidate => EventSignalMask.DrawCandidate,
                EventSignalType.BeforeProjectileStats => EventSignalMask.BeforeProjectileStats,
                _ => EventSignalMask.None
            };
        }
    }

    internal static class PassiveEventRuleModuleResults
    {
        public static bool HasKey(string key) => !string.IsNullOrWhiteSpace(key);

        public static EventResult Success(string description) =>
            new EventResult(EventResultStatus.Success, description);

        public static EventResult Skipped(string description) =>
            new EventResult(EventResultStatus.Skipped, description);

        public static EventConditionResult Passed(string description) =>
            new EventConditionResult(true, description);

        public static EventConditionResult Failed(string reason) =>
            new EventConditionResult(false, reason, reason);

        public static void ValidateKey(
            List<EventRuleValidationIssue> issues,
            string key,
            string code,
            string message,
            Object context)
        {
            if (!HasKey(key))
            {
                AddError(issues, code, message, context);
            }
        }

        public static void AddError(
            List<EventRuleValidationIssue> issues,
            string code,
            string message,
            Object context)
        {
            issues?.Add(new EventRuleValidationIssue(
                EventRuleValidationSeverity.Error,
                code,
                message,
                context));
        }
    }
}
