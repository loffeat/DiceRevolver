using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Editor
{
    public sealed class EventRuleEditorWindow : EditorWindow
    {
        private const float LeftMinimumWidth = 180f;
        private const float MiddleMinimumWidth = 260f;
        private const float RightMinimumWidth = 420f;
        private static readonly DiceFaceSlotType[] SlotFilters =
        {
            DiceFaceSlotType.Base,
            DiceFaceSlotType.OnFire,
            DiceFaceSlotType.OnHit,
            DiceFaceSlotType.OnFireEnd,
            DiceFaceSlotType.Passive
        };
        private static readonly string[] SlotNames =
        {
            "基础", "开火时", "命中时", "开火后", "被动"
        };

        [SerializeField] private EventRuleEditorSelection selectionState = new();
        [SerializeField] private Vector2 leftScroll;
        [SerializeField] private Vector2 middleScroll;
        [SerializeField] private Vector2 rightScroll;
        [SerializeField] private string renameText = string.Empty;

        [NonSerialized] private List<EventRuleDefinition> visibleRules;
        [NonSerialized] private IReadOnlyList<EventRuleValidationIssue> validationIssues =
            Array.Empty<EventRuleValidationIssue>();
        [NonSerialized] private IReadOnlyList<DiceFaceEntry> references = Array.Empty<DiceFaceEntry>();
        [NonSerialized] private EventRuleDebugSource debugSource;
        [NonSerialized] private int editorRefreshTickCount;

        internal EventRuleEditorSelection SelectionState =>
            selectionState ??= new EventRuleEditorSelection();
        internal EventRuleDefinition SelectedRule => SelectionState.ResolveSelectedRule();
        internal IReadOnlyList<EventRuleDefinition> VisibleRules =>
            visibleRules ?? (IReadOnlyList<EventRuleDefinition>)RefreshRules();
        internal IReadOnlyList<EventRuleValidationIssue> ValidationIssues => validationIssues;
        internal int EditorRefreshTickCount => editorRefreshTickCount;

        [MenuItem("Window/Dice Revolver/事件规则编辑器")]
        public static void Open()
        {
            EventRuleEditorWindow window = GetWindow<EventRuleEditorWindow>();
            window.titleContent = new GUIContent("事件规则编辑器");
            window.minSize = new Vector2(
                LeftMinimumWidth + MiddleMinimumWidth + RightMinimumWidth,
                480f);
            window.Show();
        }

        private void OnEnable()
        {
            selectionState ??= new EventRuleEditorSelection();
            debugSource ??= new EventRuleDebugSource();
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;
            EditorApplication.projectChanged += HandleProjectChanged;
            RefreshRules();
            RefreshValidation();
            RefreshReferences();
        }

        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.projectChanged -= HandleProjectChanged;
        }

        private void HandleEditorUpdate()
        {
            editorRefreshTickCount++;
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void HandleProjectChanged()
        {
            RefreshRules();
            RefreshValidation();
            RefreshReferences();
            Repaint();
        }

        internal IReadOnlyList<EventRuleDefinition> RefreshRules()
        {
            EventRuleDefinition[] allRules = EventRuleAssetUtility.FindRules().ToArray();
            visibleRules = allRules
                .Where(rule => SelectionState.Matches(
                    rule,
                    EventRuleValidator.Validate(rule, SelectionState.SlotFilter)))
                .ToList();
            return visibleRules.AsReadOnly();
        }

        internal void SelectRule(EventRuleDefinition rule)
        {
            SelectionState.Select(rule);
            renameText = rule != null ? rule.name : string.Empty;
            RefreshValidation();
            RefreshReferences();
            Repaint();
        }

        internal EventRuleDefinition CreateRuleAt(string assetPath)
        {
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(assetPath);
            SelectRule(rule);
            RefreshRules();
            return rule;
        }

        internal EventRuleDefinition DuplicateSelectedTo(string assetPath)
        {
            EventRuleDefinition selected = RequireSelectedRule();
            EventRuleDefinition duplicate = EventRuleAssetUtility.DuplicateRule(selected, assetPath);
            SelectRule(duplicate);
            RefreshRules();
            return duplicate;
        }

        internal bool RenameSelected(string newName)
        {
            EventRuleDefinition selected = SelectedRule;
            if (selected == null || !EventRuleAssetUtility.RenameRule(selected, newName))
            {
                return false;
            }

            renameText = selected.name;
            RefreshRules();
            RefreshReferences();
            return true;
        }

        internal EventRuleDefinition PingSelected() =>
            EventRuleAssetUtility.PingRule(SelectedRule);

        internal ScriptableObject AddTrigger(Type moduleType) =>
            Mutate(() => EventRuleAssetUtility.AddModule(
                RequireSelectedRule(), moduleType, "trigger"));

        internal bool RemoveTrigger() =>
            Mutate(() => EventRuleAssetUtility.RemoveModule(
                RequireSelectedRule(), "trigger"));

        internal ScriptableObject AddRuleCondition(Type moduleType) =>
            Mutate(() => EventRuleAssetUtility.AddModuleToArray(
                RequireSelectedRule(), moduleType, "conditions"));

        internal bool RemoveRuleCondition(int index) =>
            Mutate(() => EventRuleAssetUtility.RemoveModuleFromArray(
                RequireSelectedRule(), "conditions", index));

        internal ScriptableObject AddResult(Type moduleType) =>
            Mutate(() => EventRuleAssetUtility.AddResultEntry(
                RequireSelectedRule(), moduleType));

        internal bool RemoveResult(int index) =>
            Mutate(() => EventRuleAssetUtility.RemoveResultEntry(
                RequireSelectedRule(), index));

        internal ScriptableObject AddResultCondition(int resultIndex, Type moduleType) =>
            Mutate(() => EventRuleAssetUtility.AddModuleToArray(
                RequireSelectedRule(),
                moduleType,
                $"results.Array.data[{resultIndex}].conditions"));

        internal bool RemoveResultCondition(int resultIndex, int conditionIndex) =>
            Mutate(() => EventRuleAssetUtility.RemoveModuleFromArray(
                RequireSelectedRule(),
                $"results.Array.data[{resultIndex}].conditions",
                conditionIndex));

        internal bool MoveResult(int fromIndex, int toIndex) =>
            Mutate(() => EventRuleAssetUtility.MoveResult(
                RequireSelectedRule(), fromIndex, toIndex));

        internal void RefreshValidation()
        {
            EventRuleDefinition selected = SelectedRule;
            validationIssues = selected != null
                ? EventRuleValidator.Validate(selected, SelectionState.SlotFilter)
                : Array.Empty<EventRuleValidationIssue>();
        }

        private void RefreshReferences()
        {
            EventRuleDefinition selected = SelectedRule;
            if (selected == null)
            {
                references = Array.Empty<DiceFaceEntry>();
                return;
            }

            references = AssetDatabase.FindAssets("t:DiceFaceEntry")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DiceFaceEntry>)
                .Where(entry => entry != null && entry.Rule == selected)
                .OrderBy(entry => AssetDatabase.GetAssetPath(entry), StringComparer.Ordinal)
                .ToArray();
        }

        private T Mutate<T>(Func<T> command)
        {
            T result = command.Invoke();
            RefreshRules();
            RefreshValidation();
            RefreshReferences();
            Repaint();
            return result;
        }

        private EventRuleDefinition RequireSelectedRule() =>
            SelectedRule != null
                ? SelectedRule
                : throw new InvalidOperationException("No Event Rule is selected.");

        private void OnGUI()
        {
            debugSource ??= new EventRuleDebugSource();
            visibleRules ??= RefreshRules().ToList();

            GUILayout.BeginHorizontal();
            DrawLeftColumn();
            DrawMiddleColumn();
            DrawRightColumn();
            GUILayout.EndHorizontal();
        }

        private void DrawLeftColumn()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox,
                GUILayout.MinWidth(LeftMinimumWidth), GUILayout.MaxWidth(240f),
                GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("事件类型", EditorStyles.boldLabel);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            for (int index = 0; index < SlotFilters.Length; index++)
            {
                bool selected = SelectionState.SlotFilter == SlotFilters[index];
                if (GUILayout.Toggle(selected, SlotNames[index], "Button") && !selected)
                {
                    SelectionState.SlotFilter = SlotFilters[index];
                    FiltersChanged();
                }
            }

            EditorGUILayout.Space();
            string[] tags = BuildTagOptions();
            int currentTag = FindTagIndex(tags, SelectionState.TagFilter);
            int nextTag = EditorGUILayout.Popup("标签", currentTag, tags);
            if (nextTag != currentTag)
            {
                SelectionState.TagFilter = nextTag <= 0 ? string.Empty : tags[nextTag];
                FiltersChanged();
            }

            bool errorOnly = EditorGUILayout.ToggleLeft("仅显示错误规则", SelectionState.ErrorOnly);
            if (errorOnly != SelectionState.ErrorOnly)
            {
                SelectionState.ErrorOnly = errorOnly;
                FiltersChanged();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawMiddleColumn()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox,
                GUILayout.MinWidth(MiddleMinimumWidth), GUILayout.MaxWidth(380f),
                GUILayout.ExpandHeight(true));
            string search = EditorGUILayout.TextField("搜索", SelectionState.SearchText);
            if (search != SelectionState.SearchText)
            {
                SelectionState.SearchText = search;
                FiltersChanged();
            }

            middleScroll = EditorGUILayout.BeginScrollView(middleScroll);
            for (int index = 0; index < VisibleRules.Count; index++)
            {
                EventRuleDefinition rule = VisibleRules[index];
                string label = string.IsNullOrWhiteSpace(rule.DisplayName)
                    ? rule.name
                    : rule.DisplayName;
                bool selected = rule == SelectedRule;
                if (GUILayout.Toggle(selected, label, "Button") && !selected)
                {
                    SelectRule(rule);
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("新建"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "新建事件规则", "EventRule", "asset", "选择规则资源路径");
                if (!string.IsNullOrEmpty(path))
                {
                    CreateRuleAt(path);
                }
            }

            EditorGUI.BeginDisabledGroup(SelectedRule == null);
            if (GUILayout.Button("复制"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "复制事件规则", $"{SelectedRule.name} Copy", "asset", "选择副本资源路径");
                if (!string.IsNullOrEmpty(path))
                {
                    DuplicateSelectedTo(path);
                }
            }

            if (GUILayout.Button("定位"))
            {
                PingSelected();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            renameText = EditorGUILayout.TextField(renameText);
            EditorGUI.BeginDisabledGroup(SelectedRule == null || string.IsNullOrWhiteSpace(renameText));
            if (GUILayout.Button("重命名", GUILayout.Width(72f)))
            {
                RenameSelected(renameText);
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawRightColumn()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox,
                GUILayout.MinWidth(RightMinimumWidth), GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
            EventRuleDefinition rule = SelectedRule;
            if (rule == null)
            {
                EditorGUILayout.HelpBox("从中栏选择一个事件规则。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            DrawRuleMetadata(rule);
            DrawTrigger(rule);
            DrawRuleConditions(rule);
            DrawResults(rule);
            DrawValidation();
            DrawReferences();
            DrawDebug();
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawRuleMetadata(EventRuleDefinition rule)
        {
            EditorGUILayout.LabelField("基础信息与允许槽位", EditorStyles.boldLabel);
            SerializedObject serialized = new SerializedObject(rule);
            EditorGUI.BeginChangeCheck();
            DrawProperty(serialized, "displayName");
            DrawProperty(serialized, "description");
            DrawProperty(serialized, "displayColor");
            DrawProperty(serialized, "tags", true);
            DrawProperty(serialized, "rarity");
            DrawProperty(serialized, "allowedSlots");
            DrawProperty(serialized, "eventBudgetCost");
            DrawProperty(serialized, "recursionPolicy");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(rule, "Edit Event Rule");
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(rule);
                AssetDatabase.SaveAssets();
                RefreshRules();
                RefreshValidation();
            }
        }

        private void DrawTrigger(EventRuleDefinition rule)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
            if (rule.Trigger == null)
            {
                if (GUILayout.Button("添加 Trigger"))
                {
                    ShowModuleMenu<EventTriggerModule>(type => AddTrigger(type));
                }
                return;
            }

            DrawModule(rule.Trigger);
            if (GUILayout.Button("移除 Trigger"))
            {
                RemoveTrigger();
            }
        }

        private void DrawRuleConditions(EventRuleDefinition rule)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("规则 Conditions（AND）", EditorStyles.boldLabel);
            for (int index = 0; index < rule.Conditions.Count; index++)
            {
                DrawModule(rule.Conditions[index]);
                if (GUILayout.Button($"移除条件 {index + 1}"))
                {
                    RemoveRuleCondition(index);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("添加规则 Condition"))
            {
                ShowModuleMenu<EventConditionModule>(type => AddRuleCondition(type));
            }
        }

        private void DrawResults(EventRuleDefinition rule)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("有序 ResultEntries", EditorStyles.boldLabel);
            for (int resultIndex = 0; resultIndex < rule.Results.Count; resultIndex++)
            {
                EventResultEntry entry = rule.Results[resultIndex];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                Rect header = EditorGUILayout.GetControlRect(false, 20f);
                GUI.Label(header, $"Result {resultIndex + 1}", EditorStyles.boldLabel);
                HandleResultDrag(header, resultIndex);
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(resultIndex == 0);
                if (GUILayout.Button("↑"))
                {
                    MoveResult(resultIndex, resultIndex - 1);
                    GUIUtility.ExitGUI();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(resultIndex >= rule.Results.Count - 1);
                if (GUILayout.Button("↓"))
                {
                    MoveResult(resultIndex, resultIndex + 1);
                    GUIUtility.ExitGUI();
                }
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("删除"))
                {
                    RemoveResult(resultIndex);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("局部 Conditions（AND）", EditorStyles.miniBoldLabel);
                if (entry != null)
                {
                    for (int conditionIndex = 0; conditionIndex < entry.Conditions.Count; conditionIndex++)
                    {
                        DrawModule(entry.Conditions[conditionIndex]);
                        if (GUILayout.Button($"移除局部条件 {conditionIndex + 1}"))
                        {
                            RemoveResultCondition(resultIndex, conditionIndex);
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button("添加局部 Condition"))
                    {
                        int capturedResult = resultIndex;
                        ShowModuleMenu<EventConditionModule>(
                            type => AddResultCondition(capturedResult, type));
                    }

                    EditorGUILayout.LabelField("Result", EditorStyles.miniBoldLabel);
                    DrawModule(entry.Result);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加 Result"))
            {
                ShowModuleMenu<EventResultModule>(type => AddResult(type));
            }
        }

        private void HandleResultDrag(Rect target, int targetIndex)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && target.Contains(current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("DiceRevolver.EventRuleResultIndex", targetIndex);
                DragAndDrop.StartDrag($"Result {targetIndex + 1}");
                current.Use();
            }
            else if ((current.type == EventType.DragUpdated || current.type == EventType.DragPerform) &&
                     target.Contains(current.mousePosition) &&
                     DragAndDrop.GetGenericData("DiceRevolver.EventRuleResultIndex") is int sourceIndex)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                if (current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (sourceIndex != targetIndex)
                    {
                        MoveResult(sourceIndex, targetIndex);
                    }
                }
                current.Use();
            }
        }

        private static void DrawModule(ScriptableObject module)
        {
            if (module == null)
            {
                EditorGUILayout.HelpBox("模块引用为空。", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(module.GetType().Name, EditorStyles.miniBoldLabel);
            SerializedObject serialized = new SerializedObject(module);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            EditorGUI.BeginChangeCheck();
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(module, "Edit Event Rule Module");
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(module);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("实时校验", EditorStyles.boldLabel);
            if (GUILayout.Button("刷新校验"))
            {
                RefreshValidation();
            }

            for (int index = 0; index < validationIssues.Count; index++)
            {
                EventRuleValidationIssue issue = validationIssues[index];
                MessageType type = issue.Severity switch
                {
                    EventRuleValidationSeverity.Error => MessageType.Error,
                    EventRuleValidationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                EditorGUILayout.HelpBox($"[{issue.Code}] {issue.Message}", type);
            }
        }

        private void DrawReferences()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("引用关系", EditorStyles.boldLabel);
            if (references.Count == 0)
            {
                EditorGUILayout.LabelField("没有 DiceFaceEntry 引用此规则。", EditorStyles.miniLabel);
                return;
            }

            for (int index = 0; index < references.Count; index++)
            {
                EditorGUILayout.ObjectField(references[index], typeof(DiceFaceEntry), false);
            }
        }

        private void DrawDebug()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode 详细 Debug", EditorStyles.boldLabel);
            EventRuleDebugSnapshot snapshot = debugSource.ReadSelected();
            if (!string.IsNullOrEmpty(snapshot.Message))
            {
                EditorGUILayout.HelpBox(snapshot.Message, MessageType.Info);
                return;
            }

            for (int index = 0; index < snapshot.Records.Count; index++)
            {
                CombatDebugRecord record = snapshot.Records[index];
                EditorGUILayout.LabelField(
                    record.Verbose ? $"[详细] {CombatDebugFormatter.Format(record)}" : CombatDebugFormatter.Format(record),
                    EditorStyles.wordWrappedLabel);
            }
        }

        private static void DrawProperty(SerializedObject serialized, string path, bool children = false)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, children);
            }
        }

        private static void ShowModuleMenu<T>(Action<Type> selected)
            where T : ScriptableObject
        {
            GenericMenu menu = new GenericMenu();
            IReadOnlyList<Type> modules = EventRuleModuleCatalog.GetModules<T>();
            for (int index = 0; index < modules.Count; index++)
            {
                Type moduleType = modules[index];
                EventRuleModuleMenuAttribute attribute =
                    (EventRuleModuleMenuAttribute)Attribute.GetCustomAttribute(
                        moduleType,
                        typeof(EventRuleModuleMenuAttribute));
                menu.AddItem(new GUIContent(attribute.Path), false, () => selected.Invoke(moduleType));
            }

            if (modules.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有可用模块"));
            }

            menu.ShowAsContext();
        }

        private string[] BuildTagOptions()
        {
            IEnumerable<string> tags = EventRuleAssetUtility.FindRules()
                .SelectMany(rule => rule.Tags ?? Array.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase);
            return new[] { "全部标签" }.Concat(tags).ToArray();
        }

        private static int FindTagIndex(IReadOnlyList<string> tags, string selected)
        {
            if (string.IsNullOrEmpty(selected))
            {
                return 0;
            }

            for (int index = 1; index < tags.Count; index++)
            {
                if (string.Equals(tags[index], selected, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return 0;
        }

        private void FiltersChanged()
        {
            RefreshRules();
            RefreshValidation();
            Repaint();
        }
    }
}
