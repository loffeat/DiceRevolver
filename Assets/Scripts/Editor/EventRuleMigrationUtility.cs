using System;
using System.Linq;
using DiceRevolver.Prototype;
using UnityEditor;
using UnityEngine;

namespace DiceRevolver.Editor
{
    public static class EventRuleMigrationUtility
    {
        private const string Root = "Assets/Resources/DiceFacePrototype";
        private const string CoreRuleFolder = Root + "/EventRules/Core";

        [MenuItem("Dice Revolver/Migrate Core Event Rules")]
        public static void MigrateCoreRules()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "DiceFacePrototype");
            EnsureFolder(Root, "EventRules");
            EnsureFolder(Root + "/EventRules", "Core");

            ProjectileSpawnEffect basicEffect = LoadRequired<ProjectileSpawnEffect>(
                Root + "/BulletEvents/FireBasicRevolverProjectile.asset");
            ExtraShotOnFireEffect doubleTapEffect = LoadRequired<ExtraShotOnFireEffect>(
                Root + "/BulletEvents/ExtraShotOnFireEffect.asset");
            ExplosionOnHitEffect blastEffect = LoadRequired<ExplosionOnHitEffect>(
                Root + "/BulletEvents/ExplosionOnHitEffect.asset");

            Migration[] migrations =
            {
                EnsureBasicShot(basicEffect),
                EnsureDoubleTap(doubleTapEffect),
                EnsureBlastRound(blastEffect),
                EnsureLoadedFour()
            };

            AssetDatabase.SaveAssets();
            for (int index = 0; index < migrations.Length; index++)
            {
                LinkEntryAfterParity(migrations[index]);
            }

            AssetDatabase.SaveAssets();
        }

        private static Migration EnsureBasicShot(ProjectileSpawnEffect legacy)
        {
            Migration migration = EnsureRule(
                "BasicShot",
                DiceFaceSlotType.Base,
                EventSignalMask.Base);
            EnsureResult<SpawnProjectileResultModule>(migration.Rule, module =>
            {
                SerializedObject serialized = new SerializedObject(module);
                serialized.FindProperty("projectileDefinition").objectReferenceValue =
                    legacy.ProjectileDefinition;
                serialized.FindProperty("delaySeconds").floatValue = legacy.DelaySeconds;
                serialized.FindProperty("attackEffectOverride").enumValueIndex =
                    (int)legacy.AttackEffectOverride;
                serialized.FindProperty("primaryProjectile").boolValue = legacy.PrimaryProjectile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
            return migration;
        }

        private static Migration EnsureDoubleTap(ExtraShotOnFireEffect legacy)
        {
            Migration migration = EnsureRule(
                "DoubleTap",
                DiceFaceSlotType.OnFire,
                EventSignalMask.OnFire);
            EnsureResult<SpawnProjectileResultModule>(migration.Rule, module =>
            {
                SerializedObject serialized = new SerializedObject(module);
                serialized.FindProperty("useCurrentPrimaryDefinition").boolValue = true;
                serialized.FindProperty("delaySeconds").floatValue = legacy.DelaySeconds;
                serialized.FindProperty("attackEffectOverride").enumValueIndex =
                    (int)legacy.AttackEffectOverride;
                serialized.FindProperty("primaryProjectile").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
            return migration;
        }

        private static Migration EnsureBlastRound(ExplosionOnHitEffect legacy)
        {
            Migration migration = EnsureRule(
                "BlastRound",
                DiceFaceSlotType.OnHit,
                EventSignalMask.OnHit);
            EnsureRuleCondition<AttackEffectConditionModule>(migration.Rule, module =>
            {
                SerializedObject serialized = new SerializedObject(module);
                serialized.FindProperty("expectedCanTriggerHitEffects").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
            EnsureResult<SpawnProjectileResultModule>(migration.Rule, module =>
            {
                SerializedObject serialized = new SerializedObject(module);
                serialized.FindProperty("projectileDefinition").objectReferenceValue =
                    legacy.ExplosionProjectileDefinition;
                serialized.FindProperty("useHitOrigin").boolValue = true;
                serialized.FindProperty("attackEffectOverride").enumValueIndex =
                    (int)AttackEffectOverride.UseProjectileDefault;
                serialized.FindProperty("primaryProjectile").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
            return migration;
        }

        private static Migration EnsureLoadedFour()
        {
            Migration migration = EnsureRule(
                "LoadedFour",
                DiceFaceSlotType.OnFireEnd,
                EventSignalMask.OnFireEnd);
            EnsureResult<ForceFaceResultModule>(migration.Rule, module =>
            {
                SerializedObject serialized = new SerializedObject(module);
                serialized.FindProperty("face").intValue = 4;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
            return migration;
        }

        private static Migration EnsureRule(
            string assetName,
            DiceFaceSlotType slot,
            EventSignalMask signal)
        {
            string entryPath = $"{Root}/DiceFaces/{assetName}.asset";
            string rulePath = $"{CoreRuleFolder}/{assetName}Rule.asset";
            DiceFaceEntry entry = LoadRequired<DiceFaceEntry>(entryPath);
            EventRuleDefinition rule = AssetDatabase.LoadAssetAtPath<EventRuleDefinition>(rulePath);
            if (rule == null)
            {
                rule = ScriptableObject.CreateInstance<EventRuleDefinition>();
                rule.name = assetName + "Rule";
                AssetDatabase.CreateAsset(rule, rulePath);
                SerializedObject serialized = new SerializedObject(rule);
                serialized.FindProperty("displayName").stringValue = entry.DisplayName;
                serialized.FindProperty("description").stringValue = entry.Description;
                serialized.FindProperty("displayColor").colorValue = entry.DisplayColor;
                serialized.FindProperty("allowedSlots").intValue = (int)ToMask(slot);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureTrigger(rule, signal);
            EditorUtility.SetDirty(rule);
            return new Migration(entryPath, rulePath, rule, slot, signal);
        }

        private static void EnsureTrigger(EventRuleDefinition rule, EventSignalMask signal)
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty trigger = serializedRule.FindProperty("trigger");
            if (trigger.objectReferenceValue != null)
            {
                return;
            }

            SignalTypeTriggerModule module = CreateModule<SignalTypeTriggerModule>(rule);
            SerializedObject serializedModule = new SerializedObject(module);
            serializedModule.FindProperty("signals").intValue = (int)signal;
            serializedModule.ApplyModifiedPropertiesWithoutUndo();
            trigger.objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rule);
            EditorUtility.SetDirty(module);
        }

        private static void EnsureRuleCondition<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventConditionModule
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty conditions = serializedRule.FindProperty("conditions");
            if (conditions.arraySize != 0)
            {
                return;
            }

            T module = CreateModule<T>(rule);
            configure(module);
            conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0).objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rule);
            EditorUtility.SetDirty(module);
        }

        private static void EnsureResult<T>(EventRuleDefinition rule, Action<T> configure)
            where T : EventResultModule
        {
            SerializedObject serializedRule = new SerializedObject(rule);
            SerializedProperty results = serializedRule.FindProperty("results");
            if (results.arraySize != 0)
            {
                return;
            }

            T module = CreateModule<T>(rule);
            configure(module);
            results.arraySize = 1;
            SerializedProperty entry = results.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("conditions").arraySize = 0;
            entry.FindPropertyRelative("result").objectReferenceValue = module;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rule);
            EditorUtility.SetDirty(module);
        }

        private static T CreateModule<T>(EventRuleDefinition rule) where T : ScriptableObject
        {
            T module = ScriptableObject.CreateInstance<T>();
            module.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(module, rule);
            return module;
        }

        private static void LinkEntryAfterParity(Migration migration)
        {
            DiceFaceEntry entry = LoadRequired<DiceFaceEntry>(migration.EntryPath);
            EventRuleDefinition rule = LoadRequired<EventRuleDefinition>(migration.RulePath);
            if (!HasExpectedShape(rule, migration.Slot, migration.Signal))
            {
                return;
            }

            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedProperty ruleReference = serializedEntry.FindProperty("rule");
            if (ruleReference.objectReferenceValue == null)
            {
                ruleReference.objectReferenceValue = rule;
                serializedEntry.ApplyModifiedPropertiesWithoutUndo();
                serializedEntry.Update();
            }

            if (ruleReference.objectReferenceValue != rule)
            {
                return;
            }

            serializedEntry.FindProperty("effect").objectReferenceValue = null;
            ClearArray(serializedEntry.FindProperty("onFireEffects"));
            ClearArray(serializedEntry.FindProperty("onHitEffects"));
            ClearArray(serializedEntry.FindProperty("onFireEndEffects"));
            serializedEntry.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static bool HasExpectedShape(
            EventRuleDefinition rule,
            DiceFaceSlotType slot,
            EventSignalMask signal)
        {
            if (rule == null || !rule.AllowsSlot(slot) ||
                rule.Trigger is not SignalTypeTriggerModule trigger ||
                trigger.Signals != signal || rule.Results == null || rule.Results.Count == 0 ||
                rule.Results.Any(entry => entry?.Result == null))
            {
                return false;
            }

            return slot switch
            {
                DiceFaceSlotType.Base => rule.FindPrimaryProjectileDefinition() != null,
                DiceFaceSlotType.OnFire => rule.Results.Any(
                    entry => entry.Result is SpawnProjectileResultModule),
                DiceFaceSlotType.OnHit => rule.Conditions.Any(
                        condition => condition is AttackEffectConditionModule) &&
                    rule.Results.Any(entry => entry.Result is SpawnProjectileResultModule),
                DiceFaceSlotType.OnFireEnd => rule.Results.Any(
                    entry => entry.Result is ForceFaceResultModule),
                _ => false
            };
        }

        private static void ClearArray(SerializedProperty property)
        {
            if (property != null && property.isArray)
            {
                property.arraySize = 0;
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
                DiceFaceSlotType.Passive => DiceFaceSlotMask.Passive,
                _ => DiceFaceSlotMask.None
            };
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }

            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private readonly struct Migration
        {
            public Migration(
                string entryPath,
                string rulePath,
                EventRuleDefinition rule,
                DiceFaceSlotType slot,
                EventSignalMask signal)
            {
                EntryPath = entryPath;
                RulePath = rulePath;
                Rule = rule;
                Slot = slot;
                Signal = signal;
            }

            public string EntryPath { get; }
            public string RulePath { get; }
            public EventRuleDefinition Rule { get; }
            public DiceFaceSlotType Slot { get; }
            public EventSignalMask Signal { get; }
        }
    }
}
