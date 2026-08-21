using System;
using System.Collections.Generic;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    [Serializable]
    internal sealed class EventRuleEditorSelection
    {
        [SerializeField] private DiceFaceSlotType slotFilter = DiceFaceSlotType.Base;
        [SerializeField] private string tagFilter = string.Empty;
        [SerializeField] private bool errorOnly;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string selectedAssetGuid = string.Empty;

        internal DiceFaceSlotType SlotFilter
        {
            get => slotFilter;
            set => slotFilter = value;
        }

        internal string TagFilter
        {
            get => tagFilter ?? string.Empty;
            set => tagFilter = value ?? string.Empty;
        }

        internal bool ErrorOnly
        {
            get => errorOnly;
            set => errorOnly = value;
        }

        internal string SearchText
        {
            get => searchText ?? string.Empty;
            set => searchText = value ?? string.Empty;
        }

        internal string SelectedAssetGuid => selectedAssetGuid ?? string.Empty;

        internal void Select(EventRuleDefinition rule)
        {
            string path = rule != null ? AssetDatabase.GetAssetPath(rule) : string.Empty;
            selectedAssetGuid = string.IsNullOrEmpty(path)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(path);
        }

        internal EventRuleDefinition ResolveSelectedRule()
        {
            if (string.IsNullOrEmpty(selectedAssetGuid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(selectedAssetGuid);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
        }

        internal bool Matches(
            EventRuleDefinition rule,
            IReadOnlyList<EventRuleValidationIssue> issues)
        {
            if (rule == null || !rule.AllowsSlot(slotFilter))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TagFilter) &&
                !Contains(rule.Tags, TagFilter))
            {
                return false;
            }

            if (errorOnly && !ContainsError(issues))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(SearchText) ||
                   Contains(rule.DisplayName, SearchText) ||
                   Contains(rule.name, SearchText) ||
                   Contains(rule.Description, SearchText) ||
                   Contains(rule.Rarity, SearchText) ||
                   Contains(rule.Tags, SearchText);
        }

        private static bool Contains(IReadOnlyList<string> values, string expected)
        {
            if (values == null)
            {
                return false;
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(string value, string fragment) =>
            !string.IsNullOrEmpty(value) &&
            value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool ContainsError(IReadOnlyList<EventRuleValidationIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }

            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == EventRuleValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
