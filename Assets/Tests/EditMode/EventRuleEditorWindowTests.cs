using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceRevolver.Editor;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    public sealed class EventRuleEditorWindowTests
    {
        private const string TempFolder = "Assets/Tests/TempEventRuleWindow";
        private EventRuleEditorWindow window;
        private GameObject gunOwner;
        private GameObject debugOverlayOwner;

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();
            AssetDatabase.CreateFolder("Assets/Tests", "TempEventRuleWindow");
            window = EditorWindow.CreateInstance<EventRuleEditorWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;
            if (window != null)
            {
                Object.DestroyImmediate(window);
            }

            if (gunOwner != null)
            {
                Object.DestroyImmediate(gunOwner);
            }

            if (debugOverlayOwner != null)
            {
                Object.DestroyImmediate(debugOverlayOwner);
            }

            DeleteTempFolder();
            Assert.That(AssetDatabase.IsValidFolder(TempFolder), Is.False);
        }

        [Test]
        public void SelectionCombinesSlotTagErrorAndCaseInsensitiveSearchFilters()
        {
            EventRuleDefinition matching = CreateRule("Matching");
            ConfigureMetadata(
                matching,
                "Lightning Burst",
                "Arc result",
                DiceFaceSlotMask.OnHit,
                "Shock",
                "Rare");
            EventRuleDefinition wrongSlot = CreateRule("WrongSlot");
            ConfigureMetadata(
                wrongSlot,
                "Lightning Burst",
                "Arc result",
                DiceFaceSlotMask.OnFire,
                "Shock");
            EventRuleDefinition wrongTag = CreateRule("WrongTag");
            ConfigureMetadata(
                wrongTag,
                "Lightning Burst",
                "Arc result",
                DiceFaceSlotMask.OnHit,
                "Fire");

            EventRuleEditorSelection state = window.SelectionState;
            state.ShowAllEvents = false;
            state.SlotFilter = DiceFaceSlotType.OnHit;
            state.TagFilter = "sHoCk";
            state.ErrorOnly = true;
            state.SearchText = "liGHTning";
            EventRuleValidationIssue error = new EventRuleValidationIssue(
                EventRuleValidationSeverity.Error,
                "TEST",
                "broken",
                matching);

            Assert.That(state.Matches(matching, new[] { error }), Is.True);
            Assert.That(state.Matches(matching, Array.Empty<EventRuleValidationIssue>()), Is.False);
            Assert.That(state.Matches(wrongSlot, new[] { error }), Is.False);
            Assert.That(state.Matches(wrongTag, new[] { error }), Is.False);

            state.ErrorOnly = false;
            state.TagFilter = string.Empty;
            state.SearchText = "rArE";
            Assert.That(state.Matches(matching, Array.Empty<EventRuleValidationIssue>()), Is.True,
                "Search should also cover rarity metadata.");
        }

        [Test]
        public void SelectedRuleSurvivesAssetRenameBecauseSelectionStoresItsGuid()
        {
            EventRuleDefinition rule = CreateRule("BeforeRename");
            window.SelectRule(rule);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rule));

            Assert.That(window.SelectionState.SelectedAssetGuid, Is.EqualTo(guid));
            Assert.That(window.RenameSelected("AfterRename"), Is.True);

            Assert.That(window.SelectedRule, Is.Not.Null);
            Assert.That(window.SelectedRule.name, Is.EqualTo("AfterRename"));
            Assert.That(window.SelectionState.SelectedAssetGuid, Is.EqualTo(guid));
            Assert.That(AssetDatabase.GetAssetPath(window.SelectedRule),
                Is.EqualTo($"{TempFolder}/AfterRename.asset"));
        }

        [Test]
        public void WindowCreateDuplicateAndPingCommandsOperateOnRealAssets()
        {
            EventRuleDefinition created = window.CreateRuleAt($"{TempFolder}/Created.asset");
            ConfigureMetadata(created, "Created Rule", "", DiceFaceSlotMask.OnFire, "starter");

            EventRuleDefinition duplicate = window.DuplicateSelectedTo(
                $"{TempFolder}/CreatedCopy.asset");
            EventRuleDefinition pinged = window.PingSelected();

            Assert.That(created, Is.Not.Null);
            Assert.That(duplicate, Is.Not.SameAs(created));
            Assert.That(duplicate.DisplayName, Is.EqualTo("Created Rule"));
            Assert.That(duplicate.Tags, Is.EqualTo(new[] { "starter" }));
            Assert.That(pinged, Is.SameAs(duplicate));
            Assert.That(window.SelectedRule, Is.SameAs(duplicate));
        }

        [Test]
        public void WindowModuleCommandsCreateAndRemoveOwnedSubAssets()
        {
            EventRuleDefinition rule = window.CreateRuleAt($"{TempFolder}/Modules.asset");

            ScriptableObject trigger = window.AddTrigger(typeof(SignalTypeTriggerModule));
            ScriptableObject condition = window.AddRuleCondition(typeof(FaceAvailableConditionModule));
            ScriptableObject result = window.AddResult(typeof(SetDrawPriorityResultModule));
            ScriptableObject localCondition = window.AddResultCondition(
                0,
                typeof(CounterComparisonConditionModule));

            string path = AssetDatabase.GetAssetPath(rule);
            Assert.That(AssetDatabase.GetAssetPath(trigger), Is.EqualTo(path));
            Assert.That(AssetDatabase.GetAssetPath(condition), Is.EqualTo(path));
            Assert.That(AssetDatabase.GetAssetPath(result), Is.EqualTo(path));
            Assert.That(AssetDatabase.GetAssetPath(localCondition), Is.EqualTo(path));
            Assert.That(rule.Trigger, Is.SameAs(trigger));
            Assert.That(rule.Conditions, Has.Count.EqualTo(1));
            Assert.That(rule.Results, Has.Count.EqualTo(1));
            Assert.That(rule.Results[0].Conditions, Has.Count.EqualTo(1));

            Assert.That(window.RemoveResultCondition(0, 0), Is.True);
            Assert.That(window.RemoveRuleCondition(0), Is.True);
            Assert.That(window.RemoveTrigger(), Is.True);
            Assert.That(window.RemoveResult(0), Is.True);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Assert.That(rule.Trigger, Is.Null);
            Assert.That(rule.Conditions, Is.Empty);
            Assert.That(rule.Results, Is.Empty);
            Assert.That(
                AssetDatabase.LoadAllAssetsAtPath(path).Where(asset =>
                    asset is EventTriggerModule ||
                    asset is EventConditionModule ||
                    asset is EventResultModule),
                Is.Empty);
        }

        [Test]
        public void WindowReplaceTriggerWithSameTypeKeepsExistingModule()
        {
            EventRuleDefinition rule = window.CreateRuleAt($"{TempFolder}/SameTrigger.asset");
            ScriptableObject original = window.AddTrigger(typeof(SignalTypeTriggerModule));

            ScriptableObject same = window.ReplaceTrigger(typeof(SignalTypeTriggerModule));

            Assert.That(same, Is.SameAs(original));
            Assert.That(rule.Trigger, Is.SameAs(original));
        }

        [Test]
        public void WindowMovesResultEntriesThroughTheAssetCommandBoundary()
        {
            EventRuleDefinition rule = window.CreateRuleAt($"{TempFolder}/Ordered.asset");
            EventResultModule first = (EventResultModule)window.AddResult(
                typeof(SetDrawPriorityResultModule));
            EventResultModule second = (EventResultModule)window.AddResult(
                typeof(ForceFaceResultModule));

            Assert.That(window.MoveResult(0, 1), Is.True);

            Assert.That(rule.Results.Select(entry => entry.Result),
                Is.EqualTo(new[] { second, first }));
        }

        [Test]
        public void AllEventsFilterMatchesRulesOfAnySlotByDefault()
        {
            EventRuleDefinition baseRule = CreateRule("AnyBase");
            ConfigureMetadata(baseRule, "Anything", "", DiceFaceSlotMask.Base, "base");
            EventRuleDefinition onFireRule = CreateRule("AnyOnFire");
            ConfigureMetadata(onFireRule, "Anything", "", DiceFaceSlotMask.OnFire, "fire");
            EventRuleDefinition onFireEndRule = CreateRule("AnyOnFireEnd");
            ConfigureMetadata(onFireEndRule, "Anything", "", DiceFaceSlotMask.OnFireEnd, "end");

            EventRuleEditorSelection state = window.SelectionState;
            Assert.That(state.ShowAllEvents, Is.True);
            Assert.That(state.Matches(baseRule, Array.Empty<EventRuleValidationIssue>()), Is.True);
            Assert.That(state.Matches(onFireRule, Array.Empty<EventRuleValidationIssue>()), Is.True);
            Assert.That(state.Matches(onFireEndRule, Array.Empty<EventRuleValidationIssue>()), Is.True);
        }

        [Test]
        public void SelectingASlotClearsTheAllEventsFilter()
        {
            EventRuleEditorSelection state = window.SelectionState;
            state.ShowAllEvents = false;
            state.SlotFilter = DiceFaceSlotType.OnHit;
            EventRuleDefinition wrongSlot = CreateRule("WrongSlot");
            ConfigureMetadata(wrongSlot, "Anything", "", DiceFaceSlotMask.OnFire, "fire");

            Assert.That(state.Matches(wrongSlot, Array.Empty<EventRuleValidationIssue>()), Is.False);

            state.ShowAllEvents = true;
            Assert.That(state.Matches(wrongSlot, Array.Empty<EventRuleValidationIssue>()), Is.True);
        }

        [Test]
        public void ValidationRefreshReplacesStaleIssuesAfterCommands()
        {
            window.CreateRuleAt($"{TempFolder}/Validation.asset");
            window.SelectionState.ShowAllEvents = false;
            window.SelectionState.SlotFilter = DiceFaceSlotType.OnFire;
            window.RefreshValidation();
            Assert.That(window.ValidationIssues.Select(issue => issue.Code),
                Does.Contain(EventRuleValidator.RuleTriggerMissing));
            Assert.That(window.ValidationIssues.Select(issue => issue.Code),
                Does.Contain(EventRuleValidator.RuleResultsEmpty));

            window.AddTrigger(typeof(SignalTypeTriggerModule));
            window.AddResult(typeof(SetDrawPriorityResultModule));
            window.RefreshValidation();

            Assert.That(window.ValidationIssues.Select(issue => issue.Code),
                Does.Not.Contain(EventRuleValidator.RuleTriggerMissing));
            Assert.That(window.ValidationIssues.Select(issue => issue.Code),
                Does.Not.Contain(EventRuleValidator.RuleResultsEmpty));
        }

        [Test]
        public void DebugSourceProjectsAllSelectedGunRecordsOnlyWhilePlaying()
        {
            gunOwner = new GameObject("Selected Gun", typeof(DiceRevolverGun));
            DiceRevolverGun gun = gunOwner.GetComponent<DiceRevolverGun>();
            CombatDebugScope scope = gun.DebugTrace.BeginActivation(4, false, default, 3f);
            gun.DebugTrace.Record(
                scope,
                CombatDebugEventType.RuleTrigger,
                "规则",
                "trigger",
                null,
                0,
                3f);
            gun.DebugTrace.Record(
                scope,
                CombatDebugEventType.RuleCondition,
                "规则",
                "condition",
                "passed",
                1,
                3.1f,
                true);
            Selection.activeGameObject = gunOwner;

            EventRuleDebugSnapshot playing = new EventRuleDebugSource(() => true).ReadSelected();
            EventRuleDebugSnapshot editing = new EventRuleDebugSource(() => false).ReadSelected();

            Assert.That(playing.Message, Is.Empty);
            Assert.That(playing.Records.Select(record => record.EventType),
                Is.EqualTo(new[]
                {
                    CombatDebugEventType.RuleTrigger,
                    CombatDebugEventType.RuleCondition
                }));
            Assert.That(playing.Records[1].Verbose, Is.True);
            Assert.That(editing.Records, Is.Empty);
            Assert.That(editing.Message, Is.Not.Empty);
        }

        [Test]
        public void DebugSourceShowsAMessageWhenNoGunIsSelected()
        {
            Selection.activeObject = null;

            EventRuleDebugSnapshot snapshot = new EventRuleDebugSource(() => true).ReadSelected();

            Assert.That(snapshot.Records, Is.Empty);
            Assert.That(snapshot.Message, Does.Contain("Gun"));
        }

        [Test]
        public void SkippedRuleTriggerStaysInEditorProjectionButDoesNotReachDefaultHud()
        {
            gunOwner = new GameObject("Selected Gun", typeof(DiceRevolverGun));
            DiceRevolverGun gun = gunOwner.GetComponent<DiceRevolverGun>();
            debugOverlayOwner = new GameObject(
                "Combat Debug Overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Text),
                typeof(CombatDebugOverlay));
            UnityEngine.UI.Text label = debugOverlayOwner.GetComponent<UnityEngine.UI.Text>();
            CombatDebugOverlay overlay = debugOverlayOwner.GetComponent<CombatDebugOverlay>();
            overlay.Configure(label, gun.DebugTrace, 4, 0f, 16);
            Selection.activeGameObject = gunOwner;

            EventSignal signal = new EventSignal(
                EventSignalType.DrawCandidate,
                3,
                3,
                DiceFaceSlotType.Passive,
                null,
                null,
                default,
                null,
                default,
                Array.Empty<int>(),
                3,
                default,
                null,
                false,
                default);
            PassiveEventRuleServices services = new PassiveEventRuleServices(
                signal,
                null,
                null,
                gun.DebugTrace,
                () => 4f,
                null);
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            rule.name = "Unmatched Rule";
            try
            {
                services.RecordRuleDebug(
                    rule,
                    "trigger",
                    "Trigger did not match.",
                    EventResultStatus.Skipped);

                EventRuleDebugSnapshot editorSnapshot =
                    new EventRuleDebugSource(() => true).ReadSelected();
                Assert.That(gun.DebugTrace.Records, Has.Count.EqualTo(1));
                Assert.That(gun.DebugTrace.Records[0].Verbose, Is.True);
                Assert.That(label.text, Does.Not.Contain("Unmatched Rule"));
                Assert.That(editorSnapshot.Records.Select(record => record.Name),
                    Does.Contain("Unmatched Rule"));
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [UnityTest]
        public IEnumerator WindowReceivesEditorRefreshTicksOnlyWhileItIsEnabled()
        {
            int initialTicks = window.EditorRefreshTickCount;
            for (int attempt = 0; attempt < 3 && window.EditorRefreshTickCount == initialTicks; attempt++)
            {
                yield return null;
            }

            Assert.That(window.EditorRefreshTickCount, Is.GreaterThan(initialTicks));

            EventRuleEditorWindow disabledWindow = window;
            Object.DestroyImmediate(window);
            window = null;
            yield return null;
            int stoppedTicks = disabledWindow.EditorRefreshTickCount;
            yield return null;
            yield return null;

            Assert.That(disabledWindow.EditorRefreshTickCount, Is.EqualTo(stoppedTicks));
        }

        [Test]
        public void RuleServiceMapsGranularStagesAndMarksConditionsVerbose()
        {
            CombatDebugTrace trace = new CombatDebugTrace();
            EventSignal signal = new EventSignal(
                EventSignalType.DrawCandidate,
                3,
                3,
                DiceFaceSlotType.Passive,
                null,
                null,
                default,
                null,
                default,
                Array.Empty<int>(),
                3,
                default,
                null,
                false,
                default);
            PassiveEventRuleServices services = new PassiveEventRuleServices(
                signal,
                null,
                null,
                trace,
                () => 7.5f,
                null);
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            rule.name = "Debug Rule";
            try
            {
                services.RecordRuleDebug(rule, "trigger", null, EventResultStatus.Success);
                services.RecordRuleDebug(rule, "rule-condition", "passed", EventResultStatus.Success);
                services.RecordRuleDebug(rule, "result", "done", EventResultStatus.Success);

                Assert.That(trace.Records.Select(record => record.EventType),
                    Is.EqualTo(new[]
                    {
                        CombatDebugEventType.RuleTrigger,
                        CombatDebugEventType.RuleCondition,
                        CombatDebugEventType.RuleResult
                    }));
                Assert.That(trace.Records.Select(record => record.Verbose),
                    Is.EqualTo(new[] { false, true, false }));
                Assert.That(trace.Records.Select(record => record.Sequence),
                    Is.EqualTo(new long[] { 1, 2, 3 }));
                Assert.That(trace.Records.All(record => record.ChainId == trace.Records[0].ChainId),
                    Is.True);
                Assert.That(trace.Records.All(record => record.Timestamp == 7.5f), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        private static EventRuleDefinition CreateRule(string name) =>
            EventRuleAssetUtility.CreateRule($"{TempFolder}/{name}.asset");

        private static void ConfigureMetadata(
            EventRuleDefinition rule,
            string displayName,
            string description,
            DiceFaceSlotMask slots,
            string tag,
            string rarity = "")
        {
            SerializedObject serialized = new SerializedObject(rule);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("allowedSlots").intValue = (int)slots;
            serialized.FindProperty("rarity").stringValue = rarity;
            SerializedProperty tags = serialized.FindProperty("tags");
            tags.arraySize = 1;
            tags.GetArrayElementAtIndex(0).stringValue = tag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rule);
            AssetDatabase.SaveAssets();
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }

            AssetDatabase.Refresh();
        }
    }
}
