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

            array.InsertArrayElementAtIndex(insertionIndex);
            SerializedProperty previewReference = array.GetArrayElementAtIndex(insertionIndex);
            if (previewReference.propertyType != SerializedPropertyType.ObjectReference)
            {
                serializedRule.Update();
                throw new ArgumentException(
                    $"Array {arrayPropertyPath} does not contain module references.",
                    nameof(arrayPropertyPath));
            }

            try
            {
                ValidateReferenceAcceptsModule(previewReference, moduleType);
            }
            finally
            {
                serializedRule.Update();
            }

            int undoGroup = BeginUndoGroup("Add Event Rule Module");
            ScriptableObject module = null;
            try
            {
                Undo.RecordObject(rule, "Add Event Rule Module");
                array = serializedRule.FindProperty(arrayPropertyPath);
                array.InsertArrayElementAtIndex(insertionIndex);
                SerializedProperty reference = array.GetArrayElementAtIndex(insertionIndex);
                reference.objectReferenceValue = null;
                module = CreateModuleSubAsset(rule, moduleType, "Add Event Rule Module");
                reference.objectReferenceValue = module;
                serializedRule.ApplyModifiedProperties();
                SaveChanged(rule, module);
                Undo.CollapseUndoOperations(undoGroup);
                return module;
            }
            catch
            {
                RollbackUndoGroup(undoGroup, new Object[] { module }, rule);
                throw;
            }
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

            IReadOnlyList<Object> removedClosure = CollectReachableModules(module);

            int undoGroup = BeginUndoGroup("Remove Event Rule Module");
            Undo.RecordObject(rule, "Remove Event Rule Module");
            reference.objectReferenceValue = null;
            serializedRule.ApplyModifiedProperties();
            DestroyOwnedModuleClosureIfUnreferenced(rule, removedClosure);
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
            IReadOnlyList<Object> removedClosure = CollectReachableModules(module);
            int undoGroup = BeginUndoGroup("Remove Event Rule Module");
            Undo.RecordObject(rule, "Remove Event Rule Module");
            reference.objectReferenceValue = null;
            array.DeleteArrayElementAtIndex(index);
            serializedRule.ApplyModifiedProperties();
            DestroyOwnedModuleClosureIfUnreferenced(rule, removedClosure);
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
            Object[] sourceModules = CollectReachableModules(source)
                .OrderBy(module => AssetDatabase.GetAssetPath(module), StringComparer.Ordinal)
                .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal)
                .ThenBy(module => module.name, StringComparer.Ordinal)
                .ToArray();

            int undoGroup = BeginUndoGroup("Duplicate Event Rule");
            EventRuleDefinition duplicate = null;
            List<Object> createdObjects = new List<Object>();
            try
            {
                duplicate = Object.Instantiate(source);
                createdObjects.Add(duplicate);
                duplicate.name = Path.GetFileNameWithoutExtension(destinationPath);
                AssetDatabase.CreateAsset(duplicate, destinationPath);
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Event Rule");

                Dictionary<Object, Object> replacements = new Dictionary<Object, Object>();
                for (int index = 0; index < sourceModules.Length; index++)
                {
                    Object sourceModule = sourceModules[index];
                    Object copiedModule = Object.Instantiate(sourceModule);
                    createdObjects.Add(copiedModule);
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
            catch
            {
                RollbackUndoGroup(undoGroup, createdObjects, null, destinationPath);
                throw;
            }
        }

        private static ScriptableObject CreateAndAssignModule(
            EventRuleDefinition rule,
            Type moduleType,
            SerializedObject serializedRule,
            SerializedProperty reference,
            string undoName)
        {
            int undoGroup = BeginUndoGroup(undoName);
            ScriptableObject module = null;
            try
            {
                Undo.RecordObject(rule, undoName);
                module = CreateModuleSubAsset(rule, moduleType, undoName);
                reference.objectReferenceValue = module;
                serializedRule.ApplyModifiedProperties();
                SaveChanged(rule, module);
                Undo.CollapseUndoOperations(undoGroup);
                return module;
            }
            catch
            {
                RollbackUndoGroup(undoGroup, new Object[] { module }, rule);
                throw;
            }
        }

        private static ScriptableObject CreateModuleSubAsset(
            EventRuleDefinition rule,
            Type moduleType,
            string undoName)
        {
            ScriptableObject module = ScriptableObject.CreateInstance(moduleType);
            try
            {
                module.name = moduleType.Name;
                AssetDatabase.AddObjectToAsset(module, rule);
                Undo.RegisterCreatedObjectUndo(module, undoName);
                return module;
            }
            catch
            {
                if (module != null)
                {
                    Object.DestroyImmediate(module, true);
                }

                throw;
            }
        }

        private static void DestroyOwnedModuleClosureIfUnreferenced(
            EventRuleDefinition rule,
            IReadOnlyList<Object> removedClosure)
        {
            string rulePath = AssetDatabase.GetAssetPath(rule);
            HashSet<Object> remainingReachable = new HashSet<Object>(
                CollectReachableModules(rule));
            List<Object> destroyCandidates = removedClosure
                .Where(module => module != null &&
                                 !remainingReachable.Contains(module) &&
                                 AssetDatabase.GetAssetPath(module) == rulePath)
                .ToList();
            if (destroyCandidates.Count > 0)
            {
                Undo.RegisterCompleteObjectUndo(
                    destroyCandidates.ToArray(),
                    "Remove Event Rule Module Graph");
            }

            for (int index = destroyCandidates.Count - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(destroyCandidates[index]);
            }
        }

        private static IReadOnlyList<Object> CollectReachableModules(Object root)
        {
            List<Object> modules = new List<Object>();
            HashSet<Object> foundModules = new HashSet<Object>();
            if (root == null)
            {
                return modules;
            }

            if (IsModule(root))
            {
                modules.Add(root);
                foundModules.Add(root);
            }

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
                    if (!IsModule(reference))
                    {
                        continue;
                    }

                    if (foundModules.Add(reference))
                    {
                        modules.Add(reference);
                    }

                    if (visited.Add(reference))
                    {
                        pending.Enqueue(reference);
                    }
                }
            }

            return modules;
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

        private static void RollbackUndoGroup(
            int undoGroup,
            IEnumerable<Object> createdObjects,
            Object changedObject = null,
            string createdAssetPath = null)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            if (!string.IsNullOrEmpty(createdAssetPath) &&
                AssetDatabase.LoadMainAssetAtPath(createdAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(createdAssetPath);
            }

            foreach (Object createdObject in createdObjects.Reverse())
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject, true);
                }
            }

            if (changedObject != null)
            {
                EditorUtility.SetDirty(changedObject);
            }

            AssetDatabase.SaveAssets();
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
