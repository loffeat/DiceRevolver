using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
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
