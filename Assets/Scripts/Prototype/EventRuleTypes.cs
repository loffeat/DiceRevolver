using System;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [Flags]
    public enum DiceFaceSlotMask
    {
        None = 0,
        Base = 1 << 0,
        OnFire = 1 << 1,
        OnHit = 1 << 2,
        OnFireEnd = 1 << 3,
        Passive = 1 << 4,
        Active = Base | OnFire | OnHit | OnFireEnd,
        All = Active | Passive
    }

    [Flags]
    public enum EventSignalMask
    {
        None = 0,
        Base = 1 << 0,
        OnFire = 1 << 1,
        OnHit = 1 << 2,
        OnFireEnd = 1 << 3,
        ProjectileSpawned = 1 << 4,
        ProjectileHit = 1 << 5,
        ReloadStarted = 1 << 6,
        ReloadCompleted = 1 << 7,
        FaceConsumed = 1 << 8,
        DrawCandidate = 1 << 9,
        BeforeProjectileStats = 1 << 10
    }

    public enum EventSignalType
    {
        Base,
        OnFire,
        OnHit,
        OnFireEnd,
        ProjectileSpawned,
        ProjectileHit,
        ReloadStarted,
        ReloadCompleted,
        FaceConsumed,
        DrawCandidate,
        BeforeProjectileStats
    }

    public enum EventRuleRecursionPolicy
    {
        DenyReentry = 0,
        AllowWithBudget = 1,
        IgnoreBonusActivation = 2
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
