using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRevolver.Prototype
{
    [Serializable]
    public sealed class EventResultEntry
    {
        [SerializeField, InspectorName("局部条件")] private List<EventConditionModule> conditions = new();
        [SerializeField, InspectorName("结果模块")] private EventResultModule result;

        public EventResultEntry(IReadOnlyList<EventConditionModule> conditions, EventResultModule result)
        {
            this.conditions = conditions == null
                ? new List<EventConditionModule>()
                : new List<EventConditionModule>(conditions);
            this.result = result;
        }

        public IReadOnlyList<EventConditionModule> Conditions => conditions;
        public EventResultModule Result => result;
    }

    public sealed class EventRuleDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("显示名称")] private string displayName;
        [SerializeField, TextArea, InspectorName("描述")] private string description;
        [SerializeField, InspectorName("显示颜色")] private Color displayColor = Color.white;
        [SerializeField, InspectorName("标签")] private List<string> tags = new();
        [SerializeField, InspectorName("稀有度")] private string rarity;
        [SerializeField, InspectorName("事件类型")] private DiceFaceSlotMask allowedSlots = DiceFaceSlotMask.All;
        [SerializeField, InspectorName("触发器")] private EventTriggerModule trigger;
        [SerializeField, InspectorName("规则条件")] private List<EventConditionModule> conditions = new();
        [SerializeField, InspectorName("结果列表")] private List<EventResultEntry> results = new();
        [SerializeField, InspectorName("事件预算消耗")] private int eventBudgetCost = 1;
        [SerializeField, InspectorName("递归策略")] private EventRuleRecursionPolicy recursionPolicy = EventRuleRecursionPolicy.DenyReentry;

        public string DisplayName => displayName;
        public string Description => description;
        public Color DisplayColor => displayColor;
        public IReadOnlyList<string> Tags => tags;
        public string Rarity => rarity;
        public DiceFaceSlotMask AllowedSlots => allowedSlots;
        public EventTriggerModule Trigger => trigger;
        public IReadOnlyList<EventConditionModule> Conditions => conditions;
        public IReadOnlyList<EventResultEntry> Results => results;
        public int EventBudgetCost => eventBudgetCost;
        public EventRuleRecursionPolicy RecursionPolicy => recursionPolicy;

        public bool AllowsSlot(DiceFaceSlotType slot)
        {
            return (allowedSlots & ToMask(slot)) != 0;
        }

        public bool CanEquip(DiceFaceSlotType slot)
        {
            List<EventRuleValidationIssue> issues = CollectValidationIssues(slot);
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == EventRuleValidationSeverity.Error)
                {
                    return false;
                }
            }

            return true;
        }

        public List<EventRuleValidationIssue> CollectValidationIssues(DiceFaceSlotType slot)
        {
            List<EventRuleValidationIssue> issues = new();

            if (!AllowsSlot(slot))
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "slot-not-allowed",
                    $"Rule cannot be equipped in {slot}.",
                    this));
            }

            if (trigger == null)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-trigger",
                    "Rule requires a trigger.",
                    this));
            }
            else
            {
                trigger.CollectValidationIssues(issues);
            }

            CollectConditionIssues(conditions, issues);

            if (results == null || results.Count == 0)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-results",
                    "Rule requires at least one result.",
                    this));
                return issues;
            }

            for (int index = 0; index < results.Count; index++)
            {
                EventResultEntry entry = results[index];
                if (entry == null)
                {
                    issues.Add(new EventRuleValidationIssue(
                        EventRuleValidationSeverity.Error,
                        "missing-result-entry",
                        "Rule contains an empty result entry.",
                        this));
                    continue;
                }

                CollectConditionIssues(entry.Conditions, issues);
                if (entry.Result == null)
                {
                    issues.Add(new EventRuleValidationIssue(
                        EventRuleValidationSeverity.Error,
                        "missing-result",
                        "Result entry requires a result module.",
                        this));
                    continue;
                }

                entry.Result.CollectValidationIssues(issues);
            }

            if (slot == DiceFaceSlotType.Base && FindPrimaryProjectileDefinition() == null)
            {
                issues.Add(new EventRuleValidationIssue(
                    EventRuleValidationSeverity.Error,
                    "missing-primary-projectile",
                    "基础规则必须提供一个可解析的主弹丸定义。",
                    this));
            }

            return issues;
        }

        public ProjectileDefinition FindPrimaryProjectileDefinition()
        {
            return FindPrimaryProjectileDefinition(
                results,
                new HashSet<DelayResultModule>(),
                new HashSet<DelayResultModule>());
        }

        private static ProjectileDefinition FindPrimaryProjectileDefinition(
            IReadOnlyList<EventResultEntry> entries,
            HashSet<DelayResultModule> visitedDelays,
            HashSet<DelayResultModule> activeDelays)
        {
            if (entries == null)
            {
                return null;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                EventResultModule result = entries[index]?.Result;
                if (result is IEventRuleProjectileDefinitionProvider provider &&
                    provider.IsPrimaryProjectile &&
                    provider.ProjectileDefinition != null)
                {
                    return provider.ProjectileDefinition;
                }

                if (result is DelayResultModule delay)
                {
                    if (activeDelays.Contains(delay) || !visitedDelays.Add(delay))
                    {
                        continue;
                    }

                    activeDelays.Add(delay);
                    ProjectileDefinition nested = FindPrimaryProjectileDefinition(
                        delay.Entries,
                        visitedDelays,
                        activeDelays);
                    activeDelays.Remove(delay);
                    if (nested != null)
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static void CollectConditionIssues(
            IReadOnlyList<EventConditionModule> modules,
            List<EventRuleValidationIssue> issues)
        {
            if (modules == null)
            {
                return;
            }

            for (int index = 0; index < modules.Count; index++)
            {
                EventConditionModule module = modules[index];
                if (module == null)
                {
                    issues.Add(new EventRuleValidationIssue(
                        EventRuleValidationSeverity.Error,
                        "missing-condition",
                        "Rule contains an empty condition module.",
                        null));
                    continue;
                }

                module.CollectValidationIssues(issues);
            }
        }

        private static DiceFaceSlotMask ToMask(DiceFaceSlotType slot)
        {
            return slot switch
            {
                DiceFaceSlotType.Base => DiceFaceSlotMask.Base,
                DiceFaceSlotType.OnFire => DiceFaceSlotMask.OnFire,
                DiceFaceSlotType.OnHit => DiceFaceSlotMask.OnHit,
                DiceFaceSlotType.OnFireEnd => DiceFaceSlotMask.OnFireEnd,
                _ => DiceFaceSlotMask.None
            };
        }
    }
}
