using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceRevolver.Editor;
using DiceRevolver.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceRevolver.Tests
{
    [EventRuleModuleMenu("触发器/测试/目录发现")]
    public sealed class CatalogTestTriggerModule : EventTriggerModule
    {
        public override bool Matches(EventSignal signal) => true;
    }

    [EventRuleModuleMenu("条件/测试/目录发现")]
    public sealed class CatalogTestConditionModule : EventConditionModule
    {
        public override EventConditionResult Evaluate(EventEvaluationContext context)
        {
            return new EventConditionResult(true, "test");
        }
    }

    [EventRuleModuleMenu("结果/测试/目录发现")]
    public sealed class CatalogTestResultModule : EventResultModule
    {
        [SerializeField] private string marker;

        public string Marker => marker;

        public override EventResult Execute(EventExecutionContext context)
        {
            return new EventResult(EventResultStatus.Success, marker ?? string.Empty);
        }
    }

    [EventRuleModuleMenu("结果/测试/抽象类型")]
    public abstract class AbstractCatalogTestResultModule : EventResultModule
    {
    }

    [EventRuleModuleMenu("结果/测试/泛型类型")]
    public sealed class GenericCatalogTestResultModule<T> : EventResultModule
    {
        public override EventResult Execute(EventExecutionContext context)
        {
            return new EventResult(EventResultStatus.Success, typeof(T).Name);
        }
    }

    public sealed class EventRuleEditorInfrastructureTests
    {
        private const string TempFolder = "Assets/Tests/TempEventRules";

        public static void ForceAssetDatabaseRefreshBetweenRuns()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                throw new InvalidOperationException(
                    "Temporary Event Rule assets survived the first infrastructure suite.");
            }
        }

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();
            AssetDatabase.CreateFolder("Assets/Tests", "TempEventRules");
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            DeleteTempFolder();
            Assert.That(AssetDatabase.IsValidFolder(TempFolder), Is.False,
                "Task 6 tests must remove their complete temporary asset folder.");
        }

        [Test]
        public void CatalogDiscoversEveryAttributedConcreteModuleWithoutRegistration()
        {
            AssertCatalogMatchesTypeCache<EventTriggerModule>();
            AssertCatalogMatchesTypeCache<EventConditionModule>();
            AssertCatalogMatchesTypeCache<EventResultModule>();

            IReadOnlyList<Type> first = EventRuleModuleCatalog.GetModules<EventResultModule>();
            IReadOnlyList<Type> second = EventRuleModuleCatalog.GetModules<EventResultModule>();
            Assert.That(first.Contains(typeof(CatalogTestResultModule)), Is.True);
            Assert.That(first.Contains(typeof(AbstractCatalogTestResultModule)), Is.False);
            Assert.That(first.Any(type => type.IsGenericTypeDefinition), Is.False);
            Assert.That(ReferenceEquals(first, second), Is.False,
                "Every caller must receive a fresh read-only view.");
            Assert.Throws<NotSupportedException>(() => ((IList<Type>)first).Add(typeof(EventResultModule)));

            string[] actualOrder = first.Select(type =>
            {
                string path = type.GetCustomAttribute<EventRuleModuleMenuAttribute>().Path;
                return path + "\n" + type.FullName;
            }).ToArray();
            string[] expectedOrder = actualOrder
                .OrderBy(value => value.Split('\n')[0], StringComparer.Ordinal)
                .ThenBy(value => value.Split('\n')[1], StringComparer.Ordinal)
                .ToArray();
            Assert.That(actualOrder, Is.EqualTo(expectedOrder));
        }

        [Test]
        public void FindRulesReturnsSavedRulesFoundByAssetType()
        {
            EventRuleDefinition saved = EventRuleAssetUtility.CreateRule(Path("Searchable.asset"));
            IReadOnlyList<EventRuleDefinition> found = EventRuleAssetUtility.FindRules();

            Assert.That(found.Contains(saved), Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<EventRuleDefinition>)found).Add(saved));
        }

        [Test]
        public void AddModuleAttachesSubAssetAndUndoRedoRestoresTheReference()
        {
            string path = Path("AddUndo.asset");
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(path);
            Undo.ClearAll();

            SignalTypeTriggerModule module = (SignalTypeTriggerModule)
                EventRuleAssetUtility.AddModule(rule, typeof(SignalTypeTriggerModule), "trigger");

            Assert.That(rule.Trigger, Is.SameAs(module));
            Assert.That(AssetDatabase.GetAssetPath(module), Is.EqualTo(path));
            Assert.That(AssetDatabase.IsSubAsset(module), Is.True);

            Undo.PerformUndo();
            AssetDatabase.SaveAssets();
            rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
            Assert.That(rule.Trigger, Is.Null);

            Undo.PerformRedo();
            AssetDatabase.SaveAssets();
            rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
            Assert.That(rule.Trigger, Is.TypeOf<SignalTypeTriggerModule>());
            Assert.That(AssetDatabase.GetAssetPath(rule.Trigger), Is.EqualTo(path));
        }

        [Test]
        public void RemoveArrayModuleClearsListBeforeDestroyAndUndoRedoRestoresBoth()
        {
            string path = Path("RemoveUndo.asset");
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(path);
            AttackEffectConditionModule module = (AttackEffectConditionModule)
                EventRuleAssetUtility.AddModuleToArray(
                    rule, typeof(AttackEffectConditionModule), "conditions");
            Undo.ClearAll();

            Assert.That(EventRuleAssetUtility.RemoveModuleFromArray(rule, "conditions", 0), Is.True);
            Assert.That(rule.Conditions.Count, Is.Zero);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).Contains(module), Is.False);

            Undo.PerformUndo();
            AssetDatabase.SaveAssets();
            rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
            Assert.That(rule.Conditions.Count, Is.EqualTo(1));
            Assert.That(rule.Conditions[0], Is.TypeOf<AttackEffectConditionModule>());
            Assert.That(AssetDatabase.GetAssetPath(rule.Conditions[0]), Is.EqualTo(path));

            Undo.PerformRedo();
            AssetDatabase.SaveAssets();
            rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
            Assert.That(rule.Conditions.Count, Is.Zero);
        }

        [Test]
        public void MoveResultUsesSerializedOrderPersistsAndSupportsUndoRedo()
        {
            string path = Path("MoveUndo.asset");
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(path);
            SetArraySize(rule, "results", 2);
            ForceFaceResultModule first = (ForceFaceResultModule)EventRuleAssetUtility.AddModule(
                rule, typeof(ForceFaceResultModule), "results.Array.data[0].result");
            QueueActiveOverlayResultModule second = (QueueActiveOverlayResultModule)
                EventRuleAssetUtility.AddModule(
                    rule, typeof(QueueActiveOverlayResultModule), "results.Array.data[1].result");
            Undo.ClearAll();

            Assert.That(EventRuleAssetUtility.MoveResult(rule, 0, 1), Is.True);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(path);
            Assert.That(rule.Results[0].Result, Is.TypeOf<QueueActiveOverlayResultModule>());
            Assert.That(rule.Results[1].Result, Is.TypeOf<ForceFaceResultModule>());

            Undo.PerformUndo();
            AssetDatabase.SaveAssets();
            Assert.That(rule.Results[0].Result, Is.SameAs(first));
            Assert.That(rule.Results[1].Result, Is.SameAs(second));

            Undo.PerformRedo();
            AssetDatabase.SaveAssets();
            Assert.That(rule.Results[0].Result, Is.SameAs(second));
            Assert.That(rule.Results[1].Result, Is.SameAs(first));
        }

        [Test]
        public void DuplicateRuleOwnsDistinctCopiesOfEveryReferencedModule()
        {
            string sourcePath = Path("DuplicateSource.asset");
            string copyPath = Path("DuplicateCopy.asset");
            EventRuleDefinition source = EventRuleAssetUtility.CreateRule(sourcePath);
            EventRuleAssetUtility.AddModule(source, typeof(SignalTypeTriggerModule), "trigger");
            EventRuleAssetUtility.AddModuleToArray(
                source, typeof(AttackEffectConditionModule), "conditions");
            SetArraySize(source, "results", 1);
            EventRuleAssetUtility.AddModuleToArray(
                source, typeof(SourceFaceConditionModule), "results.Array.data[0].conditions");
            EventRuleAssetUtility.AddModule(
                source, typeof(ForceFaceResultModule), "results.Array.data[0].result");

            EventRuleDefinition copy = EventRuleAssetUtility.DuplicateRule(source, copyPath);

            List<Object> sourceModules = CollectReferencedModules(source);
            List<Object> copyModules = CollectReferencedModules(copy);
            Assert.That(copyModules.Count, Is.EqualTo(sourceModules.Count));
            Assert.That(copyModules.All(module => AssetDatabase.GetAssetPath(module) == copyPath), Is.True);
            Assert.That(copyModules.Any(module => sourceModules.Contains(module)), Is.False,
                "The duplicate must never point at a source Rule SubAsset.");
            Assert.That(copyModules.Select(module => module.GetType()),
                Is.EquivalentTo(sourceModules.Select(module => module.GetType())));
        }

        [Test]
        public void ValidatorMapsStructuralOwnershipRecursionServiceAndInfoSeverities()
        {
            EventRuleDefinition invalid = EventRuleAssetUtility.CreateRule(Path("Invalid.asset"));
            SetEnum(invalid, "allowedSlots", (int)DiceFaceSlotMask.Base);
            IReadOnlyList<EventRuleValidationIssue> structural =
                EventRuleValidator.Validate(invalid, DiceFaceSlotType.OnFire);
            AssertIssue(structural, EventRuleValidationSeverity.Error, "RULE_TRIGGER_MISSING");
            AssertIssue(structural, EventRuleValidationSeverity.Error, "RULE_RESULTS_EMPTY");
            AssertIssue(structural, EventRuleValidationSeverity.Error, "RULE_SLOT_CONFLICT");

            EventRuleDefinition owner = CreateValidRule("Owner.asset");
            EventRuleDefinition borrower = CreateValidRule("Borrower.asset");
            SetObjectReference(borrower, "trigger", owner.Trigger);
            IReadOnlyList<EventRuleValidationIssue> ownership =
                EventRuleValidator.Validate(borrower, DiceFaceSlotType.OnFire);
            AssertIssue(ownership, EventRuleValidationSeverity.Error, "MODULE_FOREIGN_SUBASSET");

            SetEnum(owner, "recursionPolicy", (int)EventRuleRecursionPolicy.AllowWithBudget);
            EventRuleValidationEnvironment unavailable = new EventRuleValidationEnvironment(
                optionalServicesAvailable: false,
                passiveStateSupported: true);
            IReadOnlyList<EventRuleValidationIssue> warnings =
                EventRuleValidator.Validate(owner, DiceFaceSlotType.OnFire, unavailable);
            AssertIssue(warnings, EventRuleValidationSeverity.Warning, "RULE_RECURSION_RISK");
            AssertIssue(warnings, EventRuleValidationSeverity.Warning, "SERVICE_UNAVAILABLE");

            EventRuleDefinition compatible = CreateValidRule("Compatible.asset");
            IReadOnlyList<EventRuleValidationIssue> valid =
                EventRuleValidator.Validate(compatible, DiceFaceSlotType.OnFire);
            Assert.That(valid.Any(issue => issue.Severity == EventRuleValidationSeverity.Info), Is.True,
                "A valid deny-reentry Rule should expose its legacy-compatible policy as Info.");
            Assert.That(valid.All(issue => StableCodes.Contains(issue.Code)), Is.True);
        }

        [Test]
        public void ValidatorMapsUnclassifiedRuntimeProblemsWithoutMutatingAssets()
        {
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(Path("NoMutation.asset"));
            EventRuleAssetUtility.AddModule(rule, typeof(SignalTypeTriggerModule), "trigger");
            SetArraySize(rule, "results", 1);
            IncrementCounterResultModule result = (IncrementCounterResultModule)
                EventRuleAssetUtility.AddModule(
                    rule, typeof(IncrementCounterResultModule), "results.Array.data[0].result");
            SerializedObject serializedResult = new SerializedObject(result);
            serializedResult.FindProperty("counterKey").stringValue = " ";
            serializedResult.FindProperty("amount").intValue = -1;
            serializedResult.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(result);
            AssetDatabase.SaveAssets();
            string ruleBefore = EditorJsonUtility.ToJson(rule);
            string resultBefore = EditorJsonUtility.ToJson(result);

            IReadOnlyList<EventRuleValidationIssue> issues =
                EventRuleValidator.Validate(rule, DiceFaceSlotType.OnFire);

            Assert.That(issues.Any(issue =>
                issue.Code == "MODULE_REFERENCE_MISSING" &&
                issue.Context == result &&
                issue.Severity == EventRuleValidationSeverity.Error &&
                issue.Message.Contains("增加数量")), Is.True);
            Assert.That(issues.All(issue => StableCodes.Contains(issue.Code)), Is.True);
            Assert.That(EditorJsonUtility.ToJson(rule), Is.EqualTo(ruleBefore));
            Assert.That(EditorJsonUtility.ToJson(result), Is.EqualTo(resultBefore));
        }

        [Test]
        public void AssetMutationsRejectNullAndUnsavedRules()
        {
            EventRuleDefinition unsaved = ScriptableObject.CreateInstance<EventRuleDefinition>();
            try
            {
                Assert.Throws<ArgumentNullException>(() => EventRuleAssetUtility.AddModule(
                    null, typeof(SignalTypeTriggerModule), "trigger"));
                Assert.Throws<InvalidOperationException>(() => EventRuleAssetUtility.AddModule(
                    unsaved, typeof(SignalTypeTriggerModule), "trigger"));
                Assert.Throws<InvalidOperationException>(() =>
                    EventRuleAssetUtility.DuplicateRule(unsaved, Path("Rejected.asset")));
            }
            finally
            {
                Object.DestroyImmediate(unsaved);
            }
        }

        [Test]
        public void AddModuleRejectsAnIncompatiblePropertyWithoutLeavingAnOrphanSubAsset()
        {
            string path = Path("WrongModuleType.asset");
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(path);
            int assetCountBefore = AssetDatabase.LoadAllAssetsAtPath(path).Length;

            Assert.Throws<ArgumentException>(() => EventRuleAssetUtility.AddModule(
                rule, typeof(ForceFaceResultModule), "trigger"));

            Assert.That(rule.Trigger, Is.Null);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).Length, Is.EqualTo(assetCountBefore));
        }

        [Test]
        public void DuplicateRuleClonesForeignReferencedModulesInsteadOfSharingThem()
        {
            string copyPath = Path("ForeignDuplicate.asset");
            EventRuleDefinition owner = CreateValidRule("ForeignOwner.asset");
            EventRuleDefinition borrower = CreateValidRule("ForeignBorrower.asset");
            SetObjectReference(borrower, "trigger", owner.Trigger);

            EventRuleDefinition copy = EventRuleAssetUtility.DuplicateRule(borrower, copyPath);

            Assert.That(copy.Trigger, Is.Not.Null);
            Assert.That(copy.Trigger, Is.Not.SameAs(owner.Trigger));
            Assert.That(AssetDatabase.GetAssetPath(copy.Trigger), Is.EqualTo(copyPath));
        }

        private static readonly HashSet<string> StableCodes = new HashSet<string>
        {
            "RULE_TRIGGER_MISSING",
            "RULE_RESULTS_EMPTY",
            "RULE_SLOT_CONFLICT",
            "MODULE_REFERENCE_MISSING",
            "MODULE_FOREIGN_SUBASSET",
            "RULE_RECURSION_RISK",
            "PASSIVE_STATE_UNSUPPORTED",
            "SERVICE_UNAVAILABLE"
        };

        private static void AssertCatalogMatchesTypeCache<T>() where T : ScriptableObject
        {
            Type[] productionConcrete = TypeCache.GetTypesDerivedFrom<T>()
                .Where(type => type.Assembly == typeof(EventRuleDefinition).Assembly)
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .ToArray();
            Assert.That(productionConcrete.All(type =>
                type.GetCustomAttribute<EventRuleModuleMenuAttribute>() != null), Is.True,
                $"Every concrete {typeof(T).Name} must carry discovery metadata.");
            Type[] attributedConcrete = TypeCache.GetTypesDerivedFrom<T>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .Where(type => type.GetCustomAttribute<EventRuleModuleMenuAttribute>() != null)
                .ToArray();
            Assert.That(EventRuleModuleCatalog.GetModules<T>(), Is.EquivalentTo(attributedConcrete));
        }

        private static EventRuleDefinition CreateValidRule(string fileName)
        {
            EventRuleDefinition rule = EventRuleAssetUtility.CreateRule(Path(fileName));
            EventRuleAssetUtility.AddModule(rule, typeof(SignalTypeTriggerModule), "trigger");
            SetArraySize(rule, "results", 1);
            EventRuleAssetUtility.AddModule(
                rule, typeof(ForceFaceResultModule), "results.Array.data[0].result");
            return rule;
        }

        private static List<Object> CollectReferencedModules(EventRuleDefinition rule)
        {
            List<Object> modules = new List<Object>();
            Queue<Object> pending = new Queue<Object>();
            pending.Enqueue(rule);
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

                    Object reference = property.objectReferenceValue;
                    if (!(reference is EventTriggerModule) &&
                        !(reference is EventConditionModule) &&
                        !(reference is EventResultModule))
                    {
                        continue;
                    }

                    if (!modules.Contains(reference))
                    {
                        modules.Add(reference);
                        pending.Enqueue(reference);
                    }
                }
            }

            return modules;
        }

        private static void AssertIssue(
            IReadOnlyList<EventRuleValidationIssue> issues,
            EventRuleValidationSeverity severity,
            string code)
        {
            Assert.That(issues.Any(issue => issue.Severity == severity && issue.Code == code), Is.True,
                $"Expected {severity} {code}, got {string.Join(", ", issues.Select(issue => issue.Severity + ":" + issue.Code))}");
        }

        private static void SetArraySize(Object target, string propertyPath, int size)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyPath).arraySize = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private static void SetEnum(Object target, string propertyPath, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyPath).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private static void SetObjectReference(Object target, string propertyPath, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyPath).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private static string Path(string fileName) => $"{TempFolder}/{fileName}";

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
