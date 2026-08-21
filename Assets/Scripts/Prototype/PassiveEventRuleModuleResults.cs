using System.Collections.Generic;
using UnityEngine;
using static DiceRevolver.Prototype.PassiveEventRuleModuleResults;

namespace DiceRevolver.Prototype
{
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
