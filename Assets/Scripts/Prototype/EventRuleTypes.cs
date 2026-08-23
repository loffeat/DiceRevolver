using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [Flags]
    public enum DiceFaceSlotMask
    {
        [InspectorName("无")] None = 0,
        [InspectorName("基础事件")] Base = 1 << 0,
        [InspectorName("开火时事件")] OnFire = 1 << 1,
        [InspectorName("命中时事件")] OnHit = 1 << 2,
        [InspectorName("开火后事件")] OnFireEnd = 1 << 3,
        [InspectorName("所有事件")] All = Base | OnFire | OnHit | OnFireEnd
    }

    [Flags]
    public enum EventSignalMask
    {
        [InspectorName("无")] None = 0,
        [InspectorName("基础")] Base = 1 << 0,
        [InspectorName("开火时")] OnFire = 1 << 1,
        [InspectorName("命中时")] OnHit = 1 << 2,
        [InspectorName("开火后")] OnFireEnd = 1 << 3,
        [InspectorName("弹丸生成")] ProjectileSpawned = 1 << 4,
        [InspectorName("弹丸命中")] ProjectileHit = 1 << 5,
        [InspectorName("开始换弹")] ReloadStarted = 1 << 6,
        [InspectorName("换弹完成")] ReloadCompleted = 1 << 7,
        [InspectorName("骰面消耗")] FaceConsumed = 1 << 8,
        [InspectorName("抽面候选")] DrawCandidate = 1 << 9,
        [InspectorName("弹丸属性前")] BeforeProjectileStats = 1 << 10,
        [InspectorName("敌人状态施加")] EnemyStatusApplied = 1 << 11
    }

    public enum EventSignalType
    {
        [InspectorName("基础")] Base,
        [InspectorName("开火时")] OnFire,
        [InspectorName("命中时")] OnHit,
        [InspectorName("开火后")] OnFireEnd,
        [InspectorName("弹丸生成")] ProjectileSpawned,
        [InspectorName("弹丸命中")] ProjectileHit,
        [InspectorName("开始换弹")] ReloadStarted,
        [InspectorName("换弹完成")] ReloadCompleted,
        [InspectorName("骰面消耗")] FaceConsumed,
        [InspectorName("抽面候选")] DrawCandidate,
        [InspectorName("弹丸属性前")] BeforeProjectileStats,
        [InspectorName("敌人状态施加")] EnemyStatusApplied
    }

    public enum EventRuleRecursionPolicy
    {
        [InspectorName("禁止重入")] DenyReentry = 0,
        [InspectorName("预算内允许")] AllowWithBudget = 1,
        [InspectorName("忽略奖励激活")] IgnoreBonusActivation = 2
    }

    public readonly struct EventConditionResult
    {
        public EventConditionResult(bool passed, string description, string failureReason = null)
        {
            Passed = passed;
            Description = description;
            FailureReason = failureReason;
        }

        public bool Passed { get; }
        public string Description { get; }
        public string FailureReason { get; }
    }

    public enum EventResultStatus { Success, Skipped, Failed }

    public readonly struct EventResult
    {
        public EventResult(EventResultStatus status, string description)
        {
            Status = status;
            Description = description;
        }

        public EventResultStatus Status { get; }
        public string Description { get; }
    }

    public enum EventRuleValidationSeverity { Info, Warning, Error }

    public readonly struct EventRuleValidationIssue
    {
        public EventRuleValidationIssue(EventRuleValidationSeverity severity,
            string code, string message, UnityEngine.Object context)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Context = context;
        }

        public EventRuleValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }
}
