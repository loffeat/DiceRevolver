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
    public static class EventRuleAssetUtility
    {
        public static EventRuleDefinition CreateRule(string assetPath)
        {
            ValidateDestinationPath(assetPath);
            EventRuleDefinition rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
            rule.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(rule, assetPath);
            Undo.RegisterCreatedObjectUndo(rule, "Create Event Rule");
            EditorUtility.SetDirty(rule);
            AssetDatabase.SaveAssets();
            return rule;
        }

        public static IReadOnlyList<EventRuleDefinition> FindRules()
        {
            EventRuleDefinition[] rules = AssetDatabase.FindAssets("t:EventRuleDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<EventRuleDefinition>)
                .Where(rule => rule != null)
                .ToArray();
            return Array.AsReadOnly(rules);
        }

        public static ScriptableObject AddModule(
            EventRuleDefinition rule,
            Type moduleType,
            string referencePropertyPath)
        {
            EnsureSavedRule(rule);
            ValidateModuleType(moduleType);
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty reference = RequireObjectReference(
                serializedRule,
                referencePropertyPath);
            ValidateReferenceAcceptsModule(reference, moduleType);
            if (reference.objectReferenceValue != null)
            {
                throw new InvalidOperationException(
                    $"Property {referencePropertyPath} already contains a module.");
            }

            return CreateAndAssignModule(
                rule,
                moduleType,
                serializedRule,
                reference,
                "Add Event Rule Module");
        }

        public static ScriptableObject AddModuleToArray(
            EventRuleDefinition rule,
            Type moduleType,
            string arrayPropertyPath,
            int index = -1)
        {
            EnsureSavedRule(rule);
            ValidateModuleType(moduleType);
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty array = serializedRule.FindProperty(arrayPropertyPath);
            if (array == null || !array.isArray || array.propertyType == SerializedPropertyType.String)
            {
                throw new ArgumentException(
                    $"Property {arrayPropertyPath} is not a serialized array.",
                    nameof(arrayPropertyPath));
            }

            int insertionIndex = index < 0 ? array.arraySize : index;
            if (insertionIndex < 0 || insertionIndex > array.arraySize)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int undoGroup = BeginUndoGroup("Add Event Rule Module");
            Undo.RecordObject(rule, "Add Event Rule Module");
            array.InsertArrayElementAtIndex(insertionIndex);
            SerializedProperty reference = array.GetArrayElementAtIndex(insertionIndex);
            if (reference.propertyType != SerializedPropertyType.ObjectReference)
            {
                Undo.CollapseUndoOperations(undoGroup);
                throw new ArgumentException(
                    $"Array {arrayPropertyPath} does not contain module references.",
                    nameof(arrayPropertyPath));
            }

            ValidateReferenceAcceptsModule(reference, moduleType);
            reference.objectReferenceValue = null;
            ScriptableObject module = CreateModuleSubAsset(rule, moduleType, "Add Event Rule Module");
            reference.objectReferenceValue = module;
            serializedRule.ApplyModifiedProperties();
            SaveChanged(rule, module);
            Undo.CollapseUndoOperations(undoGroup);
            return module;
        }

        public static bool RemoveModule(
            EventRuleDefinition rule,
            string referencePropertyPath)
        {
            EnsureSavedRule(rule);
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty reference = RequireObjectReference(
                serializedRule,
                referencePropertyPath);
            Object module = reference.objectReferenceValue;
            if (!IsModule(module))
            {
                return false;
            }

            int undoGroup = BeginUndoGroup("Remove Event Rule Module");
            Undo.RecordObject(rule, "Remove Event Rule Module");
            reference.objectReferenceValue = null;
            serializedRule.ApplyModifiedProperties();
            DestroyOwnedModuleIfUnreferenced(rule, module);
            SaveChanged(rule);
            Undo.CollapseUndoOperations(undoGroup);
            return true;
        }

        public static bool RemoveModuleFromArray(
            EventRuleDefinition rule,
            string arrayPropertyPath,
            int index)
        {
            EnsureSavedRule(rule);
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty array = serializedRule.FindProperty(arrayPropertyPath);
            if (array == null || !array.isArray ||
                index < 0 || index >= array.arraySize)
            {
                return false;
            }

            SerializedProperty reference = array.GetArrayElementAtIndex(index);
            if (reference.propertyType != SerializedPropertyType.ObjectReference)
            {
                return false;
            }

            Object module = reference.objectReferenceValue;
            int undoGroup = BeginUndoGroup("Remove Event Rule Module");
            Undo.RecordObject(rule, "Remove Event Rule Module");
            reference.objectReferenceValue = null;
            array.DeleteArrayElementAtIndex(index);
            serializedRule.ApplyModifiedProperties();
            DestroyOwnedModuleIfUnreferenced(rule, module);
            SaveChanged(rule);
            Undo.CollapseUndoOperations(undoGroup);
            return true;
        }

        public static bool MoveResult(EventRuleDefinition rule, int fromIndex, int toIndex)
        {
            EnsureSavedRule(rule);
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty results = serializedRule.FindProperty("results");
            if (fromIndex < 0 || fromIndex >= results.arraySize ||
                toIndex < 0 || toIndex >= results.arraySize ||
                fromIndex == toIndex)
            {
                return false;
            }

            int undoGroup = BeginUndoGroup("Move Event Rule Result");
            Undo.RecordObject(rule, "Move Event Rule Result");
            results.MoveArrayElement(fromIndex, toIndex);
            serializedRule.ApplyModifiedProperties();
            SaveChanged(rule);
            Undo.CollapseUndoOperations(undoGroup);
            return true;
        }

        public static EventRuleDefinition DuplicateRule(
            EventRuleDefinition source,
            string destinationPath)
        {
            EnsureSavedRule(source);
            ValidateDestinationPath(destinationPath);

            int undoGroup = BeginUndoGroup("Duplicate Event Rule");
            EventRuleDefinition duplicate = Object.Instantiate(source);
            duplicate.name = Path.GetFileNameWithoutExtension(destinationPath);
            AssetDatabase.CreateAsset(duplicate, destinationPath);
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Event Rule");

            string sourcePath = AssetDatabase.GetAssetPath(source);
            Dictionary<Object, Object> replacements = new Dictionary<Object, Object>();
            Object[] sourceModules = CollectModulesToDuplicate(source, sourcePath);
            for (int index = 0; index < sourceModules.Length; index++)
            {
                Object sourceModule = sourceModules[index];
                Object copiedModule = Object.Instantiate(sourceModule);
                copiedModule.name = sourceModule.name;
                AssetDatabase.AddObjectToAsset(copiedModule, duplicate);
                Undo.RegisterCreatedObjectUndo(copiedModule, "Duplicate Event Rule Module");
                replacements.Add(sourceModule, copiedModule);
            }

            RemapObjectReferences(duplicate, replacements);
            foreach (Object copiedModule in replacements.Values)
            {
                RemapObjectReferences(copiedModule, replacements);
                EditorUtility.SetDirty(copiedModule);
            }

            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            return duplicate;
        }

        private static ScriptableObject CreateAndAssignModule(
            EventRuleDefinition rule,
            Type moduleType,
            SerializedObject serializedRule,
            SerializedProperty reference,
            string undoName)
        {
            int undoGroup = BeginUndoGroup(undoName);
            Undo.RecordObject(rule, undoName);
            ScriptableObject module = CreateModuleSubAsset(rule, moduleType, undoName);
            reference.objectReferenceValue = module;
            serializedRule.ApplyModifiedProperties();
            SaveChanged(rule, module);
            Undo.CollapseUndoOperations(undoGroup);
            return module;
        }

        private static ScriptableObject CreateModuleSubAsset(
            EventRuleDefinition rule,
            Type moduleType,
            string undoName)
        {
            ScriptableObject module = ScriptableObject.CreateInstance(moduleType);
            module.name = moduleType.Name;
            AssetDatabase.AddObjectToAsset(module, rule);
            Undo.RegisterCreatedObjectUndo(module, undoName);
            return module;
        }

        private static void DestroyOwnedModuleIfUnreferenced(
            EventRuleDefinition rule,
            Object module)
        {
            if (!IsModule(module) ||
                AssetDatabase.GetAssetPath(module) != AssetDatabase.GetAssetPath(rule) ||
                IsReferencedBy(rule, module))
            {
                return;
            }

            Undo.DestroyObjectImmediate(module);
        }

        private static bool IsReferencedBy(Object root, Object target)
        {
            HashSet<Object> visited = new HashSet<Object>();
            Queue<Object> pending = new Queue<Object>();
            pending.Enqueue(root);
            visited.Add(root);
            while (pending.Count > 0)
            {
                SerializedObject serialized = new SerializedObject(pending.Dequeue());
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
                    if (reference == target)
                    {
                        return true;
                    }

                    if (IsModule(reference) && visited.Add(reference))
                    {
                        pending.Enqueue(reference);
                    }
                }
            }

            return false;
        }

        private static void RemapObjectReferences(
            Object target,
            IReadOnlyDictionary<Object, Object> replacements)
        {
            Undo.RecordObject(target, "Duplicate Event Rule References");
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue != null &&
                    replacements.TryGetValue(property.objectReferenceValue, out Object replacement))
                {
                    property.objectReferenceValue = replacement;
                }
            }

            serialized.ApplyModifiedProperties();
        }

        private static Object[] CollectModulesToDuplicate(
            EventRuleDefinition source,
            string sourcePath)
        {
            HashSet<Object> modules = new HashSet<Object>(
                AssetDatabase.LoadAllAssetsAtPath(sourcePath).Where(IsModule));
            HashSet<Object> visited = new HashSet<Object> { source };
            Queue<Object> pending = new Queue<Object>();
            pending.Enqueue(source);
            while (pending.Count > 0)
            {
                SerializedObject serialized = new SerializedObject(pending.Dequeue());
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = true;
                    Object reference = property.propertyType == SerializedPropertyType.ObjectReference
                        ? property.objectReferenceValue
                        : null;
                    if (!IsModule(reference))
                    {
                        continue;
                    }

                    modules.Add(reference);
                    if (visited.Add(reference))
                    {
                        pending.Enqueue(reference);
                    }
                }
            }

            return modules
                .OrderBy(module => AssetDatabase.GetAssetPath(module), StringComparer.Ordinal)
                .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal)
                .ThenBy(module => module.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static SerializedProperty RequireObjectReference(
            SerializedObject serialized,
            string propertyPath)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                throw new ArgumentException(
                    $"Property {propertyPath} is not an object reference.",
                    nameof(propertyPath));
            }

            return property;
        }

        private static void ValidateReferenceAcceptsModule(
            SerializedProperty reference,
            Type moduleType)
        {
            Type requiredBase = null;
            string serializedType = reference.type;
            if (serializedType.Contains(nameof(EventTriggerModule)))
            {
                requiredBase = typeof(EventTriggerModule);
            }
            else if (serializedType.Contains(nameof(EventConditionModule)))
            {
                requiredBase = typeof(EventConditionModule);
            }
            else if (serializedType.Contains(nameof(EventResultModule)))
            {
                requiredBase = typeof(EventResultModule);
            }

            if (requiredBase != null && !requiredBase.IsAssignableFrom(moduleType))
            {
                throw new ArgumentException(
                    $"Property {reference.propertyPath} requires {requiredBase.Name}, not {moduleType.Name}.",
                    nameof(moduleType));
            }
        }

        private static void EnsureSavedRule(EventRuleDefinition rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (!AssetDatabase.Contains(rule) ||
                string.IsNullOrEmpty(AssetDatabase.GetAssetPath(rule)))
            {
                throw new InvalidOperationException(
                    "Event Rule asset operations require a saved main asset.");
            }
        }

        private static void ValidateModuleType(Type moduleType)
        {
            if (moduleType == null)
            {
                throw new ArgumentNullException(nameof(moduleType));
            }

            if (moduleType.IsAbstract || moduleType.ContainsGenericParameters ||
                (!typeof(EventTriggerModule).IsAssignableFrom(moduleType) &&
                 !typeof(EventConditionModule).IsAssignableFrom(moduleType) &&
                 !typeof(EventResultModule).IsAssignableFrom(moduleType)))
            {
                throw new ArgumentException(
                    $"Type {moduleType.FullName} is not a concrete Event Rule module.",
                    nameof(moduleType));
            }
        }

        private static void ValidateDestinationPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Event Rule paths must be .asset files under Assets/.",
                    nameof(assetPath));
            }

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
            {
                throw new DirectoryNotFoundException(parent);
            }

            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                throw new IOException($"An asset already exists at {assetPath}.");
            }
        }

        private static bool IsModule(Object value)
        {
            return value is EventTriggerModule ||
                   value is EventConditionModule ||
                   value is EventResultModule;
        }

        private static int BeginUndoGroup(string name)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return group;
        }

        private static void SaveChanged(params Object[] changed)
        {
            for (int index = 0; index < changed.Length; index++)
            {
                if (changed[index] != null)
                {
                    EditorUtility.SetDirty(changed[index]);
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
