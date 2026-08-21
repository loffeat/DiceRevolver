using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Editor
{
    public readonly struct EventRuleValidationEnvironment
    {
        public EventRuleValidationEnvironment(
            bool optionalServicesAvailable,
            bool passiveStateSupported)
        {
            OptionalServicesAvailable = optionalServicesAvailable;
            PassiveStateSupported = passiveStateSupported;
        }

        public bool OptionalServicesAvailable { get; }
        public bool PassiveStateSupported { get; }

        public static EventRuleValidationEnvironment Default =>
            new EventRuleValidationEnvironment(true, true);
    }

    public static class EventRuleValidator
    {
        public const string RuleTriggerMissing = "RULE_TRIGGER_MISSING";
        public const string RuleResultsEmpty = "RULE_RESULTS_EMPTY";
        public const string RuleSlotConflict = "RULE_SLOT_CONFLICT";
        public const string ModuleReferenceMissing = "MODULE_REFERENCE_MISSING";
        public const string ModuleForeignSubAsset = "MODULE_FOREIGN_SUBASSET";
        public const string RuleRecursionRisk = "RULE_RECURSION_RISK";
        public const string PassiveStateUnsupported = "PASSIVE_STATE_UNSUPPORTED";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";

        public static IReadOnlyList<EventRuleValidationIssue> Validate(
            EventRuleDefinition rule,
            DiceFaceSlotType slot)
        {
            return Validate(rule, slot, EventRuleValidationEnvironment.Default);
        }

        public static IReadOnlyList<EventRuleValidationIssue> Validate(
            EventRuleDefinition rule,
            DiceFaceSlotType slot,
            EventRuleValidationEnvironment environment)
        {
            List<EventRuleValidationIssue> issues = new List<EventRuleValidationIssue>();
            if (rule == null)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    ModuleReferenceMissing,
                    "规则引用为空。",
                    null));
                return issues.AsReadOnly();
            }

            List<EventRuleValidationIssue> runtimeIssues = rule.CollectValidationIssues(slot);
            for (int index = 0; index < runtimeIssues.Count; index++)
            {
                issues.Add(MapRuntimeIssue(runtimeIssues[index]));
            }

            CollectOwnershipIssues(rule, issues);

            if (rule.RecursionPolicy == EventRuleRecursionPolicy.AllowWithBudget)
            {
                AddIfMissing(
                    issues,
                    EventRuleValidationSeverity.Warning,
                    RuleRecursionRisk,
                    "该规则允许在预算内重入，延迟或奖励激活仍可能形成递归链。",
                    rule);
            }

            if (slot == DiceFaceSlotType.Passive && !environment.PassiveStateSupported)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    PassiveStateUnsupported,
                    "当前宿主不支持被动规则的持久状态。",
                    rule));
            }

            if (!environment.OptionalServicesAvailable)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Warning,
                    ServiceUnavailable,
                    "当前预览环境缺少规则可能需要的可选运行时服务。",
                    rule));
            }

            if (!ContainsSeverity(issues, EventRuleValidationSeverity.Error) &&
                rule.RecursionPolicy == EventRuleRecursionPolicy.DenyReentry)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Info,
                    RuleRecursionRisk,
                    "该规则禁止重入，并保持与旧事件兼容边界一致。",
                    rule));
            }

            return issues.AsReadOnly();
        }

        private static EventRuleValidationIssue MapRuntimeIssue(EventRuleValidationIssue issue)
        {
            string code;
            EventRuleValidationSeverity severity = issue.Severity;
            switch (issue.Code)
            {
                case "missing-trigger":
                    code = RuleTriggerMissing;
                    break;
                case "missing-results":
                    code = RuleResultsEmpty;
                    break;
                case "slot-not-allowed":
                    code = RuleSlotConflict;
                    break;
                case "delayed-result-cycle":
                    code = RuleRecursionRisk;
                    severity = EventRuleValidationSeverity.Warning;
                    break;
                default:
                    code = ModuleReferenceMissing;
                    break;
            }

            return new EventRuleValidationIssue(
                severity,
                code,
                issue.Message,
                issue.Context);
        }

        private static void CollectOwnershipIssues(
            EventRuleDefinition rule,
            List<EventRuleValidationIssue> issues)
        {
            string rulePath = AssetDatabase.GetAssetPath(rule);
            HashSet<Object> visited = new HashSet<Object>();
            Queue<Object> pending = new Queue<Object>();
            pending.Enqueue(rule);
            visited.Add(rule);

            while (pending.Count > 0)
            {
                Object current = pending.Dequeue();
                SerializedObject serialized = new SerializedObject(current);
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = true;
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    Object module = property.objectReferenceValue;
                    if (!IsModule(module))
                    {
                        continue;
                    }

                    string modulePath = AssetDatabase.GetAssetPath(module);
                    if (string.IsNullOrEmpty(modulePath))
                    {
                        issues.Add(new EventRuleValidationIssue(
                            EventRuleValidationSeverity.Error,
                            ModuleReferenceMissing,
                            $"模块 {module.name} 尚未保存为规则 SubAsset。",
                            module));
                    }
                    else if (string.IsNullOrEmpty(rulePath) ||
                             !string.Equals(modulePath, rulePath, StringComparison.Ordinal))
                    {
                        issues.Add(new EventRuleValidationIssue(
                            EventRuleValidationSeverity.Error,
                            ModuleForeignSubAsset,
                            $"模块 {module.name} 属于其他规则资产。",
                            module));
                    }

                    if (visited.Add(module))
                    {
                        pending.Enqueue(module);
                    }
                }
            }
        }

        private static bool IsModule(Object value)
        {
            return value is EventTriggerModule ||
                   value is EventConditionModule ||
                   value is EventResultModule;
        }

        private static bool ContainsSeverity(
            List<EventRuleValidationIssue> issues,
            EventRuleValidationSeverity severity)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddIfMissing(
            List<EventRuleValidationIssue> issues,
            EventRuleValidationSeverity severity,
            string code,
            string message,
            Object context)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Code == code)
                {
                    return;
                }
            }

            issues.Add(new EventRuleValidationIssue(severity, code, message, context));
        }
    }
}
